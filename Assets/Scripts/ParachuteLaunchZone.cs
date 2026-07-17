using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Safe, non-camera-changing preparation area at a descent lip. Interact can be
/// pressed here to arm the parachute before the player steps into open air.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public sealed class ParachuteLaunchZone : MonoBehaviour
{
    [SerializeField] private TextMeshPro sign;

    private readonly HashSet<Collider2D> occupants = new();
    private ParachuteDescentController activeController;
    private MineRunInventory activeInventory;
    private bool lastArmed;

    public static string Prompt => BuildPrompt(false);

    public TextMeshPro Sign => sign;

    public void Configure(TextMeshPro signLabel)
    {
        sign = signLabel;
        GetComponent<Collider2D>().isTrigger = true;
        if (Application.isPlaying) RefreshSign();
    }

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnEnable()
    {
        MineInput.BindingsChanged += RefreshSign;
        RefreshSign();
    }

    private void OnDisable()
    {
        MineInput.BindingsChanged -= RefreshSign;
        if (activeController != null) activeController.ExitLaunchArea();
        occupants.Clear();
        activeController = null;
        activeInventory = null;
        lastArmed = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ParachuteDescentController controller = other.GetComponentInParent<ParachuteDescentController>();
        if (controller == null || (activeController != null && controller != activeController)) return;
        bool wasEmpty = occupants.Count == 0;
        occupants.Add(other);
        if (!wasEmpty) return;

        activeController = controller;
        activeInventory = controller.GetComponent<MineRunInventory>();
        activeController.EnterLaunchArea();
        lastArmed = activeController.IsDeploymentRequested;
        activeInventory?.ShowMessage(BuildPrompt(lastArmed));
        RefreshSign();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!occupants.Contains(other)) OnTriggerEnter2D(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!occupants.Remove(other) || occupants.Count > 0) return;
        activeController?.ExitLaunchArea();
        activeInventory?.RestoreProgressStatus();
        activeController = null;
        activeInventory = null;
        lastArmed = false;
        RefreshSign();
    }

    private void Update()
    {
        if (activeController == null) return;
        bool armed = activeController.IsDeploymentRequested;
        if (armed == lastArmed) return;
        lastArmed = armed;
        activeInventory?.ShowMessage(BuildPrompt(armed));
        RefreshSign();
    }

    private void RefreshSign()
    {
        if (sign == null) return;
        string button = MineInput.GetControllerBindingDisplayName(MineButtonAction.Interact);
        sign.text = lastArmed
            ? $"PARACHUTE ARMED\nSTEP OFF THE MARKED RIGHT SIDE\n{button} / UP / W CANCELS"
            : $"PARACHUTE DROP\nSTEP OFF THE MARKED RIGHT SIDE\nPRESS {button} / UP / W TO ARM";
    }

    private static string BuildPrompt(bool armed)
    {
        string button = MineInput.GetControllerBindingDisplayName(MineButtonAction.Interact);
        return armed
            ? $"PARACHUTE ARMED - STEP OFF THE RIGHT EDGE. PRESS {button} / UP / W AGAIN TO CANCEL."
            : $"PARACHUTE DROP: PRESS {button} / UP / W TO ARM, THEN STEP OFF THE RIGHT EDGE.";
    }
}
