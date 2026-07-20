using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Reflection;
using TMPro;
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
        LevelEntranceDoor entranceDoor = FindFirstObjectByType<LevelEntranceDoor>();
        LevelExitDoor exitDoor = FindFirstObjectByType<LevelExitDoor>();
        MineRunInventory inventory = FindFirstObjectByType<MineRunInventory>();
        BronzeKeyCollectible authoredKey = FindFirstObjectByType<BronzeKeyCollectible>();
        RewardChest chest = FindFirstObjectByType<RewardChest>();
        MineLevelMenuController levelMenu = FindFirstObjectByType<MineLevelMenuController>();
        MidLevelShopController midLevelShop = FindFirstObjectByType<MidLevelShopController>();
        ParachuteDescentController parachute = FindFirstObjectByType<ParachuteDescentController>();
        HangGliderVisualController gliderVisual = FindFirstObjectByType<HangGliderVisualController>();
        AutomatedPlaytestWaypoint[] waypoints =
            FindObjectsByType<AutomatedPlaytestWaypoint>(FindObjectsSortMode.None);

        Rigidbody2D movementBody = movement == null ? null : movement.GetComponent<Rigidbody2D>();
        bool referencesPresent = health != null && weight != null && movement != null && movementBody != null &&
            outfitVisual != null && entranceDoor != null && exitDoor != null && inventory != null &&
            authoredKey != null && chest != null && levelMenu != null && midLevelShop != null &&
            parachute != null && gliderVisual != null && waypoints.Length >= 11 && health.MaxHealth == 7;
        MineDoorAnimator entranceDoorAnimator = entranceDoor == null
            ? null
            : entranceDoor.GetComponentInChildren<MineDoorAnimator>(true);
        MineDoorAnimator exitDoorAnimator = exitDoor == null
            ? null
            : exitDoor.GetComponentInChildren<MineDoorAnimator>(true);
        bool entranceDoorPassed = referencesPresent && entranceDoor.Hero == movement &&
            entranceDoor.IsComplete && entranceDoor.EntranceSeconds >= .5f &&
            entranceDoor.GetComponentsInChildren<Collider2D>(true).Length == 0 &&
            Vector2.Distance(entranceDoor.GameplayPosition, movement.transform.position) <= .1f &&
            DoorAnimatorConfigured(entranceDoorAnimator) && entranceDoorAnimator.HasOpened &&
            !entranceDoorAnimator.IsOpen;
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
            MineInput.GetDefaultControllerBindingPath(MineButtonAction.Home) == "<Gamepad>/select" &&
            MineInput.GetActionName(MineButtonAction.Jump) == "JUMP" &&
            MineInput.GetActionName(MineButtonAction.Interact).StartsWith("INTERACT", StringComparison.Ordinal) &&
            (MineInput.GetActionName(MineButtonAction.Interact).Contains("PARACHUTE") ||
             MineInput.GetActionName(MineButtonAction.Interact).Contains("GLIDER"));
        bool parachuteInputSeparationPassed = false;
        bool gliderVerticalModesPassed = false;
        bool gliderVisualStatesPassed = false;
        bool spikeHitboxShapePassed = VerifySpikeHitboxGeometry();
        bool pauseMenuPassed = false;
        bool midLevelShopPassed = false;
        bool deathActionLockPassed = false;
        bool weightPassed = false;
        bool exitConfiguredPassed = false;
        bool exitDoorInteractionPassed = false;
        bool gameOverProgressResetPassed = VerifyGameOverProgressReset();
        bool playtestUnlockEasterEggPassed = VerifyPlaytestUnlockEasterEgg();
        bool scopedCountedInventoryPassed = VerifyScopedCountedInventory();
        bool playtestCheatsPassed = false;
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

            Vector2 beforeParachutePosition = movementBody.position;
            Vector2 beforeParachuteVelocity = movementBody.linearVelocity;
            movementBody.position = new Vector2(120f, 120f);
            movementBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            movement.EnableAutomatedControl(true);
            parachute.ResetDescentState();
            movement.SetAutomatedInput(0f, false, false, false);
            yield return null;
            yield return null;

            movement.SetAutomatedInput(0f, true, false, false);
            yield return null;
            yield return null;
            bool jumpDidNotDeploy = !parachute.IsDeployed;

            movement.SetAutomatedInput(0f, false, false, false);
            yield return null;
            movement.SetAutomatedInput(0f, false, false, true);
            yield return null;
            yield return null;
            bool deployedAnywhere = parachute.IsDeploymentRequested && parachute.IsDeployed &&
                !parachute.IsInLaunchArea && !parachute.IsInDescentZone;

            movement.SetAutomatedInput(0f, false, false, true, 1f);
            yield return null;
            yield return null;
            bool hoverVisual = gliderVisual.CurrentState == HangGliderVisualState.Hover &&
                gliderVisual.GliderRenderer.sprite == gliderVisual.HoverFrontSprite &&
                !gliderVisual.GliderRenderer.flipX &&
                gliderVisual.HasAlignedGripAnchor && gliderVisual.GripAnchorError <= .002f &&
                outfitVisual.IsFlightPoseActive &&
                outfitVisual.CurrentPerspective == MinerOutfitVisual.Perspective.TowardCamera &&
                outfitVisual.CurrentAnimationRow == 3;
            int hoverStartFrame = gliderVisual.CurrentFrame;
            float flapUntil = Time.unscaledTime + .25f;
            while (Time.unscaledTime < flapUntil) yield return null;
            bool wingFlapped = gliderVisual.CurrentFrame != hoverStartFrame ||
                Mathf.Abs(gliderVisual.WingFlexAmount) > .002f;

            movementBody.linearVelocity = Vector2.zero;
            movement.SetAutomatedInput(0f, false, false, true, 0f);
            yield return null;
            yield return null;
            bool floatVisual = gliderVisual.CurrentState == HangGliderVisualState.Float &&
                gliderVisual.GliderRenderer.sprite == gliderVisual.FloatRightSprite &&
                !gliderVisual.GliderRenderer.flipX &&
                gliderVisual.HasAlignedGripAnchor && gliderVisual.GripAnchorError <= .002f &&
                outfitVisual.CurrentPerspective == MinerOutfitVisual.Perspective.Side &&
                outfitVisual.CurrentAnimationFrame == 3;

            movement.SetAutomatedInput(0f, false, false, true, -1f);
            yield return null;
            yield return null;
            bool diveVisual = gliderVisual.CurrentState == HangGliderVisualState.Dive &&
                gliderVisual.GliderRenderer.sprite == gliderVisual.DiveRightSprite &&
                !gliderVisual.GliderRenderer.flipX &&
                gliderVisual.HasAlignedGripAnchor && gliderVisual.GripAnchorError <= .002f &&
                outfitVisual.CurrentAnimationFrame == 4;

            movement.SetAutomatedInput(-1f, false, false, true, 0f);
            yield return null;
            yield return null;
            bool leftVisual = gliderVisual.CurrentState == HangGliderVisualState.GlideLeft &&
                gliderVisual.GliderRenderer.sprite == gliderVisual.BankRightSprite &&
                gliderVisual.IsFacingLeft && gliderVisual.GliderRenderer.flipX &&
                gliderVisual.HasAlignedGripAnchor && gliderVisual.GripAnchorError <= .002f;

            movement.SetAutomatedInput(1f, false, false, true, 0f);
            yield return null;
            yield return null;
            bool rightVisual = gliderVisual.CurrentState == HangGliderVisualState.GlideRight &&
                gliderVisual.GliderRenderer.sprite == gliderVisual.BankRightSprite &&
                !gliderVisual.IsFacingLeft && !gliderVisual.GliderRenderer.flipX &&
                gliderVisual.HasAlignedGripAnchor && gliderVisual.GripAnchorError <= .002f;
            gliderVisualStatesPassed = gliderVisual.HasCompleteDirectionalArt && hoverVisual &&
                floatVisual && diveVisual && leftVisual && rightVisual && wingFlapped;

            movement.SetAutomatedInput(0f, false, false, true, 0f);
            gliderVerticalModesPassed = VerifyGliderVerticalPhysics(parachute, movementBody);

            parachute.EnterDescentZone(120f);
            movementBody.linearVelocity = new Vector2(movementBody.linearVelocity.x, -1f);
            yield return null;
            bool zoneOnlyChangedCamera = parachute.IsDeployed && parachute.IsInDescentZone &&
                parachute.IsCameraTrackingDescent && Mathf.Approximately(parachute.DescentCenterX, 120f);
            parachute.ExitDescentZone();
            yield return null;
            bool leavingZoneKeptGlider = parachute.IsDeployed && !parachute.IsInDescentZone &&
                !parachute.IsCameraTrackingDescent;

            movement.SetAutomatedInput(0f, false, false, false);
            yield return null;
            movement.SetAutomatedInput(0f, false, false, true);
            yield return null;
            bool secondPressClosedGlider = !parachute.IsDeployed && !parachute.IsDeploymentRequested;
            gliderVisualStatesPassed &= gliderVisual.CurrentState == HangGliderVisualState.Stowed &&
                !gliderVisual.GliderRenderer.enabled && !outfitVisual.IsFlightPoseActive &&
                outfitVisual.CurrentPerspective == MinerOutfitVisual.Perspective.Side;

            parachute.ResetDescentState();
            yield return null;
            bool resetReleasedJump = !parachute.IsDeployed && !parachute.IsInDescentZone &&
                !movement.IsJumpSuppressed;
            parachuteInputSeparationPassed = jumpDidNotDeploy && deployedAnywhere &&
                zoneOnlyChangedCamera && leavingZoneKeptGlider && secondPressClosedGlider &&
                resetReleasedJump;
            if (!parachuteInputSeparationPassed)
            {
                Debug.LogWarning($"Glider toggle diagnostics: jumpDidNotDeploy={jumpDidNotDeploy}, " +
                    $"deployedAnywhere={deployedAnywhere}, zoneOnlyChangedCamera={zoneOnlyChangedCamera}, " +
                    $"leavingZoneKeptGlider={leavingZoneKeptGlider}, " +
                    $"secondPressClosedGlider={secondPressClosedGlider}, resetReleasedJump={resetReleasedJump}.");
            }
            movement.SetAutomatedInput(0f, false);
            movement.EnableAutomatedControl(false);
            movementBody.position = beforeParachutePosition;
            movementBody.linearVelocity = beforeParachuteVelocity;
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
            int keysBeforeOpen = inventory.KeyCount;
            bool openedWithKey = chest.TryInteract(inventory, .25f);
            Collider2D chestTrigger = chest.GetComponent<Collider2D>();
            SpriteRenderer chestRenderer = chest.GetComponent<SpriteRenderer>();
            bool awardedOnce = openedWithKey && GameProgress.Crystals == crystalsBefore + 5 &&
                keysBeforeOpen == 1 && inventory.KeyCount == 0 && chest.IsOpened &&
                GameProgress.IsChestOpened(chest.DungeonId, chest.LevelNumber, chest.ChestId) &&
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
            replayChest.Configure(chest.DungeonId, chest.LevelNumber, chest.ChestId, chest.OpenedSprite);
            bool replayVisualRestored = replayChest.IsOpened && replayRenderer.sprite == chest.OpenedSprite &&
                replayChestObject.GetComponent<Collider2D>().enabled;

            GameObject replayKeyObject = new("Smoke Test Replayed Bronze Key");
            replayKeyObject.AddComponent<CircleCollider2D>().isTrigger = true;
            BronzeKeyCollectible replayKey = replayKeyObject.AddComponent<BronzeKeyCollectible>();
            replayKey.Configure(authoredKey.DungeonId, authoredKey.LevelNumber, authoredKey.PickupId);
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

            Vector3 beforeShopPosition = movement.transform.position;
            levelMenu.OpenShop();
            bool shopOpenedInPlace = MineLevelMenuController.IsPaused && levelMenu.IsShopVisible &&
                midLevelShop.IsShopVisible && midLevelShop.ShopPanel.activeSelf &&
                !levelMenu.PausePanel.activeSelf && Mathf.Approximately(Time.timeScale, 0f) &&
                movement.transform.position == beforeShopPosition;
            levelMenu.CloseShop();
            midLevelShopPassed = shopOpenedInPlace && !MineLevelMenuController.IsPaused &&
                !levelMenu.IsShopVisible && !midLevelShop.ShopPanel.activeSelf &&
                Mathf.Approximately(Time.timeScale, 1f) &&
                movement.transform.position == beforeShopPosition;

            float damageLockDeadline = Time.time + 1.5f;
            while (health.IsInvulnerable && Time.time < damageLockDeadline) yield return null;
            int livesBeforeDeathLock = GameProgress.Lives;
            GameProgress.AddHealthPotion();
            int potionsBeforeDeathLock = GameProgress.HealthPotions;
            bool ordinaryHitStarted = health.TakeDamage(1, health.transform.position);
            bool fatalDamageStarted = ordinaryHitStarted && health.IsInvulnerable && health.KillFromFall();
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

            playtestCheatsPassed = VerifyPlaytestCheats(health);

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
                heroBodyCollider != null && !heroBodyCollider.enabled &&
                DoorAnimatorConfigured(exitDoorAnimator) &&
                (exitDoorAnimator.IsAnimating || exitDoorAnimator.HasOpened);
        }

        bool passed = referencesPresent && entranceDoorPassed && automatedJumpPassed && jumpAnticipationPassed && damagePassed &&
            damageVisualRestorationPassed &&
            healingPassed && weightPassed && exitConfiguredPassed && gameOverProgressResetPassed &&
            playtestUnlockEasterEggPassed &&
            scopedCountedInventoryPassed && playtestCheatsPassed && gliderVerticalModesPassed &&
            gliderVisualStatesPassed &&
            exitDoorInteractionPassed && wallContactReleasePassed && emptyHeartDisplayPassed &&
            pickRemovedPassed && chestInteractionPassed && powerRunJumpPassed && controllerBindingsPassed &&
            parachuteInputSeparationPassed && pauseMenuPassed && midLevelShopPassed &&
            deathActionLockPassed && spikeHitboxShapePassed;
        string reportPath = ReadArgument("-mechanicsReport") ??
            Path.Combine(Application.dataPath, "..", "Logs", "MineMechanicsSmokeTest.json");
        var result = new SmokeResult
        {
            passed = passed,
            referencesPresent = referencesPresent,
            entranceDoorPassed = entranceDoorPassed,
            damagePassed = damagePassed,
            damageVisualRestorationPassed = damageVisualRestorationPassed,
            healingPassed = healingPassed,
            automatedJumpPassed = automatedJumpPassed,
            jumpAnticipationPassed = jumpAnticipationPassed,
            powerRunJumpPassed = powerRunJumpPassed,
            controllerBindingsPassed = controllerBindingsPassed,
            parachuteInputSeparationPassed = parachuteInputSeparationPassed,
            gliderVerticalModesPassed = gliderVerticalModesPassed,
            gliderVisualStatesPassed = gliderVisualStatesPassed,
            spikeHitboxShapePassed = spikeHitboxShapePassed,
            pauseMenuPassed = pauseMenuPassed,
            midLevelShopPassed = midLevelShopPassed,
            deathActionLockPassed = deathActionLockPassed,
            weightCalculationPassed = weightPassed,
            exitDoorConfigured = exitConfiguredPassed,
            exitDoorInteractionPassed = exitDoorInteractionPassed,
            gameOverProgressResetPassed = gameOverProgressResetPassed,
            playtestUnlockEasterEggPassed = playtestUnlockEasterEggPassed,
            scopedCountedInventoryPassed = scopedCountedInventoryPassed,
            playtestCheatsPassed = playtestCheatsPassed,
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

    private static bool VerifyGliderVerticalPhysics(ParachuteDescentController glider,
        Rigidbody2D body)
    {
        if (glider == null || body == null || !glider.IsDeployed) return false;

        PropertyInfo verticalProperty = typeof(ParachuteDescentController).GetProperty(
            nameof(ParachuteDescentController.GliderVerticalInput),
            BindingFlags.Instance | BindingFlags.Public);
        FieldInfo verticalBackingField = typeof(ParachuteDescentController).GetField(
            $"<{nameof(ParachuteDescentController.GliderVerticalInput)}>k__BackingField",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo fixedUpdate = typeof(ParachuteDescentController).GetMethod("FixedUpdate",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (verticalProperty == null || verticalBackingField == null || fixedUpdate == null)
            return false;

        float originalGravity = body.gravityScale;
        Vector2 originalVelocity = body.linearVelocity;

        verticalBackingField.SetValue(glider, 1f);
        body.linearVelocity = new Vector2(0f, -3f);
        fixedUpdate.Invoke(glider, null);
        float hoverVelocity = body.linearVelocityY;
        bool upHovered = glider.IsHovering && Mathf.Approximately(body.gravityScale, 0f) &&
            hoverVelocity > -3f;

        verticalBackingField.SetValue(glider, 0f);
        body.linearVelocity = new Vector2(0f, -50f);
        fixedUpdate.Invoke(glider, null);
        float neutralVelocity = body.linearVelocityY;
        bool neutralGlided = Mathf.Abs(neutralVelocity + glider.DeployedTerminalSpeed) <= .05f &&
            body.gravityScale > 0f && body.gravityScale < glider.FastDescentGravity;

        verticalBackingField.SetValue(glider, -1f);
        body.linearVelocity = new Vector2(0f, -50f);
        fixedUpdate.Invoke(glider, null);
        float downVelocity = body.linearVelocityY;
        bool downDescendedFaster = Mathf.Abs(downVelocity + glider.FastDescentTerminalSpeed) <= .05f &&
            downVelocity < neutralVelocity &&
            Mathf.Approximately(body.gravityScale, glider.FastDescentGravity);

        verticalBackingField.SetValue(glider, 0f);
        body.gravityScale = originalGravity;
        body.linearVelocity = originalVelocity;
        return upHovered && neutralGlided && downDescendedFaster;
    }

    private static bool DoorAnimatorConfigured(MineDoorAnimator animator)
    {
        return animator != null && animator.ClosedRenderer != null && animator.OpenRenderer != null &&
            animator.ClosedRenderer != animator.OpenRenderer &&
            animator.ClosedRenderer.sprite != null && animator.OpenRenderer.sprite != null &&
            animator.OpeningSeconds >= .2f;
    }

    private static bool VerifyScopedCountedInventory()
    {
        GameProgress.SetPlaytestAccess(false);
        GameProgress.SetPlaytestAccess(true);
        if (!GameProgress.BeginPlaytestRun())
        {
            GameProgress.SetPlaytestAccess(false);
            return false;
        }

        const string dungeon = GameProgress.SilverDungeonId;
        const int level = 1;
        const string keyA = "mechanics-smoke-silver-key-a";
        const string keyB = "mechanics-smoke-silver-key-b";
        const string chestA = "mechanics-smoke-silver-chest-a";
        const string chestB = "mechanics-smoke-silver-chest-b";
        const string chestC = "mechanics-smoke-silver-chest-c";
        int bronzeBefore = GameProgress.GetKeyCount(GameProgress.BronzeDungeonId, level);

        bool firstCollected = GameProgress.TryCollectKey(dungeon, level, keyA);
        bool secondCollected = GameProgress.TryCollectKey(dungeon, level, keyB);
        bool duplicateRejected = !GameProgress.TryCollectKey(dungeon, level, keyA);
        bool countedTwo = GameProgress.GetKeyCount(dungeon, level) == 2;
        bool firstOpened = GameProgress.TryUnlockChest(dungeon, level, chestA) &&
            GameProgress.GetKeyCount(dungeon, level) == 1 &&
            GameProgress.IsChestOpened(dungeon, level, chestA);
        bool replayRejected = !GameProgress.TryUnlockChest(dungeon, level, chestA) &&
            GameProgress.GetKeyCount(dungeon, level) == 1;
        bool secondOpened = GameProgress.TryUnlockChest(dungeon, level, chestB) &&
            GameProgress.GetKeyCount(dungeon, level) == 0 &&
            GameProgress.IsChestOpened(dungeon, level, chestB);
        bool noFreeThirdChest = !GameProgress.TryUnlockChest(dungeon, level, chestC) &&
            !GameProgress.IsChestOpened(dungeon, level, chestC);
        bool bronzeScopeUnaffected = GameProgress.GetKeyCount(GameProgress.BronzeDungeonId, level) ==
            bronzeBefore;

        GameProgress.EndPlaytestRun();
        GameProgress.SetPlaytestAccess(false);
        return firstCollected && secondCollected && duplicateRejected && countedTwo &&
            firstOpened && replayRejected && secondOpened && noFreeThirdChest && bronzeScopeUnaffected;
    }

    private static bool VerifyPlaytestCheats(PlayerHealth health)
    {
        if (health == null) return false;
        GameProgress.SetPlaytestAccess(false);
        GameProgress.SetPlaytestAccess(true);
        if (!GameProgress.BeginPlaytestRun())
        {
            GameProgress.SetPlaytestAccess(false);
            return false;
        }

        PlaytestCheatController cheats = FindFirstObjectByType<PlaytestCheatController>();
        if (cheats == null)
        {
            GameProgress.EndPlaytestRun();
            GameProgress.SetPlaytestAccess(false);
            return false;
        }

        MethodInfo acceptCharacter = typeof(PlaytestCheatController).GetMethod("AcceptCharacter",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (acceptCharacter == null)
        {
            GameProgress.EndPlaytestRun();
            GameProgress.SetPlaytestAccess(false);
            return false;
        }

        health.RestoreToFullHealth();
        ExpireInvulnerabilityForRegression(health);
        bool damageAccepted = health.TakeDamage(1, health.transform.position);
        foreach (char character in "HEALTH") acceptCharacter.Invoke(cheats, new object[] { character });
        bool healthRestored = damageAccepted && health.CurrentHealth == health.MaxHealth &&
            cheats.PendingCommand.Length == 0;

        int livesBefore = GameProgress.Lives;
        foreach (char character in "LIFE") acceptCharacter.Invoke(cheats, new object[] { character });
        bool livesGranted = GameProgress.Lives == livesBefore + 10 && cheats.PendingCommand.Length == 0;

        GameProgress.EndPlaytestRun();
        GameProgress.SetPlaytestAccess(false);
        health.RestoreToFullHealth();
        health.RefreshHud();
        return healthRestored && livesGranted;
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
        const string silverResetKey = "mechanics-smoke-reset-silver-key";
        const string silverResetChest = "mechanics-smoke-reset-silver-chest";
        GameProgress.TryCollectKey(GameProgress.SilverDungeonId, 1, silverResetKey);
        GameProgress.TryUnlockChest(GameProgress.SilverDungeonId, 1, silverResetChest);

        GameProgress.RestartAfterGameOver();

        if (GameProgress.Lives != GameProgress.StartingLives ||
            GameProgress.Crystals != 0 ||
            GameProgress.HealthPotions != 0 ||
            GameProgress.MaxHearts != GameProgress.BaseHearts ||
            GameProgress.HighestUnlockedLevel != 2 ||
            GameProgress.HasSilverKey || GameProgress.PlaytestAccessEnabled ||
            GameProgress.IsPlaytestRunActive ||
            GameProgress.GetKeyCount(GameProgress.SilverDungeonId, 1) != 0 ||
            GameProgress.IsKeyPickupCollected(GameProgress.SilverDungeonId, 1, silverResetKey) ||
            GameProgress.IsChestOpened(GameProgress.SilverDungeonId, 1, silverResetChest))
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

    private static bool VerifyPlaytestUnlockEasterEgg()
    {
        GameProgress.SetPlaytestAccess(false);
        int savedCrystals = GameProgress.Crystals;
        int savedLives = GameProgress.Lives;
        int savedPotions = GameProgress.HealthPotions;
        int savedHearts = GameProgress.MaxHearts;
        int savedHighestLevel = GameProgress.HighestUnlockedLevel;
        bool savedSilverKey = GameProgress.HasSilverKey;
        bool savedBronzeKey = GameProgress.HasBronzeKey(11);
        bool savedChest = GameProgress.IsChestOpened(11);

        GameObject probeObject = new("Smoke Test Foreman's Master Key");
        probeObject.SetActive(false);
        MineShopController probe = probeObject.AddComponent<MineShopController>();
        GameObject balanceObject = new("Smoke Test Master Key Banner",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        balanceObject.transform.SetParent(probeObject.transform, false);
        TextMeshProUGUI masterKeyBanner = balanceObject.GetComponent<TextMeshProUGUI>();
        probe.Configure(null, null, null, masterKeyBanner, null);

        GameObject levelElevenObject = new("Smoke Test Locked Level 11",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.Button));
        MineLevelSelectButton levelElevenNode = levelElevenObject.AddComponent<MineLevelSelectButton>();
        levelElevenNode.Configure(11, "Level11_TreasureVein", "Treasure Vein", null);
        UnityEngine.UI.Button levelElevenButton = levelElevenObject.GetComponent<UnityEngine.UI.Button>();
        bool levelElevenInitiallyLocked = !levelElevenButton.interactable;

        bool keyboardTriggered = false;
        foreach (char character in MineShopController.PlaytestKeyboardCode)
            keyboardTriggered |= probe.SubmitPlaytestKeyboardCharacter(character);
        bool keyboardUnlockedEveryLevel = keyboardTriggered && GameProgress.PlaytestAccessEnabled &&
            Enumerable.Range(1, GameProgress.MaxMineLevel).All(GameProgress.IsLevelUnlocked) &&
            !GameProgress.IsLevelUnlocked(0) && !GameProgress.IsLevelUnlocked(13) &&
            levelElevenInitiallyLocked && levelElevenButton.interactable &&
            masterKeyBanner.text.Contains("MASTER KEY") &&
            GameProgress.HighestUnlockedLevel == savedHighestLevel &&
            GameProgress.HasSilverKey == savedSilverKey;

        bool keyboardReturnedKey = false;
        foreach (char character in MineShopController.PlaytestKeyboardCode)
            keyboardReturnedKey |= probe.SubmitPlaytestKeyboardCharacter(character);
        bool storyLocksRestored = keyboardReturnedKey && !GameProgress.PlaytestAccessEnabled &&
            !GameProgress.IsLevelUnlocked(11) && !GameProgress.IsLevelUnlocked(12) &&
            !levelElevenButton.interactable && masterKeyBanner.text.Contains("RETURNED");

        bool controllerTriggered = false;
        for (int index = 0; index < MineShopController.PlaytestControllerSequenceLength; index++)
            controllerTriggered |= probe.SubmitPlaytestControllerDirection(
                MineShopController.GetPlaytestControllerSequenceStep(index));
        bool controllerUnlockedEveryLevel = controllerTriggered && GameProgress.PlaytestAccessEnabled &&
            Enumerable.Range(1, GameProgress.MaxMineLevel).All(GameProgress.IsLevelUnlocked) &&
            levelElevenButton.interactable;

        bool sandboxStarted = GameProgress.BeginPlaytestRun();
        GameProgress.AddCrystals(100);
        bool sandboxLifePurchase = GameProgress.BuyExtraLife();
        bool sandboxPotionPurchase = GameProgress.BuyHealthPotion();
        bool sandboxHeartPurchase = GameProgress.BuyHeartUpgrade();
        GameProgress.ConsumeLife();
        GameProgress.CollectSilverKey();
        GameProgress.CollectBronzeKey(11);
        GameProgress.MarkChestOpened(11);
        GameProgress.CompleteLevel(11);
        bool sandboxBehavedNormally = sandboxStarted && GameProgress.IsPlaytestRunActive &&
            sandboxLifePurchase && sandboxPotionPurchase && sandboxHeartPurchase &&
            GameProgress.Crystals == savedCrystals + 47 && GameProgress.Lives == savedLives &&
            GameProgress.HealthPotions == savedPotions + 1 && GameProgress.MaxHearts == savedHearts + 1 &&
            GameProgress.HighestUnlockedLevel == GameProgress.MaxMineLevel && GameProgress.HasSilverKey &&
            GameProgress.HasBronzeKey(11) && GameProgress.IsChestOpened(11);

        GameProgress.EndPlaytestRun();
        bool sandboxDiscarded = !GameProgress.IsPlaytestRunActive &&
            GameProgress.Crystals == savedCrystals && GameProgress.Lives == savedLives &&
            GameProgress.HealthPotions == savedPotions && GameProgress.MaxHearts == savedHearts &&
            GameProgress.HighestUnlockedLevel == savedHighestLevel &&
            GameProgress.HasSilverKey == savedSilverKey &&
            GameProgress.HasBronzeKey(11) == savedBronzeKey &&
            GameProgress.IsChestOpened(11) == savedChest;

        bool failedRunSandboxStarted = GameProgress.BeginPlaytestRun();
        GameProgress.AddCrystals(5);
        GameProgress.RestartAfterGameOver();
        bool failedRunDiscardedSafely = failedRunSandboxStarted && !GameProgress.IsPlaytestRunActive &&
            GameProgress.PlaytestAccessEnabled && GameProgress.Crystals == savedCrystals &&
            GameProgress.Lives == savedLives;

        GameProgress.SetPlaytestAccess(false);
        UnityEngine.Object.Destroy(probeObject);
        UnityEngine.Object.Destroy(levelElevenObject);
        return keyboardUnlockedEveryLevel && storyLocksRestored && controllerUnlockedEveryLevel &&
            sandboxBehavedNormally && sandboxDiscarded && failedRunDiscardedSafely;
    }

    [Serializable]
    private sealed class SmokeResult
    {
        public bool passed;
        public bool referencesPresent;
        public bool entranceDoorPassed;
        public bool damagePassed;
        public bool damageVisualRestorationPassed;
        public bool healingPassed;
        public bool automatedJumpPassed;
        public bool jumpAnticipationPassed;
        public bool powerRunJumpPassed;
        public bool controllerBindingsPassed;
        public bool parachuteInputSeparationPassed;
        public bool gliderVerticalModesPassed;
        public bool gliderVisualStatesPassed;
        public bool spikeHitboxShapePassed;
        public bool pauseMenuPassed;
        public bool midLevelShopPassed;
        public bool deathActionLockPassed;
        public bool weightCalculationPassed;
        public bool exitDoorConfigured;
        public bool exitDoorInteractionPassed;
        public bool gameOverProgressResetPassed;
        public bool playtestUnlockEasterEggPassed;
        public bool scopedCountedInventoryPassed;
        public bool playtestCheatsPassed;
        public bool wallContactReleasePassed;
        public bool emptyHeartDisplayPassed;
        public bool pickRemovedPassed;
        public bool chestInteractionPassed;
        public int waypointCount;
    }
}
