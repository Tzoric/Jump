using TMPro;
using UnityEngine;

public sealed class ParachuteInstructionDisplay : MonoBehaviour
{
    [SerializeField] private ParachuteDescentController parachute;
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    public ParachuteDescentController Parachute => parachute;
    public GameObject PromptPanel => promptPanel;
    public TextMeshProUGUI PromptText => promptText;

    public void Configure(ParachuteDescentController controller, GameObject panel,
        TextMeshProUGUI text)
    {
        parachute = controller;
        promptPanel = panel;
        promptText = text;
        if (Application.isPlaying) RefreshText();
        RefreshVisibility();
    }

    private void OnEnable()
    {
        MineInput.BindingsChanged += RefreshText;
        RefreshText();
        RefreshVisibility();
    }

    private void OnDisable()
    {
        MineInput.BindingsChanged -= RefreshText;
        if (promptPanel != null) promptPanel.SetActive(false);
    }

    private void Update()
    {
        RefreshText();
        RefreshVisibility();
    }

    public void RefreshText()
    {
        if (promptText == null) return;
        string interact = MineInput.GetControllerBindingDisplayName(MineButtonAction.Interact);
        string jump = MineInput.GetControllerBindingDisplayName(MineButtonAction.Jump);
        if (parachute != null && parachute.IsDeployed)
        {
            promptText.text =
                $"PARACHUTE OPEN\nSTEER WITH LEFT STICK / D-PAD - PRESS {interact} / UP / W AGAIN TO FAST-DROP\n" +
                $"{jump} / SPACE REMAINS JUMP";
        }
        else if (parachute != null && parachute.IsDeploymentRequested)
        {
            promptText.text =
                $"PARACHUTE ARMED\nSTEP OFF THE MARKED RIGHT EDGE - PRESS {interact} / UP / W AGAIN TO CANCEL\n" +
                $"{jump} / SPACE REMAINS JUMP";
        }
        else
        {
            promptText.text =
                $"PARACHUTE SHAFT\nPRESS {interact} / UP / W TO ARM - THEN STEP OFF THE MARKED RIGHT EDGE\n" +
                $"{jump} / SPACE REMAINS JUMP";
        }
    }

    private void RefreshVisibility()
    {
        if (promptPanel == null) return;
        bool visible = Application.isPlaying && parachute != null &&
                       (parachute.IsInLaunchArea || parachute.IsInDescentZone ||
                        parachute.IsCameraTrackingDescent);
        if (promptPanel.activeSelf != visible) promptPanel.SetActive(visible);
    }
}
