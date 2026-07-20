using UnityEngine;

/// <summary>
/// Keyboard-only quality-of-life commands for MINER playtest runs. This object
/// is created automatically and remains inert outside the in-memory playtest
/// sandbox, so typed commands can never alter a normal save.
/// </summary>
[DefaultExecutionOrder(-1000)]
public sealed class PlaytestCheatController : MonoBehaviour
{
    private const string HealthCommand = "HEALTH";
    private const string LifeCommand = "LIFE";
    private const float CommandTimeoutSeconds = 2f;

    private static PlaytestCheatController instance;
    private static int potionSuppressionFrame = -1;

    private string commandBuffer = string.Empty;
    private float lastCharacterAt;

    /// <summary>
    /// H starts the HEALTH command during a MINER run. Expose its one-frame
    /// suppression centrally so PlayerHealth cannot spend a potion first.
    /// </summary>
    public static bool SuppressPotionThisFrame =>
        GameProgress.IsPlaytestRunActive &&
        (potionSuppressionFrame == Time.frameCount || Input.GetKeyDown(KeyCode.H));

    public string PendingCommand => commandBuffer;
    public float CommandTimeout => CommandTimeoutSeconds;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        instance = null;
        potionSuppressionFrame = -1;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null) return;
        var host = new GameObject(nameof(PlaytestCheatController));
        instance = host.AddComponent<PlaytestCheatController>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
    }

    private void Update()
    {
        if (!GameProgress.IsPlaytestRunActive)
        {
            ClearPendingCommand();
            return;
        }

        if (Input.GetKeyDown(KeyCode.H)) potionSuppressionFrame = Time.frameCount;

        if (commandBuffer.Length > 0 &&
            Time.unscaledTime - lastCharacterAt > CommandTimeoutSeconds)
        {
            ClearPendingCommand();
        }

        string typed = Input.inputString;
        for (int index = 0; index < typed.Length; index++)
        {
            char character = char.ToUpperInvariant(typed[index]);
            if (character < 'A' || character > 'Z') continue;
            AcceptCharacter(character);
        }
    }

    private void AcceptCharacter(char character)
    {
        lastCharacterAt = Time.unscaledTime;
        string candidate = commandBuffer + character;

        if (candidate == HealthCommand)
        {
            ExecuteHealthCommand();
            ClearPendingCommand();
            return;
        }

        if (candidate == LifeCommand)
        {
            ExecuteLifeCommand();
            ClearPendingCommand();
            return;
        }

        commandBuffer = LongestCommandPrefixSuffix(candidate);
    }

    private static string LongestCommandPrefixSuffix(string candidate)
    {
        int maximumLength = Mathf.Min(candidate.Length,
            Mathf.Max(HealthCommand.Length, LifeCommand.Length));
        for (int length = maximumLength; length > 0; length--)
        {
            string suffix = candidate.Substring(candidate.Length - length, length);
            if (HealthCommand.StartsWith(suffix, System.StringComparison.Ordinal) ||
                LifeCommand.StartsWith(suffix, System.StringComparison.Ordinal))
            {
                return suffix;
            }
        }

        return string.Empty;
    }

    private static void ExecuteHealthCommand()
    {
        if (!GameProgress.IsPlaytestRunActive) return;
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        if (health == null) return;
        health.RestoreToFullHealth();
        health.RefreshHud();
        MineRunInventory inventory = health.GetComponent<MineRunInventory>() ??
            FindFirstObjectByType<MineRunInventory>();
        inventory?.ShowMessage("PLAYTEST: HEALTH RESTORED");
    }

    private static void ExecuteLifeCommand()
    {
        if (!GameProgress.IsPlaytestRunActive) return;
        GameProgress.AddLife(10);
        PlayerHealth health = FindFirstObjectByType<PlayerHealth>();
        health?.RefreshHud();
        MineRunInventory inventory = health == null
            ? FindFirstObjectByType<MineRunInventory>()
            : health.GetComponent<MineRunInventory>();
        inventory?.ShowMessage("PLAYTEST: +10 LIVES");
    }

    private void ClearPendingCommand()
    {
        commandBuffer = string.Empty;
        lastCharacterAt = 0f;
    }
}
