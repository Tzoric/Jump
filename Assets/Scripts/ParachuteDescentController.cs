using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D), typeof(HeroMovement))]
public sealed class ParachuteDescentController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer canopyRenderer;
    [SerializeField, Min(0.1f)] private float deployedGravity = 1f;
    [SerializeField, Min(0.5f)] private float deployedTerminalSpeed = 4.25f;
    [SerializeField, Min(0.5f)] private float closedTerminalSpeed = 12f;
    [SerializeField, Min(0.5f)] private float maximumHorizontalSpeed = 5.5f;

    private Rigidbody2D body;
    private HeroMovement movement;
    private float normalGravity;
    private int activeZoneCount;
    private int activeLaunchAreaCount;
    private bool cameraTrackingDescent;
    private bool deployedDuringCurrentDescent;
    private bool deploymentRequested;
    private bool previousParachuteHeld;

    public bool IsInDescentZone => activeZoneCount > 0;
    public bool IsInLaunchArea => activeLaunchAreaCount > 0;
    public bool IsCameraTrackingDescent => cameraTrackingDescent;
    public bool IsDeployed { get; private set; }
    public bool DeployedDuringCurrentDescent => deployedDuringCurrentDescent;
    public bool IsDeploymentRequested => deploymentRequested;
    public float DeployedTerminalSpeed => deployedTerminalSpeed;
    public float DescentCenterX { get; private set; }

    public void Configure(SpriteRenderer canopy, float slowGravity = 1f,
        float slowTerminalSpeed = 4.25f, float fastTerminalSpeed = 12f)
    {
        canopyRenderer = canopy;
        deployedGravity = Mathf.Max(0.1f, slowGravity);
        deployedTerminalSpeed = Mathf.Max(0.5f, slowTerminalSpeed);
        closedTerminalSpeed = Mathf.Max(deployedTerminalSpeed, fastTerminalSpeed);
        if (canopyRenderer != null) canopyRenderer.enabled = false;
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        movement = GetComponent<HeroMovement>();
        normalGravity = body.gravityScale;
        if (canopyRenderer != null) canopyRenderer.enabled = false;
    }

    private void Update()
    {
        bool held = movement.ParachuteInputHeld;
        bool pressed = held && !previousParachuteHeld;
        previousParachuteHeld = held;

        if (pressed && (IsInLaunchArea || IsInDescentZone))
            deploymentRequested = !deploymentRequested;

        bool airborneDescent = IsInDescentZone && !movement.IsGrounded;
        if (airborneDescent && body.linearVelocityY <= .25f)
            cameraTrackingDescent = true;
        if (movement.IsGrounded)
            cameraTrackingDescent = false;

        bool shouldDeploy = airborneDescent && deploymentRequested;
        SetDeployed(shouldDeploy);
        if (shouldDeploy) deployedDuringCurrentDescent = true;
    }

    private void FixedUpdate()
    {
        if (!IsInDescentZone) return;

        // Run after HeroMovement so the descent steering cap is deterministic.
        float terminalSpeed = IsDeployed ? deployedTerminalSpeed : closedTerminalSpeed;
        body.linearVelocity = new Vector2(
            Mathf.Clamp(body.linearVelocityX, -maximumHorizontalSpeed, maximumHorizontalSpeed),
            Mathf.Max(body.linearVelocityY, -terminalSpeed));
    }

    private void LateUpdate()
    {
        if (canopyRenderer == null || !canopyRenderer.enabled) return;
        float tilt = body == null ? 0f : Mathf.Clamp(-body.linearVelocityX * 2.2f, -12f, 12f);
        canopyRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, tilt);
    }

    public void EnterDescentZone(float shaftCenterX)
    {
        if (activeZoneCount == 0)
        {
            deployedDuringCurrentDescent = false;
            // A button held while entering may count as the opening press, but an
            // already armed chute must not toggle closed at the trigger boundary.
            if (!deploymentRequested) previousParachuteHeld = false;
        }
        activeZoneCount++;
        DescentCenterX = shaftCenterX;
    }

    public void ExitDescentZone()
    {
        activeZoneCount = Mathf.Max(0, activeZoneCount - 1);
        if (activeZoneCount == 0)
        {
            deploymentRequested = false;
            cameraTrackingDescent = false;
            SetDeployed(false);
        }
    }

    public void EnterLaunchArea()
    {
        bool wasOutside = activeLaunchAreaCount == 0;
        activeLaunchAreaCount++;
        if (wasOutside && !deploymentRequested) previousParachuteHeld = false;
    }

    public void ExitLaunchArea()
    {
        activeLaunchAreaCount = Mathf.Max(0, activeLaunchAreaCount - 1);
    }

    public void ResetDescentState()
    {
        activeZoneCount = 0;
        activeLaunchAreaCount = 0;
        cameraTrackingDescent = false;
        deployedDuringCurrentDescent = false;
        deploymentRequested = false;
        previousParachuteHeld = false;
        if (movement != null) movement.SetJumpSuppressed(false);
        SetDeployed(false);
    }

    private void SetDeployed(bool deployed)
    {
        IsDeployed = deployed;
        if (body != null) body.gravityScale = deployed ? deployedGravity : normalGravity;
        if (canopyRenderer != null) canopyRenderer.enabled = deployed;
    }

    private void OnDisable()
    {
        ResetDescentState();
    }
}
