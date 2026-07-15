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
        LevelExitDoor exitDoor = FindFirstObjectByType<LevelExitDoor>();
        AutomatedPlaytestWaypoint[] waypoints =
            FindObjectsByType<AutomatedPlaytestWaypoint>(FindObjectsSortMode.None);

        bool referencesPresent = health != null && weight != null && movement != null && exitDoor != null &&
            waypoints.Length == 11;
        bool damagePassed = false;
        bool healingPassed = false;
        bool automatedJumpPassed = false;
        bool weightPassed = false;
        bool exitConfiguredPassed = false;

        if (referencesPresent)
        {
            yield return null;
            float startingY = movement.transform.position.y;
            float highestY = startingY;
            movement.EnableAutomatedControl(true);
            movement.SetAutomatedInput(0f, true);
            float jumpTestEnds = Time.time + 0.3f;
            while (Time.time < jumpTestEnds)
            {
                highestY = Mathf.Max(highestY, movement.transform.position.y);
                yield return null;
            }
            movement.SetAutomatedInput(0f, false);
            movement.EnableAutomatedControl(false);
            automatedJumpPassed = highestY > startingY + 0.75f;

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

        bool passed = referencesPresent && automatedJumpPassed && damagePassed && healingPassed && weightPassed &&
            exitConfiguredPassed;
        string reportPath = ReadArgument("-mechanicsReport") ??
            Path.Combine(Application.dataPath, "..", "Logs", "MineMechanicsSmokeTest.json");
        var result = new SmokeResult
        {
            passed = passed,
            referencesPresent = referencesPresent,
            damagePassed = damagePassed,
            healingPassed = healingPassed,
            automatedJumpPassed = automatedJumpPassed,
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
        public bool weightCalculationPassed;
        public bool exitDoorConfigured;
        public int waypointCount;
    }
}
