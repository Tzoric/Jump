using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class AutomatedPlaytester : MonoBehaviour
{
    private const float DefaultTimeoutSeconds = 120f;
    private const float FailureHeight = -30f;

    private readonly List<AutomatedPlaytestWaypoint> waypoints = new();

    private HeroMovement hero;
    private LevelExitDoor exitDoor;
    private MineLevelMenuController levelMenu;
    private float startedAt;
    private float jumpReleaseAt;
    private float nextJumpAt;
    private int initialCollectibleCount;
    private int jumpAttempts;
    private int waypointIndex;
    private string reportPath;
    private string startingScenePath;
    private float timeoutSeconds;
    private float nextProgressLogAt;
    private Vector3 lastMovingPosition;
    private Vector3 lastPlayerPosition;
    private float lastMovedAt;
    private float escapeUntil;
    private float escapeDirection;
    private int forcedJumpReleaseFrames;
    private int lastHealth;
    private int lastRespawnCount;
    private bool goalInitialized;
    private bool usesExitDoor;
    private bool failOnRespawn;
    private bool failOnDamage;
    private bool powerRunEnabled;
    private bool returnHomeMode;
    private bool traceFirstJump;
    private bool returnHomeRequested;
    private bool airborneSinceLastWaypoint;
    private bool startSettled;
    private int passAfterWaypoints;
    private int startAfterWaypoint;
    private string expectedExitSceneName;
    private float failureHeight = FailureHeight;
    private int startingUnlockedLevel;
    private int startingCrystals;
    private int startingLives;
    private int startingPotions;
    private float traceFirstJumpUntil;
    private float nextJumpTraceAt;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartWhenRequested()
    {
        string[] arguments = Environment.GetCommandLineArgs();
        bool requestedByCommandLine = Array.IndexOf(arguments, "-automatedPlaytest") >= 0;
        bool requestedByMenu = Environment.GetEnvironmentVariable("JUMP_AUTOMATED_PLAYTEST") == "1";
        if (!requestedByCommandLine && !requestedByMenu)
        {
            return;
        }

        Environment.SetEnvironmentVariable("JUMP_AUTOMATED_PLAYTEST", null);

        var runner = new GameObject("Automated Playtester");
        DontDestroyOnLoad(runner);
        runner.AddComponent<AutomatedPlaytester>();
        Debug.Log("AUTOMATED PLAYTEST: virtual controller bootstrap created.");
    }

    private void Start()
    {
        reportPath = ReadArgument("-playtestReport") ??
            Path.Combine(Application.dataPath, "..", "Logs", "AutomatedPlaytest.json");
        timeoutSeconds = ReadFloatArgument("-playtestTimeout", DefaultTimeoutSeconds);
        // Keep Update-driven held-jump timing identical to normal play. High time scales can
        // run several physics steps between Updates and artificially shorten the jump arc.
        Time.timeScale = 1f;
        failOnRespawn = Array.IndexOf(Environment.GetCommandLineArgs(), "-playtestFailOnRespawn") >= 0;
        failOnDamage = Array.IndexOf(Environment.GetCommandLineArgs(), "-playtestFailOnDamage") >= 0;
        powerRunEnabled = Array.IndexOf(Environment.GetCommandLineArgs(), "-playtestPowerRun") >= 0;
        returnHomeMode = Array.IndexOf(Environment.GetCommandLineArgs(), "-playtestReturnHome") >= 0;
        traceFirstJump = Array.IndexOf(Environment.GetCommandLineArgs(), "-playtestTraceFirstJump") >= 0;
        passAfterWaypoints = ReadIntArgument("-playtestPassAfterWaypoints", 0);
        startAfterWaypoint = ReadIntArgument("-playtestStartAfterWaypoint", 0);
        startingUnlockedLevel = GameProgress.HighestUnlockedLevel;
        startingCrystals = GameProgress.Crystals;
        startingLives = GameProgress.Lives;
        startingPotions = GameProgress.HealthPotions;
        startedAt = Time.unscaledTime;
        nextProgressLogAt = startedAt + 5f;
        startingScenePath = SceneManager.GetActiveScene().path;
        FindHeroAndBegin();
    }

    private void Update()
    {
        if (returnHomeMode && returnHomeRequested && SceneManager.GetActiveScene().path != startingScenePath)
        {
            MineShopController shop = FindFirstObjectByType<MineShopController>();
            bool preservedProgress = GameProgress.HighestUnlockedLevel == startingUnlockedLevel &&
                GameProgress.Crystals == startingCrystals && GameProgress.Lives == startingLives &&
                GameProgress.HealthPotions == startingPotions;
            bool reachedShop = SceneManager.GetActiveScene().name == MineLevelMenuController.DefaultHomeScene &&
                shop != null && shop.IsShopVisible && Mathf.Approximately(Time.timeScale, 1f);
            Finish(reachedShop && preservedProgress,
                reachedShop && preservedProgress
                    ? "Returned to the overview shop without completing the level or changing progress."
                    : "Return-home flow did not preserve progress, restore time, and open the overview shop.",
                false);
            return;
        }

        if (usesExitDoor && goalInitialized && SceneManager.GetActiveScene().path != startingScenePath)
        {
            string activeScene = SceneManager.GetActiveScene().name;
            if (string.Equals(activeScene, expectedExitSceneName, StringComparison.Ordinal))
            {
                Finish(true, "Reached the exit door and left the level.", true);
            }
            else
            {
                Finish(false, $"Left the level through '{activeScene}' instead of the exit door destination '{expectedExitSceneName}'.", false);
            }
            return;
        }

        if (Time.unscaledTime - startedAt > timeoutSeconds)
        {
            string goal = usesExitDoor ? "the exit door" : "every required crystal";
            Finish(false, $"The virtual controller timed out before reaching {goal}. " +
                "This is not proof that the level is impossible.", false);
            return;
        }

        if (hero == null)
        {
            FindHeroAndBegin();
            return;
        }

        lastPlayerPosition = hero.transform.position;
        PlayerHealth health = hero.GetComponent<PlayerHealth>();
        if (health != null)
        {
            lastHealth = health.CurrentHealth;
            lastRespawnCount = health.RespawnCount;
            if (failOnDamage && health.CurrentHealth < health.MaxHealth)
            {
                Finish(false, "The no-damage route regression lost at least one heart.", false);
                return;
            }
            if (failOnRespawn && lastRespawnCount > 0)
            {
                Finish(false, "The safe-route regression playtest lost a life before reaching the exit.", false);
                return;
            }
        }

        if (!startSettled)
        {
            if (!hero.IsGrounded)
            {
                hero.SetAutomatedInput(0f, false);
                return;
            }

            startSettled = true;
            lastMovingPosition = hero.transform.position;
            lastMovedAt = Time.unscaledTime;
        }

        if (returnHomeMode && !returnHomeRequested)
        {
            if (levelMenu == null)
            {
                Finish(false, "The level has no pause/home controller.", false);
                return;
            }

            levelMenu.SetPaused(true);
            if (!MineLevelMenuController.IsPaused || !Mathf.Approximately(Time.timeScale, 0f))
            {
                Finish(false, "The level could not enter its paused state before returning home.", false);
                return;
            }

            returnHomeRequested = true;
            levelMenu.ReturnToOverview();
            return;
        }

        if (hero.transform.position.y < failureHeight)
        {
            Finish(false, $"The player fell below y={failureHeight:0.##}.", false);
            return;
        }

        Transform target;
        int goalsRemaining;

        if (usesExitDoor)
        {
            if (!hero.IsGrounded)
            {
                airborneSinceLastWaypoint = true;
            }
            AdvancePastReachedWaypoints();
            if (passAfterWaypoints > 0 && waypointIndex >= passAfterWaypoints)
            {
                Finish(true, $"Reached {waypointIndex} regression waypoint(s) without losing a life.", false);
                return;
            }
            target = waypointIndex < waypoints.Count ? waypoints[waypointIndex].transform : exitDoor.transform;
            goalsRemaining = waypoints.Count - waypointIndex + 1;
        }
        else
        {
            List<GameObject> collectibles = FindCollectibles();
            if (collectibles.Count == 0)
            {
                Finish(true, "Collected every required crystal.", false);
                return;
            }

            target = ChooseCollectibleTarget(collectibles);
            goalsRemaining = collectibles.Count;
        }

        if (Time.unscaledTime >= nextProgressLogAt)
        {
            Rigidbody2D body = hero.GetComponent<Rigidbody2D>();
            Debug.Log($"AUTOMATED PLAYTEST: {goalsRemaining} route goals remain; " +
                $"player={hero.transform.position}; target={target.name} at {target.position}; " +
                $"grounded={hero.IsGrounded}; velocity={(body == null ? Vector2.zero : body.linearVelocity)}.");
            nextProgressLogAt = Time.unscaledTime + 5f;
        }

        float horizontalDistance = target.position.x - hero.transform.position.x;
        if (traceFirstJump && Time.unscaledTime < traceFirstJumpUntil &&
            Time.unscaledTime >= nextJumpTraceAt)
        {
            Rigidbody2D traceBody = hero.GetComponent<Rigidbody2D>();
            Debug.Log($"AUTOMATED FIRST-JUMP TRACE: player={hero.transform.position}; " +
                $"target={target.position}; grounded={hero.IsGrounded}; " +
                $"velocity={(traceBody == null ? Vector2.zero : traceBody.linearVelocity)}; " +
                $"preparing={hero.IsPreparingJump}; power={hero.IsPowerJumping}.");
            nextJumpTraceAt = Time.unscaledTime + .08f;
        }
        // Feather the virtual left stick near an authored landing instead of crossing
        // it at full run speed. This models normal player correction and prevents the
        // regression driver itself from turning a safe landing into a spike overshoot.
        float horizontal = Mathf.Abs(horizontalDistance) < .12f
            ? 0f
            : Mathf.Clamp(horizontalDistance / .65f, -1f, 1f);

        if (Vector3.Distance(hero.transform.position, lastMovingPosition) > 0.25f)
        {
            lastMovingPosition = hero.transform.position;
            lastMovedAt = Time.unscaledTime;
        }
        else if (Time.unscaledTime - lastMovedAt > 1.5f)
        {
            escapeDirection = horizontal <= 0f ? 1f : -1f;
            escapeUntil = Time.unscaledTime + 0.15f;
            lastMovedAt = Time.unscaledTime;
            Debug.Log($"AUTOMATED PLAYTEST: stuck recovery moving {escapeDirection}.");
        }

        if (Time.unscaledTime < escapeUntil)
        {
            horizontal = escapeDirection;
        }

        if (forcedJumpReleaseFrames > 0)
        {
            forcedJumpReleaseFrames--;
            hero.SetAutomatedInput(0f, false);
            return;
        }

        if (usesExitDoor && waypointIndex < waypoints.Count)
        {
            AutomatedPlaytestWaypoint currentWaypoint = waypoints[waypointIndex];
            Vector2 landingOffset = currentWaypoint.transform.position - hero.transform.position;
            if (currentWaypoint.Mode == AutomatedWaypointMode.GroundedLanding &&
                Mathf.Abs(landingOffset.x) <= currentWaypoint.ReachRadius && Mathf.Abs(landingOffset.y) <= .3f)
            {
                // Give a centered landing one quiet frame so physics can settle and the
                // waypoint can be acknowledged before another automated jump begins.
                hero.SetAutomatedInput(0f, false);
                return;
            }
        }

        if (usesExitDoor && waypointIndex >= waypoints.Count && exitDoor != null)
        {
            if (exitDoor.IsUsed)
            {
                hero.SetAutomatedInput(0f, false);
                return;
            }

            if (exitDoor.IsPlayerNearby)
            {
                hero.SetAutomatedInput(0f, false);
                if (hero.IsGrounded && exitDoor.TryInteract(hero))
                {
                    Debug.Log("AUTOMATED PLAYTEST: used Interact to enter the exit door.");
                }
                return;
            }
        }

        bool approachingAirbornePass = usesExitDoor && waypointIndex < waypoints.Count &&
            waypoints[waypointIndex].Mode == AutomatedWaypointMode.AirbornePass;
        bool waypointPowerRun = usesExitDoor && waypointIndex < waypoints.Count &&
            waypoints[waypointIndex].UsePowerJump;
        bool usePowerRun = powerRunEnabled || waypointPowerRun;
        bool readyForLaunch = !usesExitDoor || waypointIndex >= waypoints.Count ||
            IsReadyForRouteJump(target, horizontalDistance);
        if (hero.IsGrounded && !approachingAirbornePass && readyForLaunch &&
            Time.unscaledTime >= nextJumpAt)
        {
            float heldJumpSeconds = usePowerRun ? hero.PowerJumpHoldSeconds : .20f;
            jumpReleaseAt = Time.unscaledTime + hero.JumpAnticipationSeconds + heldJumpSeconds;
            nextJumpAt = Time.unscaledTime + 0.45f;
            jumpAttempts++;
            if (traceFirstJump && jumpAttempts == 1)
            {
                traceFirstJumpUntil = Time.unscaledTime + 1.5f;
                nextJumpTraceAt = Time.unscaledTime;
            }
            if (jumpAttempts <= 5)
            {
                Debug.Log($"AUTOMATED PLAYTEST: jump attempt {jumpAttempts} from {hero.transform.position}.");
            }
        }

        bool parachuteHeld = approachingAirbornePass && !hero.IsGrounded &&
            waypoints[waypointIndex].DeployParachute;
        hero.SetAutomatedInput(horizontal, Time.unscaledTime < jumpReleaseAt,
            usePowerRun && Mathf.Abs(horizontal) >= .5f, parachuteHeld);
    }

    private bool IsReadyForRouteJump(Transform target, float horizontalDistance)
    {
        if (target == null || target.position.y - hero.transform.position.y <= .8f ||
            Mathf.Abs(horizontalDistance) <= .8f)
        {
            return true;
        }

        int groundMask = LayerMask.GetMask("Ground");
        RaycastHit2D support = Physics2D.Raycast(hero.transform.position, Vector2.down, 2f, groundMask);
        if (support.collider == null) return true;

        // Begin the squat roughly two units before the support edge. The miner keeps
        // moving during anticipation, so a ledge-edge press would meet the next higher
        // platform before the body has risen above its side collider.
        const float launchMargin = 2f;
        return horizontalDistance > 0f
            ? hero.transform.position.x >= support.collider.bounds.max.x - launchMargin
            : hero.transform.position.x <= support.collider.bounds.min.x + launchMargin;
    }

    private void FindHeroAndBegin()
    {
        hero = FindFirstObjectByType<HeroMovement>();
        if (hero == null)
        {
            return;
        }

        hero.EnableAutomatedControl(true);
        lastMovingPosition = hero.transform.position;
        lastPlayerPosition = hero.transform.position;
        lastMovedAt = Time.unscaledTime;

        if (goalInitialized)
        {
            return;
        }

        exitDoor = FindFirstObjectByType<LevelExitDoor>();
        levelMenu = FindFirstObjectByType<MineLevelMenuController>();
        usesExitDoor = exitDoor != null;
        expectedExitSceneName = usesExitDoor ? exitDoor.DestinationScene : null;
        waypoints.Clear();
        waypoints.AddRange(FindObjectsByType<AutomatedPlaytestWaypoint>(FindObjectsSortMode.None)
            .OrderBy(waypoint => waypoint.Order));
        if (waypoints.Count > 0)
            failureHeight = Mathf.Min(FailureHeight, waypoints.Min(waypoint => waypoint.transform.position.y) - 15f);

        if (startAfterWaypoint > 0)
        {
            int startIndex = waypoints.FindIndex(waypoint => waypoint.Order == startAfterWaypoint);
            if (startIndex < 0)
            {
                Finish(false, $"The requested starting waypoint {startAfterWaypoint} does not exist.", false);
                return;
            }

            AutomatedPlaytestWaypoint startWaypoint = waypoints[startIndex];
            Rigidbody2D body = hero.GetComponent<Rigidbody2D>();
            hero.transform.position = startWaypoint.transform.position;
            if (body != null)
            {
                body.position = startWaypoint.transform.position;
                body.linearVelocity = Vector2.zero;
                body.angularVelocity = 0f;
            }
            Physics2D.SyncTransforms();
            waypointIndex = startIndex + 1;
            lastMovingPosition = hero.transform.position;
            lastPlayerPosition = hero.transform.position;
            lastMovedAt = Time.unscaledTime;
            Debug.Log($"AUTOMATED PLAYTEST: starting after route waypoint {startAfterWaypoint} at " +
                      $"{hero.transform.position} for a focused regression.");
        }

        List<GameObject> collectibles = FindCollectibles();
        initialCollectibleCount = collectibles.Count;
        goalInitialized = true;

        if (!usesExitDoor && initialCollectibleCount == 0)
        {
            Finish(false, "The scene has neither an exit door nor required crystals to use as a completion goal.", false);
            return;
        }

        Debug.Log(usesExitDoor
            ? $"AUTOMATED PLAYTEST: using exit-door route with {waypoints.Count} authored waypoints."
            : $"AUTOMATED PLAYTEST: using {initialCollectibleCount} required crystals as goals.");
    }

    private void AdvancePastReachedWaypoints()
    {
        while (waypointIndex < waypoints.Count)
        {
            AutomatedPlaytestWaypoint waypoint = waypoints[waypointIndex];
            Vector2 offset = waypoint.transform.position - hero.transform.position;
            if (waypoint.Mode == AutomatedWaypointMode.AirbornePass)
            {
                if (offset.magnitude > waypoint.ReachRadius) break;
                Debug.Log($"AUTOMATED PLAYTEST: reached airborne route waypoint {waypoint.Order}.");
                waypointIndex++;
                jumpReleaseAt = 0f;
                nextJumpAt = Time.unscaledTime;
                continue;
            }

            if (!hero.IsGrounded) break;
            Rigidbody2D body = hero.GetComponent<Rigidbody2D>();
            bool verticallyAligned = Mathf.Abs(offset.y) <= 0.3f;
            // Reach the authored landing center before advancing. A broad tolerance let the
            // controller launch from ledge tips and collide with the side of the next platform.
            bool horizontallyAligned = Mathf.Abs(offset.x) <= waypoint.ReachRadius;
            bool settled = body != null && Mathf.Abs(body.linearVelocity.y) <= .2f;
            // Some authored routes deliberately place waypoint 1 on the start shelf. Accept
            // that settled initial position without requiring a pointless jump in place.
            bool requiresAirborneArrival = waypointIndex > 0;
            if ((requiresAirborneArrival && !airborneSinceLastWaypoint) ||
                !verticallyAligned || !horizontallyAligned || !settled)
            {
                break;
            }

            Debug.Log($"AUTOMATED PLAYTEST: reached route waypoint {waypoint.Order}.");
            waypointIndex++;
            airborneSinceLastWaypoint = false;
            jumpReleaseAt = 0f;
            nextJumpAt = Time.unscaledTime;
            forcedJumpReleaseFrames = 1;
        }
    }

    private Transform ChooseCollectibleTarget(List<GameObject> collectibles)
    {
        float lowestHeight = collectibles.Min(collectible => collectible.transform.position.y);
        Transform best = collectibles[0].transform;
        float bestScore = float.PositiveInfinity;

        foreach (GameObject collectible in collectibles)
        {
            Vector2 offset = collectible.transform.position - hero.transform.position;
            float heightPenalty = collectible.transform.position.y <= lowestHeight + 1.5f ? 0f : 1000f;
            float score = heightPenalty + Mathf.Abs(offset.x) + Mathf.Abs(offset.y) * 0.25f;
            if (score < bestScore)
            {
                best = collectible.transform;
                bestScore = score;
            }
        }

        return best;
    }

    private static List<GameObject> FindCollectibles()
    {
        var result = new List<GameObject>();
        result.AddRange(GameObject.FindGameObjectsWithTag("BlueCrystal"));
        result.AddRange(GameObject.FindGameObjectsWithTag("BlackBigCrystal"));
        return result;
    }

    private void Finish(bool passed, string message, bool exitReached)
    {
        hero?.SetAutomatedInput(0f, false);
        var result = new PlaytestResult
        {
            scene = startingScenePath,
            completionMode = returnHomeMode ? "ReturnHome" : usesExitDoor ? "ExitDoor" : "RequiredCrystals",
            passed = passed,
            message = message,
            exitReached = exitReached,
            waypointsCompleted = waypointIndex,
            totalWaypoints = waypoints.Count,
            startingCollectibles = initialCollectibleCount,
            collectedBlueCrystals = hero == null ? 0 : hero.BlueCrystalCount,
            collectedBlackCrystals = hero == null ? 0 : hero.BlackBigCrystalCount,
            finalHealth = lastHealth,
            respawns = lastRespawnCount,
            elapsedRealSeconds = Time.unscaledTime - startedAt,
            finalPlayerPosition = lastPlayerPosition
        };

        string directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(reportPath, JsonUtility.ToJson(result, true));
        Debug.Log($"AUTOMATED PLAYTEST {(passed ? "PASSED" : "FAILED")}: {message}");

#if UNITY_EDITOR
        if (Application.isBatchMode)
        {
            UnityEditor.EditorApplication.Exit(passed ? 0 : 2);
        }
        else
        {
            UnityEditor.EditorApplication.isPlaying = false;
        }
#else
        Application.Quit(passed ? 0 : 2);
#endif
        enabled = false;
    }

    private static string ReadArgument(string name)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(arguments, name);
        return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
    }

    private static float ReadFloatArgument(string name, float fallback)
    {
        string value = ReadArgument(name);
        return float.TryParse(value, out float parsed) && parsed > 0f ? parsed : fallback;
    }

    private static int ReadIntArgument(string name, int fallback)
    {
        string value = ReadArgument(name);
        return int.TryParse(value, out int parsed) && parsed > 0 ? parsed : fallback;
    }

    [Serializable]
    private sealed class PlaytestResult
    {
        public string scene;
        public string completionMode;
        public bool passed;
        public string message;
        public bool exitReached;
        public int waypointsCompleted;
        public int totalWaypoints;
        public int startingCollectibles;
        public int collectedBlueCrystals;
        public int collectedBlackCrystals;
        public int finalHealth;
        public int respawns;
        public float elapsedRealSeconds;
        public Vector3 finalPlayerPosition;
    }
}
