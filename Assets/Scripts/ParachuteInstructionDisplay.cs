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

    private void Update() => RefreshVisibility();

    public void RefreshText()
    {
        if (promptText == null) return;
        string interact = MineInput.GetControllerBindingDisplayName(MineButtonAction.Interact);
        string jump = MineInput.GetControllerBindingDisplayName(MineButtonAction.Jump);
        promptText.text =
            $"PARACHUTE SHAFT\nSTEP OFF THE MARKED RIGHT EDGE - PRESS {interact} / UP / W TO OPEN\n" +
            $"PRESS AGAIN TO FAST-DROP     |     {jump} / SPACE ALWAYS JUMPS";
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
