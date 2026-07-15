using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class AutomatedPlaytester : MonoBehaviour
{
    private const float DefaultTimeoutSeconds = 120f;
    private const float FailureHeight = -30f;

    private HeroMovement hero;
    private float startedAt;
    private float jumpReleaseAt;
    private float nextJumpAt;
    private int initialCollectibleCount;
    private string reportPath;
    private float timeoutSeconds;
    private float nextProgressLogAt;
    private Vector3 lastMovingPosition;
    private float lastMovedAt;
    private float escapeUntil;
    private float escapeDirection;

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
        FindHeroAndBegin();
    }

    private void Update()
    {
        if (hero == null)
        {
            FindHeroAndBegin();
            return;
        }

        List<GameObject> collectibles = FindCollectibles();
        if (collectibles.Count == 0)
        {
            Finish(true, "Collected every required crystal.");
            return;
        }

        if (Time.unscaledTime - startedAt > timeoutSeconds)
        {
            Finish(false, "The virtual controller timed out before collecting every required crystal. " +
                "This is not proof that the level is impossible.");
            return;
        }

        if (hero.transform.position.y < FailureHeight)
        {
            Finish(false, $"The player fell below y={FailureHeight}.");
            return;
        }

        Transform target = ChooseTarget(collectibles);
        if (Time.unscaledTime >= nextProgressLogAt)
        {
            Debug.Log($"AUTOMATED PLAYTEST: {collectibles.Count} remain; player={hero.transform.position}; " +
                $"target={target.name} at {target.position}; grounded={hero.IsGrounded}.");
            nextProgressLogAt = Time.unscaledTime + 5f;
        }
        float horizontalDistance = target.position.x - hero.transform.position.x;
        float verticalDistance = target.position.y - hero.transform.position.y;
        float horizontal = Mathf.Abs(verticalDistance) > 0.75f && Mathf.Abs(horizontalDistance) < 0.3f
            ? 1f
            : (Mathf.Abs(horizontalDistance) < 0.15f ? 0f : Mathf.Sign(horizontalDistance));

        if (Vector3.Distance(hero.transform.position, lastMovingPosition) > 0.25f)
        {
            lastMovingPosition = hero.transform.position;
            lastMovedAt = Time.unscaledTime;
        }
        else if (Time.unscaledTime - lastMovedAt > 1.5f)
        {
            escapeDirection = horizontal <= 0f ? 1f : -1f;
            // The playtest runs at 4x speed, so this short real-time pulse is
            // roughly a half-second controller correction in game time.
            escapeUntil = Time.unscaledTime + 0.15f;
            lastMovedAt = Time.unscaledTime;
            Debug.Log($"AUTOMATED PLAYTEST: stuck recovery moving {escapeDirection}.");
        }

        if (Time.unscaledTime < escapeUntil)
        {
            horizontal = escapeDirection;
        }

        if (hero.IsGrounded && Time.unscaledTime >= nextJumpAt)
        {
            jumpReleaseAt = Time.unscaledTime + 0.18f;
            nextJumpAt = Time.unscaledTime + 0.45f;
        }

        bool holdJump = Time.unscaledTime < jumpReleaseAt;
        hero.SetAutomatedInput(horizontal, holdJump);
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
        lastMovedAt = Time.unscaledTime;
        List<GameObject> collectibles = FindCollectibles();
        initialCollectibleCount = collectibles.Count;
        if (initialCollectibleCount == 0)
        {
            Finish(false, "The scene has no BlueCrystal or BlackBigCrystal objects to use as completion goals.");
        }
    }

    private Transform ChooseTarget(List<GameObject> collectibles)
    {
        float lowestHeight = float.PositiveInfinity;
        foreach (GameObject collectible in collectibles)
        {
            lowestHeight = Mathf.Min(lowestHeight, collectible.transform.position.y);
        }

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

    private void Finish(bool passed, string message)
    {
        hero?.SetAutomatedInput(0f, false);
        PlayerHealth health = hero == null ? null : hero.GetComponent<PlayerHealth>();
        var result = new PlaytestResult
        {
            scene = SceneManager.GetActiveScene().path,
            passed = passed,
            message = message,
            startingCollectibles = initialCollectibleCount,
            collectedBlueCrystals = hero == null ? 0 : hero.BlueCrystalCount,
            collectedBlackCrystals = hero == null ? 0 : hero.BlackBigCrystalCount,
            finalHealth = health == null ? 0 : health.CurrentHealth,
            respawns = health == null ? 0 : health.RespawnCount,
            elapsedRealSeconds = Time.unscaledTime - startedAt,
            finalPlayerPosition = hero == null ? Vector3.zero : hero.transform.position
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
        public bool passed;
        public string message;
        public int startingCollectibles;
        public int collectedBlueCrystals;
        public int collectedBlackCrystals;
        public int finalHealth;
        public int respawns;
        public float elapsedRealSeconds;
        public Vector3 finalPlayerPosition;
    }
}
