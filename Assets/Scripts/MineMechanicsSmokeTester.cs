using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;

public sealed class MineMechanicsSmokeTester : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void StartWhenRequested()
    {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), "-mineMechanicsSmokeTest") < 0)
        {
            return;
        }

        new GameObject("Mine Mechanics Smoke Tester").AddComponent<MineMechanicsSmokeTester>();
    }

    private IEnumerator Start()
    {
        yield return null;

        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        PlayerWeight weight = FindFirstObjectByType<PlayerWeight>();
        HeroMovement movement = FindFirstObjectByType<HeroMovement>();
        MinerOutfitVisual outfitVisual = FindFirstObjectByType<MinerOutfitVisual>();
        LevelExitDoor exitDoor = FindFirstObjectByType<LevelExitDoor>();
        MineRunInventory inventory = FindFirstObjectByType<MineRunInventory>();
        RewardChest chest = FindFirstObjectByType<RewardChest>();
        MineLevelMenuController levelMenu = FindFirstObjectByType<MineLevelMenuController>();
        AutomatedPlaytestWaypoint[] waypoints =
            FindObjectsByType<AutomatedPlaytestWaypoint>(FindObjectsSortMode.None);

        Rigidbody2D movementBody = movement == null ? null : movement.GetComponent<Rigidbody2D>();
        bool referencesPresent = health != null && weight != null && movement != null && movementBody != null &&
            outfitVisual != null && exitDoor != null && inventory != null && chest != null && levelMenu != null &&
            waypoints.Length == 11;
        bool damagePassed = false;
        bool damageVisualRestorationPassed = false;
        bool healingPassed = false;
        bool automatedJumpPassed = false;
        bool jumpAnticipationPassed = false;
        bool powerRunJumpPassed = false;
        bool controllerBindingsPassed = MineInput.GetBindableActions().Length == MineInput.BindableActionCount &&
            MineInput.GetDefaultControllerBindingPath(MineButtonAction.Run) == "<Gamepad>/buttonSouth" &&
            MineInput.GetDefaultControllerBindingPath(MineButtonAction.Jump) == "<Gamepad>/buttonEast" &&
            MineInput.GetDefaultControllerBindingPath(MineButtonAction.Interact) == "<Gamepad>/buttonWest" &&
            MineInput.GetDefaultControllerBindingPath(MineButtonAction.Potion) == "<Gamepad>/buttonNorth" &&
            MineInput.GetDefaultControllerBindingPath(MineButtonAction.Pause) == "<Gamepad>/start" &&
            MineInput.GetDefaultControllerBindingPath(MineButtonAction.Home) == "<Gamepad>/select";
        bool spikeHitboxShapePassed = VerifySpikeHitboxGeometry();
        bool pauseMenuPassed = false;
        bool deathActionLockPassed = false;
        bool weightPassed = false;
        bool exitConfiguredPassed = false;
        bool exitDoorInteractionPassed = false;
        bool gameOverProgressResetPassed = VerifyGameOverProgressReset();
        bool wallContactReleasePassed = false;
        bool emptyHeartDisplayPassed = false;
        bool chestInteractionPassed = false;
        bool pickRemovedPassed = outfitVisual != null && outfitVisual.HandPickaxe == null &&
            outfitVisual.Outfit != null && outfitVisual.Outfit.HandTool == null &&
            !FindObjectsByType<Transform>(FindObjectsSortMode.None).Any(transform =>
                transform.name.IndexOf("pick", StringComparison.OrdinalIgnoreCase) >= 0);

        if (referencesPresent)
        {
            float settleDeadline = Time.time + 2f;
            while ((!movement.IsGrounded || Mathf.Abs(movementBody.linearVelocityY) > .5f) &&
                   Time.time < settleDeadline)
            {
                yield return null;
            }

            float startingY = movement.transform.position.y;
            float highestY = startingY;
            movement.EnableAutomatedControl(true);
            movement.SetAutomatedInput(0f, true);
            bool showedGroundedSquat = false;
            float squatObservationDeadline = Time.realtimeSinceStartup + .2f;
            while (Time.realtimeSinceStartup < squatObservationDeadline && !showedGroundedSquat)
            {
                showedGroundedSquat = movement.IsPreparingJump &&
                    outfitVisual.CurrentAnimationRow == 2 && outfitVisual.CurrentAnimationFrame == 1 &&
                    Mathf.Abs(movementBody.linearVelocityY) <= .5f &&
                    Mathf.Abs(movement.transform.position.y - startingY) <= .08f;
                if (!showedGroundedSquat) yield return null;
            }

            // Releasing during anticipation must still produce a short hop; only the
            // variable-height hold portion is cancelled.
            movement.SetAutomatedInput(0f, false);
            bool launchedAfterSquat = false;
            bool showedRiseFrame = false;
            float launchDeadline = Time.time + .22f;
            while (Time.time < launchDeadline && !launchedAfterSquat)
            {
                highestY = Mathf.Max(highestY, movement.transform.position.y);
                if (movementBody.linearVelocityY > 1f)
                {
                    launchedAfterSquat = true;
                    float riseObservationDeadline = Time.realtimeSinceStartup + .12f;
                    while (Time.realtimeSinceStartup < riseObservationDeadline && !showedRiseFrame)
                    {
                        showedRiseFrame = outfitVisual.CurrentAnimationRow == 2 &&
                            outfitVisual.CurrentAnimationFrame == 2;
                        if (!showedRiseFrame) yield return null;
                    }
                    break;
                }
                yield return null;
            }

            float jumpTestEnds = Time.time + .35f;
            while (Time.time < jumpTestEnds)
            {
                highestY = Mathf.Max(highestY, movement.transform.position.y);
                yield return null;
            }

            float quickTapHeight = highestY - startingY;
            float landingDeadline = Time.time + 1.5f;
            while ((!movement.IsGrounded || Mathf.Abs(movementBody.linearVelocityY) > .5f) &&
                   Time.time < landingDeadline)
            {
                highestY = Mathf.Max(highestY, movement.transform.position.y);
                yield return null;
            }

            bool landedAfterQuickTap = movement.IsGrounded;
            float heldJumpStartY = movement.transform.position.y;
            float heldHighestY = heldJumpStartY;
            movement.SetAutomatedInput(0f, true);
            float heldInputEnds = Time.time + movement.JumpAnticipationSeconds + .20f;
            while (Time.time < heldInputEnds)
            {
                heldHighestY = Mathf.Max(heldHighestY, movement.transform.position.y);
                yield return null;
            }
            movement.SetAutomatedInput(0f, false);
            float heldObservationEnds = Time.time + .45f;
            while (Time.time < heldObservationEnds)
            {
                heldHighestY = Mathf.Max(heldHighestY, movement.transform.position.y);
                yield return null;
            }

            movement.EnableAutomatedControl(false);
            float heldJumpHeight = heldHighestY - heldJumpStartY;
            automatedJumpPassed = launchedAfterSquat && landedAfterQuickTap && quickTapHeight > .75f &&
                heldJumpHeight > quickTapHeight + .5f;
            jumpAnticipationPassed = showedGroundedSquat && launchedAfterSquat && showedRiseFrame;

            Vector2 beforePowerTestPosition = movementBody.position;
            Vector2 beforePowerTestVelocity = movementBody.linearVelocity;
            GameObject runway = new("Smoke Test Power Runway");
            runway.layer = LayerMask.NameToLayer("Ground");
            BoxCollider2D runwayCollider = runway.AddComponent<BoxCollider2D>();
            runwayCollider.size = new Vector2(40f, 1f);
            runway.transform.position = new Vector2(100f, 100f);
            movementBody.position = new Vector2(93f, 101.5f);
            movementBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            float runwaySettleDeadline = Time.time + 1f;
            while (!movement.IsGrounded && Time.time < runwaySettleDeadline) yield return null;

            movement.EnableAutomatedControl(true);
            movement.SetAutomatedInput(1f, false, false);
            float walkObservationEnds = Time.time + .18f;
            while (Time.time < walkObservationEnds) yield return null;
            bool ordinaryWalkObserved = !movement.IsRunning &&
                Mathf.Abs(Mathf.Abs(movementBody.linearVelocityX) - movement.WalkSpeed) <= .2f &&
                outfitVisual.CurrentAnimationRow == 0;

            movement.SetAutomatedInput(1f, false, true);
            float runObservationEnds = Time.time + .18f;
            while (Time.time < runObservationEnds) yield return null;
            bool runObserved = movement.IsRunning &&
                Mathf.Abs(Mathf.Abs(movementBody.linearVelocityX) - movement.RunSpeed) <= .2f &&
                outfitVisual.CurrentAnimationRow == 1;

            float powerJumpStartY = movement.transform.position.y;
            float powerJumpStartX = movement.transform.position.x;
            float powerHighestY = powerJumpStartY;
            movement.SetAutomatedInput(1f, true, true);
            bool committedPowerJump = false;
            bool powerJumpLaunched = false;
            float powerInputEnds = Time.time + movement.JumpAnticipationSeconds + movement.PowerJumpHoldSeconds;
            while (Time.time < powerInputEnds)
            {
                committedPowerJump |= movement.IsPreparingJump && movement.IsPowerJumping;
                powerJumpLaunched |= movementBody.linearVelocityY > movement.JumpForce + .5f;
                powerHighestY = Mathf.Max(powerHighestY, movement.transform.position.y);
                yield return null;
            }
            movement.SetAutomatedInput(1f, false, true);
            float powerObservationEnds = Time.time + .5f;
            while (Time.time < powerObservationEnds)
            {
                powerHighestY = Mathf.Max(powerHighestY, movement.transform.position.y);
                yield return null;
            }

            float powerJumpHeight = powerHighestY - powerJumpStartY;
            float powerJumpDistance = Mathf.Abs(movement.transform.position.x - powerJumpStartX);
            Debug.Log($"POWER RUN SMOKE: walk={ordinaryWalkObserved}, run={runObserved}, " +
                $"committed={committedPowerJump}, launched={powerJumpLaunched}, " +
                $"ordinaryHeldHeight={heldJumpHeight:0.00}, powerHeight={powerJumpHeight:0.00}, " +
                $"powerDistance={powerJumpDistance:0.00}.");
            powerRunJumpPassed = ordinaryWalkObserved && runObserved && committedPowerJump &&
                powerJumpLaunched && powerJumpHeight > heldJumpHeight + .5f && powerJumpDistance > 5f;
            movement.SetAutomatedInput(0f, false);
            movement.EnableAutomatedControl(false);
            movementBody.position = beforePowerTestPosition;
            movementBody.linearVelocity = beforePowerTestVelocity;
            Destroy(runway);
            Physics2D.SyncTransforms();

            Vector2 originalPosition = movementBody.position;
            Vector2 originalVelocity = movementBody.linearVelocity;
            GameObject wall = new("Smoke Test Vertical Wall");
            wall.layer = LayerMask.NameToLayer("Ground");
            BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
            wallCollider.size = new Vector2(1f, 14f);
            Vector2 contactStart = new(7.5f, 14f);
            Collider2D heroCollider = movement.GetComponentsInChildren<Collider2D>(true)
                .FirstOrDefault(collider => !collider.isTrigger);
            wall.transform.position = contactStart + new Vector2(
                heroCollider == null ? .6f : heroCollider.bounds.extents.x + .48f, 0f);
            movementBody.position = contactStart;
            movementBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            int healthBeforeWallTest = health.CurrentHealth;
            int respawnsBeforeWallTest = health.RespawnCount;
            movement.EnableAutomatedControl(true);
            movement.SetAutomatedInput(1f, false);
            float wallStartY = movementBody.position.y;
            float wallTestEnds = Time.time + .35f;
            while (Time.time < wallTestEnds) yield return null;
            wallContactReleasePassed = wallStartY - movementBody.position.y > .6f &&
                movementBody.linearVelocityY < -1f && health.CurrentHealth == healthBeforeWallTest &&
                health.RespawnCount == respawnsBeforeWallTest;
            movement.SetAutomatedInput(0f, false);
            movement.EnableAutomatedControl(false);
            movementBody.position = originalPosition;
            movementBody.linearVelocity = originalVelocity;
            Destroy(wall);
            Physics2D.SyncTransforms();

            int startingHealth = health.CurrentHealth;
            SpriteRenderer minerRenderer = outfitVisual.VisualRenderer;
            Color minerRestColor = minerRenderer == null ? Color.white : minerRenderer.color;
            damagePassed = health.TakeDamage(1, health.transform.position + Vector3.left) &&
                health.CurrentHealth == startingHealth - 1;
            bool damageFlashBecameVisible = minerRenderer != null &&
                minerRenderer.color.a < minerRestColor.a - .05f;
            emptyHeartDisplayPassed = health.HealthDisplaySupportsHeartGlyph &&
                health.HealthDisplayText.Contains("<color=#493B45>\u2665") &&
                !health.HealthDisplayText.Contains("\u2661");
            GameProgress.AddHealthPotion();
            int potionsBeforeUse = GameProgress.HealthPotions;
            healingPassed = health.TryUsePotion() && health.CurrentHealth == startingHealth &&
                GameProgress.HealthPotions == potionsBeforeUse - 1;

            // Deterministically reproduce the real boundary race: expire only the
            // immunity clock while the first coroutine is visibly in its low-alpha
            // wait, then accept another hit before that old wait can finish.
            bool boundaryWasTranslucent = damageFlashBecameVisible && minerRenderer != null &&
                minerRenderer.color.a < minerRestColor.a - .05f;
            bool expiryForced = ExpireInvulnerabilityForRegression(health);
            bool boundaryDamageAccepted = boundaryWasTranslucent && expiryForced &&
                health.TakeDamage(1, health.transform.position + Vector3.right);
            float flashRestoreDeadline = Time.time + 1.5f;
            while (health.IsInvulnerable && Time.time < flashRestoreDeadline) yield return null;
            yield return new WaitForSeconds(.12f);
            damageVisualRestorationPassed = damageFlashBecameVisible && boundaryDamageAccepted &&
                minerRenderer != null && minerRenderer.enabled && ColorsMatch(minerRenderer.color, minerRestColor);
            health.Heal(1);

            weight.SetCarriedWeight(2f);
            weight.SetWeightMultiplier(0.5f);
            weight.SetGravityMultiplier(0.5f);
            weightPassed = Mathf.Approximately(weight.ApparentWeight, 0.75f);
            weight.SetCarriedWeight(0f);
            weight.ResetPowerUpModifiers();

            exitConfiguredPassed = exitDoor.DestinationScene == "DungeonOverview" &&
                exitDoor.GetComponent<Collider2D>().enabled &&
                exitDoor.GetComponent<Collider2D>().isTrigger &&
                LevelExitDoor.ExitPrompt.Contains("UP") && LevelExitDoor.ExitPrompt.Contains("W");

            Vector2 beforeChestTest = movementBody.position;
            movement.EnableAutomatedControl(true);
            movement.SetAutomatedInput(0f, false);
            movementBody.position = chest.transform.position;
            movementBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            int crystalsBefore = GameProgress.Crystals;
            bool contactDidNotOpen = !chest.IsOpened && GameProgress.Crystals == crystalsBefore;
            bool lockedInteractionRejected = !chest.TryInteract(inventory, .25f) &&
                inventory.StatusText == RewardChest.LockedPrompt;

            inventory.CollectBronzeKey();
            bool openedWithKey = chest.TryInteract(inventory, .25f);
            Collider2D chestTrigger = chest.GetComponent<Collider2D>();
            SpriteRenderer chestRenderer = chest.GetComponent<SpriteRenderer>();
            bool awardedOnce = openedWithKey && GameProgress.Crystals == crystalsBefore + 5 &&
                chest.IsOpened && GameProgress.IsChestOpened(chest.LevelNumber) &&
                chest.OpenedSprite != null && chestRenderer.sprite == chest.OpenedSprite &&
                chestTrigger.enabled && chestTrigger.isTrigger;

            int crystalsAfterOpen = GameProgress.Crystals;
            bool replayInteractionRejected = !chest.TryInteract(inventory, .99f) &&
                GameProgress.Crystals == crystalsAfterOpen && inventory.StatusText == RewardChest.OpenedPrompt;

            GameObject replayChestObject = new("Smoke Test Replayed Chest");
            SpriteRenderer replayRenderer = replayChestObject.AddComponent<SpriteRenderer>();
            replayRenderer.sprite = chestRenderer.sprite;
            replayChestObject.AddComponent<BoxCollider2D>().isTrigger = true;
            RewardChest replayChest = replayChestObject.AddComponent<RewardChest>();
            replayChest.Configure(chest.LevelNumber, chest.OpenedSprite);
            bool replayVisualRestored = replayChest.IsOpened && replayRenderer.sprite == chest.OpenedSprite &&
                replayChestObject.GetComponent<Collider2D>().enabled;

            GameObject replayKeyObject = new("Smoke Test Replayed Bronze Key");
            replayKeyObject.AddComponent<CircleCollider2D>().isTrigger = true;
            BronzeKeyCollectible replayKey = replayKeyObject.AddComponent<BronzeKeyCollectible>();
            replayKey.Configure(chest.LevelNumber);
            bool collectedKeyStayedHidden = !replayKeyObject.activeSelf;

            chestInteractionPassed = contactDidNotOpen && lockedInteractionRejected && awardedOnce &&
                replayInteractionRejected && replayVisualRestored && collectedKeyStayedHidden;
            Destroy(replayChestObject);
            Destroy(replayKeyObject);
            movementBody.position = beforeChestTest;
            movementBody.linearVelocity = Vector2.zero;
            movement.EnableAutomatedControl(false);
            Physics2D.SyncTransforms();

            levelMenu.SetPaused(true);
            bool pausedStateApplied = MineLevelMenuController.IsPaused &&
                Mathf.Approximately(Time.timeScale, 0f) && levelMenu.PausePanel.activeSelf;
            levelMenu.ResumeGame();
            pauseMenuPassed = pausedStateApplied && !MineLevelMenuController.IsPaused &&
                Mathf.Approximately(Time.timeScale, 1f) && !levelMenu.PausePanel.activeSelf &&
                levelMenu.HomeScene == MineLevelMenuController.DefaultHomeScene;

            float damageLockDeadline = Time.time + 1.5f;
            while (health.IsInvulnerable && Time.time < damageLockDeadline) yield return null;
            int livesBeforeDeathLock = GameProgress.Lives;
            GameProgress.AddHealthPotion();
            int potionsBeforeDeathLock = GameProgress.HealthPotions;
            bool fatalDamageStarted = health.TakeDamage(health.CurrentHealth, health.transform.position);
            OverviewArrival.Clear();
            levelMenu.SetPaused(true);
            levelMenu.ReturnToOverview();
            bool actionsRejectedDuringDeath = fatalDamageStarted && health.IsRespawning && !health.CanAct &&
                !MineLevelMenuController.IsPaused && Mathf.Approximately(Time.timeScale, 1f) &&
                !OverviewArrival.IsShopRequested && !health.TryUsePotion() &&
                GameProgress.HealthPotions == potionsBeforeDeathLock;

            float respawnLockDeadline = Time.time + 2f;
            while (health.IsRespawning && Time.time < respawnLockDeadline) yield return null;
            yield return null;
            bool lifeWasConsumed = GameProgress.Lives == livesBeforeDeathLock - 1;
            bool respawnVisualRestored = minerRenderer != null && minerRenderer.enabled &&
                ColorsMatch(minerRenderer.color, minerRestColor);
            bool potionCleanedUp = GameProgress.ConsumePotion();
            GameProgress.AddLife();
            deathActionLockPassed = actionsRejectedDuringDeath && !health.IsRespawning && health.CanAct &&
                lifeWasConsumed && respawnVisualRestored && potionCleanedUp &&
                GameProgress.Lives == livesBeforeDeathLock;

            movement.EnableAutomatedControl(true);
            movement.SetAutomatedInput(0f, false);
            movementBody.position = (Vector2)exitDoor.transform.position + Vector2.down * .85f;
            movementBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            float doorContactDeadline = Time.time + 2f;
            while ((!exitDoor.IsPlayerNearby || !movement.IsGrounded) && Time.time < doorContactDeadline)
            {
                yield return null;
            }

            bool doorContactOnlyPrompted = exitDoor.IsPlayerNearby && !exitDoor.IsUsed &&
                inventory.StatusText == LevelExitDoor.ExitPrompt;
            bool explicitDoorInteractionStarted = exitDoor.TryInteract(movement);
            Collider2D heroBodyCollider = movement.GetComponent<Collider2D>();
            exitDoorInteractionPassed = doorContactOnlyPrompted && explicitDoorInteractionStarted &&
                exitDoor.IsUsed && movementBody.bodyType == RigidbodyType2D.Kinematic &&
                heroBodyCollider != null && !heroBodyCollider.enabled;
        }

        bool passed = referencesPresent && automatedJumpPassed && jumpAnticipationPassed && damagePassed &&
            damageVisualRestorationPassed &&
            healingPassed && weightPassed && exitConfiguredPassed && gameOverProgressResetPassed &&
            exitDoorInteractionPassed && wallContactReleasePassed && emptyHeartDisplayPassed &&
            pickRemovedPassed && chestInteractionPassed && powerRunJumpPassed && controllerBindingsPassed &&
            pauseMenuPassed && deathActionLockPassed && spikeHitboxShapePassed;
        string reportPath = ReadArgument("-mechanicsReport") ??
            Path.Combine(Application.dataPath, "..", "Logs", "MineMechanicsSmokeTest.json");
        var result = new SmokeResult
        {
            passed = passed,
            referencesPresent = referencesPresent,
            damagePassed = damagePassed,
            damageVisualRestorationPassed = damageVisualRestorationPassed,
            healingPassed = healingPassed,
            automatedJumpPassed = automatedJumpPassed,
            jumpAnticipationPassed = jumpAnticipationPassed,
            powerRunJumpPassed = powerRunJumpPassed,
            controllerBindingsPassed = controllerBindingsPassed,
            spikeHitboxShapePassed = spikeHitboxShapePassed,
            pauseMenuPassed = pauseMenuPassed,
            deathActionLockPassed = deathActionLockPassed,
            weightCalculationPassed = weightPassed,
            exitDoorConfigured = exitConfiguredPassed,
            exitDoorInteractionPassed = exitDoorInteractionPassed,
            gameOverProgressResetPassed = gameOverProgressResetPassed,
            wallContactReleasePassed = wallContactReleasePassed,
            emptyHeartDisplayPassed = emptyHeartDisplayPassed,
            pickRemovedPassed = pickRemovedPassed,
            chestInteractionPassed = chestInteractionPassed,
            waypointCount = waypoints.Length
        };

        string directory = Path.GetDirectoryName(reportPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(reportPath, JsonUtility.ToJson(result, true));
        Debug.Log($"MINE MECHANICS SMOKE TEST {(passed ? "PASSED" : "FAILED")}");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.Exit(passed ? 0 : 3);
#else
        Application.Quit(passed ? 0 : 3);
#endif
    }

    private static string ReadArgument(string name)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(arguments, name);
        return index >= 0 && index + 1 < arguments.Length ? arguments[index + 1] : null;
    }

    private static bool ColorsMatch(Color actual, Color expected)
    {
        return Mathf.Abs(actual.r - expected.r) <= .001f &&
            Mathf.Abs(actual.g - expected.g) <= .001f &&
            Mathf.Abs(actual.b - expected.b) <= .001f &&
            Mathf.Abs(actual.a - expected.a) <= .001f;
    }

    private static bool ExpireInvulnerabilityForRegression(PlayerHealth health)
    {
        FieldInfo deadline = typeof(PlayerHealth).GetField("invulnerableUntil",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (health == null || deadline == null) return false;

        deadline.SetValue(health, Time.time - .01f);
        return !health.IsInvulnerable;
    }

    private static bool VerifySpikeHitboxGeometry()
    {
        GameObject spike = new("Smoke Test Three-Tooth Spike Hitbox");
        spike.transform.position = new Vector3(-500f,-500f,0f);
        spike.transform.rotation = Quaternion.Euler(0f,0f,18f);
        spike.transform.localScale = new Vector3(1.5f,1.25f,1f);
        PolygonCollider2D polygon = SpikeHitboxGeometry.AddCollider(spike);
        Physics2D.SyncTransforms();

        bool shapeMatches = polygon.pathCount == SpikeHitboxGeometry.PathCount;
        for (int path = 0; path < SpikeHitboxGeometry.PathCount && shapeMatches; path++)
        {
            Vector2[] points = polygon.GetPath(path);
            shapeMatches = points.Length == SpikeHitboxGeometry.PointsPerPath;
            for (int point = 0; point < points.Length && shapeMatches; point++)
            {
                shapeMatches = Vector2.Distance(points[point],
                    SpikeHitboxGeometry.ExpectedPoint(path,point)) <= .002f;
            }
        }

        float[] toothCenters = { -.5208333f, .0208333f, .5625f };
        float[] transparentValleys = { -.25f, .2916667f };
        bool teethHit = toothCenters.All(center =>
            polygon.OverlapPoint(spike.transform.TransformPoint(new Vector2(center,0f))));
        bool valleysClear = transparentValleys.All(valley =>
            !polygon.OverlapPoint(spike.transform.TransformPoint(new Vector2(valley,0f))));
        UnityEngine.Object.Destroy(spike);
        return shapeMatches && teethHit && valleysClear;
    }

    private static bool VerifyGameOverProgressReset()
    {
        GameProgress.AddCrystals(100);
        GameProgress.BuyHealthPotion();
        GameProgress.BuyHeartUpgrade();
        GameProgress.AddLife(4);
        GameProgress.CompleteLevel(9);
        GameProgress.CollectSilverKey();

        for (int levelNumber = 1; levelNumber <= 12; levelNumber++)
        {
            GameProgress.CollectBronzeKey(levelNumber);
            GameProgress.MarkChestOpened(levelNumber);
        }

        GameProgress.RestartAfterGameOver();

        if (GameProgress.Lives != GameProgress.StartingLives ||
            GameProgress.Crystals != 0 ||
            GameProgress.HealthPotions != 0 ||
            GameProgress.MaxHearts != GameProgress.BaseHearts ||
            GameProgress.HighestUnlockedLevel != 2 ||
            GameProgress.HasSilverKey)
        {
            return false;
        }

        for (int levelNumber = 1; levelNumber <= 12; levelNumber++)
        {
            if (GameProgress.HasBronzeKey(levelNumber) || GameProgress.IsChestOpened(levelNumber))
            {
                return false;
            }
        }

        return true;
    }

    [Serializable]
    private sealed class SmokeResult
    {
        public bool passed;
        public bool referencesPresent;
        public bool damagePassed;
        public bool damageVisualRestorationPassed;
        public bool healingPassed;
        public bool automatedJumpPassed;
        public bool jumpAnticipationPassed;
        public bool powerRunJumpPassed;
        public bool controllerBindingsPassed;
        public bool spikeHitboxShapePassed;
        public bool pauseMenuPassed;
        public bool deathActionLockPassed;
        public bool weightCalculationPassed;
        public bool exitDoorConfigured;
        public bool exitDoorInteractionPassed;
        public bool gameOverProgressResetPassed;
        public bool wallContactReleasePassed;
        public bool emptyHeartDisplayPassed;
        public bool pickRemovedPassed;
        public bool chestInteractionPassed;
        public int waypointCount;
    }
}
