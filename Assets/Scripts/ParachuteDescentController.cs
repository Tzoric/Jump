using UnityEngine;

[DefaultExecutionOrder(100)]
[RequireComponent(typeof(Rigidbody2D), typeof(HeroMovement))]
public sealed class ParachuteDescentController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer canopyRenderer;
    [SerializeField, Min(0.1f)] private float deployedGravity = 1f;
    [SerializeField, Min(0.5f)] private float deployedTerminalSpeed = 4.25f;
    [SerializeField, Min(0.5f)] private float closedTerminalSpeed = 7f;
    [SerializeField, Min(0.5f)] private float maximumHorizontalSpeed = 5.5f;
    [SerializeField, Range(0.1f, 0.9f)] private float verticalInputDeadZone = .35f;
    [SerializeField, Range(-0.5f, 0.5f)] private float hoverVerticalSpeed;
    [SerializeField, Min(1f)] private float hoverResponse = 30f;
    [SerializeField, Min(0.1f)] private float fastDescentGravity = 1.75f;

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
    public float FastDescentTerminalSpeed => closedTerminalSpeed;
    public float HoverVerticalSpeed => hoverVerticalSpeed;
    public float HoverResponse => hoverResponse;
    public float FastDescentGravity => fastDescentGravity;
    public float MaximumHorizontalSpeed => maximumHorizontalSpeed;
    public float VerticalInputDeadZone => verticalInputDeadZone;
    public float GliderVerticalInput { get; private set; }
    public bool IsHovering => IsDeployed && GliderVerticalInput > verticalInputDeadZone;
    public float ActiveTerminalSpeed =>
        GliderVerticalInput < -verticalInputDeadZone ? closedTerminalSpeed : deployedTerminalSpeed;
    public float DescentCenterX { get; private set; }

    public void Configure(SpriteRenderer canopy, float slowGravity = 1f,
        float slowTerminalSpeed = 4.25f, float fastTerminalSpeed = 7f)
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
        if (movement == null || !movement.isActiveAndEnabled)
        {
            previousParachuteHeld = movement != null && movement.ParachuteInputHeld;
            deploymentRequested = false;
            cameraTrackingDescent = false;
            deployedDuringCurrentDescent = false;
            GliderVerticalInput = 0f;
            SetDeployed(false);
            return;
        }

        bool held = movement.ParachuteInputHeld;
        bool pressed = held && !previousParachuteHeld;
        previousParachuteHeld = held;

        // Consume Interact presses made while standing. Holding X as the player
        // walks off a ledge must never arm the glider accidentally.
        if (movement.IsGrounded)
        {
            deploymentRequested = false;
            cameraTrackingDescent = false;
            deployedDuringCurrentDescent = false;
            GliderVerticalInput = 0f;
            SetDeployed(false);
            return;
        }

        if (pressed)
            deploymentRequested = !deploymentRequested;

        // Descent zones now affect camera framing only. Glider deployment and
        // physics are available during every airborne section in every level.
        bool cameraDescent = IsInDescentZone && body.linearVelocityY <= .25f;
        cameraTrackingDescent = cameraDescent;

        bool shouldDeploy = deploymentRequested;
        SetDeployed(shouldDeploy);
        if (shouldDeploy)
        {
            deployedDuringCurrentDescent = true;
            GliderVerticalInput = movement.GliderVerticalInput;
        }
        else
        {
            GliderVerticalInput = 0f;
        }
    }

    private void FixedUpdate()
    {
        if (!IsDeployed) return;

        // Run after HeroMovement so glider steering and its terminal-speed cap
        // are deterministic regardless of which level or trigger volume it is in.
        float verticalVelocity = body.linearVelocityY;
        if (GliderVerticalInput > verticalInputDeadZone)
        {
            body.gravityScale = 0f;
            verticalVelocity = Mathf.MoveTowards(verticalVelocity, hoverVerticalSpeed,
                hoverResponse * Time.fixedDeltaTime);
            verticalVelocity = Mathf.Max(verticalVelocity, -deployedTerminalSpeed);
        }
        else if (GliderVerticalInput < -verticalInputDeadZone)
        {
            body.gravityScale = fastDescentGravity;
            verticalVelocity = Mathf.Max(verticalVelocity, -closedTerminalSpeed);
        }
        else
        {
            body.gravityScale = deployedGravity;
            verticalVelocity = Mathf.Max(verticalVelocity, -deployedTerminalSpeed);
        }

        body.linearVelocity = new Vector2(
            Mathf.Clamp(body.linearVelocityX, -maximumHorizontalSpeed, maximumHorizontalSpeed),
            verticalVelocity);
    }

    public void EnterDescentZone(float shaftCenterX)
    {
        activeZoneCount++;
        DescentCenterX = shaftCenterX;
    }

    public void ExitDescentZone()
    {
        activeZoneCount = Mathf.Max(0, activeZoneCount - 1);
        if (activeZoneCount == 0)
        {
            cameraTrackingDescent = false;
        }
    }

    public void EnterLaunchArea()
    {
        activeLaunchAreaCount++;
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
        // Consume a button that is still held during landing/respawn/reset.
        // Deployment is a toggle, so reopening must require release + a new press.
        previousParachuteHeld = movement != null && movement.ParachuteInputHeld;
        GliderVerticalInput = 0f;
        if (movement != null) movement.SetJumpSuppressed(false);
        SetDeployed(false);
    }

    private void SetDeployed(bool deployed)
    {
        if (IsDeployed == deployed)
        {
            if (canopyRenderer != null) canopyRenderer.enabled = deployed;
            return;
        }

        IsDeployed = deployed;
        if (body != null) body.gravityScale = deployed ? deployedGravity : normalGravity;
        if (canopyRenderer != null) canopyRenderer.enabled = deployed;
    }

    private void OnDisable()
    {
        ResetDescentState();
    }
}
