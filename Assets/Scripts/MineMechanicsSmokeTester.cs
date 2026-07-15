using System;
using System.Collections;
using System.IO;
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
        AutomatedPlaytestWaypoint[] waypoints =
            FindObjectsByType<AutomatedPlaytestWaypoint>(FindObjectsSortMode.None);

        Rigidbody2D movementBody = movement == null ? null : movement.GetComponent<Rigidbody2D>();
        bool referencesPresent = health != null && weight != null && movement != null && movementBody != null &&
            outfitVisual != null && exitDoor != null && waypoints.Length == 11;
        bool damagePassed = false;
        bool healingPassed = false;
        bool automatedJumpPassed = false;
        bool jumpAnticipationPassed = false;
        bool weightPassed = false;
        bool exitConfiguredPassed = false;

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

            int startingHealth = health.CurrentHealth;
            damagePassed = health.TakeDamage(1, health.transform.position + Vector3.left) &&
                health.CurrentHealth == startingHealth - 1;
            health.Heal(1);
            healingPassed = health.CurrentHealth == startingHealth;

            weight.SetCarriedWeight(2f);
            weight.SetWeightMultiplier(0.5f);
            weight.SetGravityMultiplier(0.5f);
            weightPassed = Mathf.Approximately(weight.ApparentWeight, 0.75f);
            weight.SetCarriedWeight(0f);
            weight.ResetPowerUpModifiers();

            exitConfiguredPassed = exitDoor.DestinationScene == "DungeonOverview" &&
                exitDoor.GetComponent<Collider2D>().isTrigger;
        }

        bool passed = referencesPresent && automatedJumpPassed && jumpAnticipationPassed && damagePassed &&
            healingPassed && weightPassed && exitConfiguredPassed;
        string reportPath = ReadArgument("-mechanicsReport") ??
            Path.Combine(Application.dataPath, "..", "Logs", "MineMechanicsSmokeTest.json");
        var result = new SmokeResult
        {
            passed = passed,
            referencesPresent = referencesPresent,
            damagePassed = damagePassed,
            healingPassed = healingPassed,
            automatedJumpPassed = automatedJumpPassed,
            jumpAnticipationPassed = jumpAnticipationPassed,
            weightCalculationPassed = weightPassed,
            exitDoorConfigured = exitConfiguredPassed,
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

    [Serializable]
    private sealed class SmokeResult
    {
        public bool passed;
        public bool referencesPresent;
        public bool damagePassed;
        public bool healingPassed;
        public bool automatedJumpPassed;
        public bool jumpAnticipationPassed;
        public bool weightCalculationPassed;
        public bool exitDoorConfigured;
        public int waypointCount;
    }
}
