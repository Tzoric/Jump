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
        Time.timeScale = 4f;
        startedAt = Time.unscaledTime;
        nextProgressLogAt = startedAt + 5f;
        startingScenePath = SceneManager.GetActiveScene().path;
        FindHeroAndBegin();
    }

    private void Update()
    {
        if (usesExitDoor && goalInitialized && SceneManager.GetActiveScene().path != startingScenePath)
        {
            Finish(true, "Reached the exit door and left the level.", true);
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
        }

        if (hero.transform.position.y < FailureHeight)
        {
            Finish(false, $"The player fell below y={FailureHeight}.", false);
            return;
        }

        Transform target;
        int goalsRemaining;

        if (usesExitDoor)
        {
            AdvancePastReachedWaypoints();
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
        float horizontal = Mathf.Abs(horizontalDistance) < 0.18f ? 0f : Mathf.Sign(horizontalDistance);

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

        if (hero.IsGrounded && Time.unscaledTime >= nextJumpAt)
        {
            jumpReleaseAt = Time.unscaledTime + 0.18f;
            nextJumpAt = Time.unscaledTime + 0.45f;
            jumpAttempts++;
            if (jumpAttempts <= 5)
            {
                Debug.Log($"AUTOMATED PLAYTEST: jump attempt {jumpAttempts} from {hero.transform.position}.");
            }
        }

        hero.SetAutomatedInput(horizontal, Time.unscaledTime < jumpReleaseAt);
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
        usesExitDoor = exitDoor != null;
        waypoints.Clear();
        waypoints.AddRange(FindObjectsByType<AutomatedPlaytestWaypoint>(FindObjectsSortMode.None)
            .OrderBy(waypoint => waypoint.Order));

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
        if (!hero.IsGrounded)
        {
            return;
        }

        while (waypointIndex < waypoints.Count)
        {
            Vector2 offset = waypoints[waypointIndex].transform.position - hero.transform.position;
            Rigidbody2D body = hero.GetComponent<Rigidbody2D>();
            bool verticallyAligned = Mathf.Abs(offset.y) <= 0.3f;
            // Reach the authored landing center before advancing. A broad tolerance let the
            // controller launch from ledge tips and collide with the side of the next platform.
            bool horizontallyAligned = Mathf.Abs(offset.x) <= .65f;
            bool settled = body != null && Mathf.Abs(body.linearVelocity.y) <= 0.2f;
            if (!verticallyAligned || !horizontallyAligned || !settled)
            {
                break;
            }

            Debug.Log($"AUTOMATED PLAYTEST: reached route waypoint {waypoints[waypointIndex].Order}.");
            waypointIndex++;
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
            completionMode = usesExitDoor ? "ExitDoor" : "RequiredCrystals",
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
