using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public sealed class MineControlsController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI controllerDisplay;
    [SerializeField] private TextMeshProUGUI statusDisplay;
    [SerializeField] private Button runButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button interactButton;
    [SerializeField] private Button potionButton;
    [SerializeField] private Button pauseButton;
    [SerializeField] private Button homeButton;
    [SerializeField] private Button restoreDefaultsButton;

    private Coroutine pendingRebind;
    private InputSystemUIInputModule uiInputModule;
    private MineButtonAction pendingAction;
    private float nextControllerRefresh;

    public TextMeshProUGUI ControllerDisplay => controllerDisplay;
    public TextMeshProUGUI StatusDisplay => statusDisplay;
    public Button RunButton => runButton;
    public Button JumpButton => jumpButton;
    public Button InteractButton => interactButton;
    public Button PotionButton => potionButton;
    public Button PauseButton => pauseButton;
    public Button HomeButton => homeButton;
    public Button RestoreDefaultsButton => restoreDefaultsButton;
    public bool IsListening => pendingRebind != null || MineInput.IsRebinding;

    public void Configure(TextMeshProUGUI controllerName, TextMeshProUGUI status,
        Button run, Button jump, Button interact, Button potion, Button pause, Button home,
        Button restoreDefaults)
    {
        controllerDisplay = controllerName;
        statusDisplay = status;
        runButton = run;
        jumpButton = jump;
        interactButton = interact;
        potionButton = potion;
        pauseButton = pause;
        homeButton = home;
        restoreDefaultsButton = restoreDefaults;
        RefreshLabels();
    }

    private void OnEnable()
    {
        MineInput.BindingsChanged += RefreshLabels;
        MineInput.SelectMostRecentlyUsedController();
        RefreshLabels();
    }

    private void OnDisable()
    {
        MineInput.BindingsChanged -= RefreshLabels;
        if (pendingRebind != null)
        {
            StopCoroutine(pendingRebind);
            pendingRebind = null;
        }
        MineInput.CancelInteractiveRebind();
        RestoreUiInput();
        SetButtonsInteractable(true);
    }

    private void Update()
    {
        if (IsListening || Time.unscaledTime < nextControllerRefresh) return;
        nextControllerRefresh = Time.unscaledTime + .25f;
        if (MineInput.SelectMostRecentlyUsedController()) RefreshLabels();
    }

    public void RebindRun() => BeginRebind(MineButtonAction.Run);
    public void RebindJump() => BeginRebind(MineButtonAction.Jump);
    public void RebindInteract() => BeginRebind(MineButtonAction.Interact);
    public void RebindPotion() => BeginRebind(MineButtonAction.Potion);
    public void RebindPause() => BeginRebind(MineButtonAction.Pause);
    public void RebindHome() => BeginRebind(MineButtonAction.Home);

    public void RestoreDefaults()
    {
        MineInput.CancelInteractiveRebind();
        MineInput.SelectMostRecentlyUsedController();
        MineInput.ResetCurrentControllerBindings();
        if (statusDisplay != null)
            statusDisplay.text = "DEFAULT CONTROLLER BUTTONS RESTORED FOR THIS CONTROLLER.";
        Select(runButton);
    }

    public Button GetButton(MineButtonAction action) => action switch
    {
        MineButtonAction.Run => runButton,
        MineButtonAction.Jump => jumpButton,
        MineButtonAction.Interact => interactButton,
        MineButtonAction.Potion => potionButton,
        MineButtonAction.Pause => pauseButton,
        MineButtonAction.Home => homeButton,
        _ => null
    };

    public void RefreshLabels()
    {
        if (controllerDisplay != null)
            controllerDisplay.text = $"ACTIVE CONTROLLER: {MineInput.ConnectedControllerName}";
        SetButtonLabel(runButton, MineButtonAction.Run);
        SetButtonLabel(jumpButton, MineButtonAction.Jump);
        SetButtonLabel(interactButton, MineButtonAction.Interact);
        SetButtonLabel(potionButton, MineButtonAction.Potion);
        SetButtonLabel(pauseButton, MineButtonAction.Pause);
        SetButtonLabel(homeButton, MineButtonAction.Home);
    }

    private void BeginRebind(MineButtonAction action)
    {
        if (IsListening) return;
        pendingAction = action;
        if (statusDisplay != null)
        {
            statusDisplay.text = $"RELEASE BUTTONS, THEN PRESS A NEW BUTTON FOR {MineInput.GetActionName(action)}.  ESC CANCELS.";
        }
        SetButtonsInteractable(false);
        pendingRebind = StartCoroutine(BeginAfterSubmitRelease(action));
    }

    private IEnumerator BeginAfterSubmitRelease(MineButtonAction action)
    {
        yield return null;
        MineInput.SelectMostRecentlyUsedController();
        while (MineInput.AnyControllerButtonPressed) yield return null;

        uiInputModule = FindFirstObjectByType<InputSystemUIInputModule>();
        if (uiInputModule != null) uiInputModule.enabled = false;
        pendingRebind = null;
        if (MineInput.BeginInteractiveRebind(action, FinishRebind, CancelRebind)) yield break;

        RestoreUiInput();
        SetButtonsInteractable(true);
        if (statusDisplay != null)
            statusDisplay.text = "CONNECT A CONTROLLER, THEN SELECT THE ACTION AGAIN.";
        Select(GetButton(action));
    }

    private void FinishRebind(MineRebindResult result)
    {
        RestoreUiInput();
        SetButtonsInteractable(true);
        RefreshLabels();
        if (statusDisplay != null)
        {
            statusDisplay.text = result.SwappedAction.HasValue
                ? $"{MineInput.GetActionName(result.Action)} IS NOW {result.DisplayName}; " +
                  $"{MineInput.GetActionName(result.SwappedAction.Value)} WAS SWAPPED TO KEEP BUTTONS UNIQUE."
                : $"{MineInput.GetActionName(result.Action)} IS NOW {result.DisplayName}.";
        }
        Select(GetButton(result.Action));
    }

    private void CancelRebind()
    {
        RestoreUiInput();
        SetButtonsInteractable(true);
        RefreshLabels();
        if (statusDisplay != null) statusDisplay.text = "BUTTON MAPPING CANCELLED OR TIMED OUT.";
        Select(GetButton(pendingAction));
    }

    private void RestoreUiInput()
    {
        if (uiInputModule != null) uiInputModule.enabled = true;
        uiInputModule = null;
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (runButton != null) runButton.interactable = interactable;
        if (jumpButton != null) jumpButton.interactable = interactable;
        if (interactButton != null) interactButton.interactable = interactable;
        if (potionButton != null) potionButton.interactable = interactable;
        if (pauseButton != null) pauseButton.interactable = interactable;
        if (homeButton != null) homeButton.interactable = interactable;
        if (restoreDefaultsButton != null) restoreDefaultsButton.interactable = interactable;
    }

    private static void SetButtonLabel(Button button, MineButtonAction action)
    {
        if (button == null) return;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (label != null)
        {
            label.text = $"{MineInput.GetActionName(action)}  -  {MineInput.GetControllerBindingDisplayName(action)}";
        }
    }

    private static void Select(Button button)
    {
        if (button != null && EventSystem.current != null && button.gameObject.activeInHierarchy)
            EventSystem.current.SetSelectedGameObject(button.gameObject);
    }
}
