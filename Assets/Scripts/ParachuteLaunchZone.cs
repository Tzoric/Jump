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
    public static string Prompt => string.Empty;

    public TextMeshPro Sign => sign;

    public void Configure(TextMeshPro signLabel)
    {
        sign = signLabel;
        GetComponent<Collider2D>().isTrigger = true;
        if (sign != null) sign.gameObject.SetActive(false);
    }

    private void Awake() => GetComponent<Collider2D>().isTrigger = true;

    private void OnDisable()
    {
        if (activeController != null) activeController.ExitLaunchArea();
        occupants.Clear();
        activeController = null;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        ParachuteDescentController controller = other.GetComponentInParent<ParachuteDescentController>();
        if (controller == null || (activeController != null && controller != activeController)) return;
        bool wasEmpty = occupants.Count == 0;
        occupants.Add(other);
        if (!wasEmpty) return;

        activeController = controller;
        activeController.EnterLaunchArea();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!occupants.Contains(other)) OnTriggerEnter2D(other);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!occupants.Remove(other) || occupants.Count > 0) return;
        activeController?.ExitLaunchArea();
        activeController = null;
    }
}
