using UnityEngine;

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
    private bool cameraTrackingDescent;

    public bool IsInDescentZone => activeZoneCount > 0;
    public bool IsCameraTrackingDescent => cameraTrackingDescent;
    public bool IsDeployed { get; private set; }
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
        if (cameraTrackingDescent && !IsInDescentZone && movement.IsGrounded)
            cameraTrackingDescent = false;

        bool shouldDeploy = IsInDescentZone && !movement.IsGrounded &&
                            movement.ParachuteInputHeld;
        SetDeployed(shouldDeploy);
    }

    private void FixedUpdate()
    {
        if (!IsInDescentZone) return;

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
        activeZoneCount++;
        DescentCenterX = shaftCenterX;
        cameraTrackingDescent = true;
        movement.SetJumpSuppressed(true);
    }

    public void ExitDescentZone()
    {
        activeZoneCount = Mathf.Max(0, activeZoneCount - 1);
        if (activeZoneCount == 0)
        {
            movement.SetJumpSuppressed(false);
            SetDeployed(false);
        }
    }

    public void ResetDescentState()
    {
        activeZoneCount = 0;
        cameraTrackingDescent = false;
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
