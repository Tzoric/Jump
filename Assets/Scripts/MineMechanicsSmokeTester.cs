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
        FallingSpike spike = FindFirstObjectByType<FallingSpike>();
        Rigidbody2D playerBody = health == null ? null : health.GetComponent<Rigidbody2D>();

        bool referencesPresent = health != null && weight != null && spike != null && playerBody != null;
        bool damagePassed = false;
        bool healingPassed = false;
        bool weightPassed = false;
        bool spikePassed = false;

        if (referencesPresent)
        {
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

            health.transform.position = spike.transform.position + Vector3.down * 3f;
            playerBody.linearVelocity = Vector2.zero;
            yield return new WaitForSeconds(0.6f);
            spikePassed = spike.IsTriggered;
        }

        bool passed = referencesPresent && damagePassed && healingPassed && weightPassed && spikePassed;
        string reportPath = ReadArgument("-mechanicsReport") ??
            Path.Combine(Application.dataPath, "..", "Logs", "MineMechanicsSmokeTest.json");
        var result = new SmokeResult
        {
            passed = passed,
            referencesPresent = referencesPresent,
            damagePassed = damagePassed,
            healingPassed = healingPassed,
            weightCalculationPassed = weightPassed,
            fallingSpikeTriggered = spikePassed
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
        public bool weightCalculationPassed;
        public bool fallingSpikeTriggered;
    }
}
