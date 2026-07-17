using TMPro;
using UnityEngine;

public sealed class MineControlHintDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelInstructions;
    [SerializeField] private TextMeshProUGUI pauseInstructions;
    [SerializeField, TextArea] private string levelObjective;

    public TextMeshProUGUI LevelInstructions => levelInstructions;
    public TextMeshProUGUI PauseInstructions => pauseInstructions;
    public string LevelObjective => levelObjective;

    public void Configure(TextMeshProUGUI instructions, TextMeshProUGUI pauseHelp, string objective)
    {
        levelInstructions = instructions;
        pauseInstructions = pauseHelp;
        levelObjective = objective;
        Refresh();
    }

    private void OnEnable()
    {
        MineInput.BindingsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        MineInput.BindingsChanged -= Refresh;
    }

    public void Refresh()
    {
        if (levelInstructions != null)
        {
            levelInstructions.text = $"{levelObjective}\n" +
                $"CONTROLLER: STICK / D-PAD MOVE  |  {Button(MineButtonAction.Run)} RUN + " +
                $"{Button(MineButtonAction.Jump)} JUMP = POWER JUMP  |  " +
                $"{Button(MineButtonAction.Interact)} INTERACT  |  " +
                $"{Button(MineButtonAction.Potion)} POTION  |  " +
                $"{Button(MineButtonAction.Pause)} PAUSE  |  " +
                $"{Button(MineButtonAction.Home)} SHOP  |  REMAP: OVERVIEW > CONTROLS\n" +
                "KEYBOARD: ARROWS / A-D MOVE  |  SHIFT RUN + SPACE JUMP  |  UP / W INTERACT  |  " +
                "H POTION  |  ESC PAUSE  |  BACKSPACE SHOP";
        }

        if (pauseInstructions != null)
        {
            pauseInstructions.text =
                $"{Button(MineButtonAction.Pause)} / ESC: RESUME     |     " +
                $"{Button(MineButtonAction.Home)} / BACKSPACE: RETURN TO OVERVIEW SHOP";
        }
    }

    private static string Button(MineButtonAction action) =>
        MineInput.GetControllerBindingDisplayName(action);
}
