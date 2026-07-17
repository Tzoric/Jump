using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public sealed class MineShopController : MonoBehaviour
{
    public const string PlaytestKeyboardCode = "MINER";
    public const int PlaytestControllerSequenceLength = 10;

    private const float SecretSequenceTimeout = 5f;
    private const float ControllerEngageThreshold = .65f;
    private const float ControllerReleaseThreshold = .35f;
    private const float ReturnedMessageSeconds = 3f;
    private static readonly Vector2Int[] PlaytestControllerSequence =
    {
        Vector2Int.up, Vector2Int.up, Vector2Int.down, Vector2Int.down,
        Vector2Int.left, Vector2Int.right, Vector2Int.left, Vector2Int.right,
        Vector2Int.down, Vector2Int.up
    };

    [SerializeField] private GameObject levelPanel;
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private TextMeshProUGUI balanceDisplay;
    [SerializeField] private TextMeshProUGUI statusDisplay;
    [SerializeField] private GameObject levelDefaultSelection;
    [SerializeField] private GameObject shopDefaultSelection;
    [SerializeField] private GameObject controlsDefaultSelection;

    private GameObject pendingSelection;
    private int keyboardSecretStep;
    private int controllerSecretStep;
    private float lastKeyboardSecretInput;
    private float lastControllerSecretInput;
    private float returnedMessageUntil = -1f;
    private bool waitingForControllerNeutral;

    public GameObject LevelPanel => levelPanel;
    public GameObject ShopPanel => shopPanel;
    public GameObject ControlsPanel => controlsPanel;
    public GameObject ControlsDefaultSelection => controlsDefaultSelection;
    public TextMeshProUGUI BalanceDisplay => balanceDisplay;
    public bool IsShopVisible => shopPanel != null && shopPanel.activeSelf;
    public bool IsControlsVisible => controlsPanel != null && controlsPanel.activeSelf;

    private void OnEnable()
    {
        GameProgress.EndPlaytestRun();
        if (GameProgress.Lives <= 0)
        {
            OverviewArrival.Clear();
            SceneManager.LoadScene("GameOver");
            return;
        }

        if (OverviewArrival.ConsumeShopRequest()) ShowShop();
        else Refresh();
    }

    private void Start() => SelectPending();

    private void Update()
    {
        if (!GameProgress.PlaytestAccessEnabled && returnedMessageUntil > 0f &&
            Time.unscaledTime >= returnedMessageUntil)
        {
            returnedMessageUntil = -1f;
            Refresh();
        }

        if (levelPanel == null || !levelPanel.activeSelf || MineInput.IsRebinding)
        {
            ResetSecretInput();
            return;
        }

        foreach (char character in Input.inputString)
        {
            SubmitPlaytestKeyboardCharacter(character);
        }

        ReadControllerSecret();
    }

    private void OnDisable()
    {
        ResetSecretInput();
    }

    public void Configure(GameObject levels, GameObject shop, GameObject controls,
        TextMeshProUGUI balance, TextMeshProUGUI status,
        GameObject firstLevelSelection = null, GameObject firstShopSelection = null,
        GameObject firstControlsSelection = null)
    {
        levelPanel = levels;
        shopPanel = shop;
        controlsPanel = controls;
        balanceDisplay = balance;
        statusDisplay = status;
        levelDefaultSelection = firstLevelSelection;
        shopDefaultSelection = firstShopSelection;
        controlsDefaultSelection = firstControlsSelection;
        Refresh();
    }

    public void ShowLevels()
    {
        levelPanel.SetActive(true);
        shopPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        Refresh();
        Select(levelDefaultSelection);
    }

    public void ShowShop()
    {
        levelPanel.SetActive(false);
        shopPanel.SetActive(true);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        statusDisplay.text = "Spend crystals collected in the shafts.";
        Refresh();
        Select(shopDefaultSelection);
    }

    public void ShowControls()
    {
        levelPanel.SetActive(false);
        shopPanel.SetActive(false);
        controlsPanel.SetActive(true);
        Select(controlsDefaultSelection);
    }

    public void BuyExtraLife() => Report(GameProgress.BuyExtraLife(), "Extra life purchased.");
    public void BuyHealthPotion() => Report(GameProgress.BuyHealthPotion(),
        $"Health potion purchased. Press {MineInput.GetControllerBindingDisplayName(MineButtonAction.Potion)} or H in a level to use it.");
    public void BuyHeartUpgrade() => Report(GameProgress.BuyHeartUpgrade(), "Permanent heart upgrade purchased.");

    public bool SubmitPlaytestKeyboardCharacter(char character)
    {
        float now = Time.unscaledTime;
        if (keyboardSecretStep > 0 && now - lastKeyboardSecretInput > SecretSequenceTimeout)
            keyboardSecretStep = 0;
        lastKeyboardSecretInput = now;

        char upper = char.ToUpperInvariant(character);
        char expected = PlaytestKeyboardCode[keyboardSecretStep];
        keyboardSecretStep = upper == expected
            ? keyboardSecretStep + 1
            : upper == PlaytestKeyboardCode[0] ? 1 : 0;
        if (keyboardSecretStep < PlaytestKeyboardCode.Length) return false;

        keyboardSecretStep = 0;
        TogglePlaytestAccess();
        return true;
    }

    public bool SubmitPlaytestControllerDirection(Vector2Int direction)
    {
        float now = Time.unscaledTime;
        if (controllerSecretStep > 0 && now - lastControllerSecretInput > SecretSequenceTimeout)
            controllerSecretStep = 0;
        lastControllerSecretInput = now;

        Vector2Int expected = PlaytestControllerSequence[controllerSecretStep];
        controllerSecretStep = direction == expected
            ? controllerSecretStep + 1
            : direction == PlaytestControllerSequence[0] ? 1 : 0;
        if (controllerSecretStep < PlaytestControllerSequence.Length) return false;

        controllerSecretStep = 0;
        TogglePlaytestAccess();
        return true;
    }

    public static Vector2Int GetPlaytestControllerSequenceStep(int index)
    {
        return PlaytestControllerSequence[Mathf.Clamp(index, 0, PlaytestControllerSequence.Length - 1)];
    }

    public bool TogglePlaytestAccess()
    {
        bool enabled = GameProgress.TogglePlaytestAccess();
        returnedMessageUntil = enabled ? -1f : Time.unscaledTime + ReturnedMessageSeconds;
        Refresh();
        foreach (MineLevelSelectButton node in GetComponentsInChildren<MineLevelSelectButton>(true))
            node.Refresh();
        Debug.Log(enabled
            ? "PLAYTEST EASTER EGG: Foreman's Master Key enabled; all twelve tunnels are open in a save-safe sandbox."
            : "PLAYTEST EASTER EGG: Foreman's Master Key returned; story locks restored.");
        return enabled;
    }

    private void Report(bool success, string message)
    {
        statusDisplay.text = success ? message : "Not enough green crystals.";
        Refresh();
    }

    private void Refresh()
    {
        if (balanceDisplay != null)
        {
            if (GameProgress.PlaytestAccessEnabled)
            {
                balanceDisplay.text = $"FOREMAN'S MASTER KEY - ALL 12 TUNNELS OPEN     GEMS  {GameProgress.Crystals}     LIVES  {GameProgress.Lives}     POTIONS  {GameProgress.HealthPotions}";
            }
            else if (returnedMessageUntil > Time.unscaledTime)
            {
                balanceDisplay.text = "MASTER KEY RETURNED - STORY LOCKS RESTORED";
            }
            else
            {
                balanceDisplay.text = $"GREEN CRYSTALS  {GameProgress.Crystals}     LIVES  {GameProgress.Lives}     POTIONS  {GameProgress.HealthPotions}";
            }
        }
    }

    private void ReadControllerSecret()
    {
        Vector2 input = MineInput.ControllerMoveVector;
        if (waitingForControllerNeutral)
        {
            if (Mathf.Abs(input.x) <= ControllerReleaseThreshold &&
                Mathf.Abs(input.y) <= ControllerReleaseThreshold)
                waitingForControllerNeutral = false;
            return;
        }

        Vector2Int direction = Vector2Int.zero;
        if (Mathf.Abs(input.x) >= ControllerEngageThreshold ||
            Mathf.Abs(input.y) >= ControllerEngageThreshold)
        {
            direction = Mathf.Abs(input.x) > Mathf.Abs(input.y)
                ? new Vector2Int(input.x > 0f ? 1 : -1, 0)
                : new Vector2Int(0, input.y > 0f ? 1 : -1);
        }

        if (direction == Vector2Int.zero) return;
        SubmitPlaytestControllerDirection(direction);
        waitingForControllerNeutral = true;
    }

    private void ResetSecretInput()
    {
        keyboardSecretStep = 0;
        controllerSecretStep = 0;
        waitingForControllerNeutral = false;
    }

    private void Select(GameObject selection)
    {
        pendingSelection = selection;
        SelectPending();
    }

    private void SelectPending()
    {
        if (pendingSelection != null && EventSystem.current != null && pendingSelection.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(pendingSelection);
        }
    }
}
