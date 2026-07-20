using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Animator))]
public class HeroMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Rigidbody2D heroRb;
    [SerializeField, Min(0f)] private float speed = 7.5f;
    [SerializeField, Min(0f)] private float runSpeed = 9f;
    [SerializeField, Min(0f)] private float jumpForce = 12f;
    [SerializeField, Min(0f)] private float powerJumpForce = 14.75f;
    [SerializeField, Min(0f)] private float jumpTime = 0.24f;
    [SerializeField, Min(0f)] private float powerJumpTime = 0.26f;
    [SerializeField, Range(0f, 0.2f)] private float jumpAnticipationSeconds = 0.08f;

    [Header("Ground Check")]
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private Transform feetPosition;
    [SerializeField, Min(0.01f)] private float groundCheckCircle = 0.3f;

    [Header("Presentation")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Animator animator;
    [SerializeField] private TextMeshProUGUI blueCrystalCountDisplay;
    [SerializeField] private TextMeshProUGUI blackBigCrystalCountDisplay;

    private float horizontalInput;
    private float jumpTimeCounter;
    private float activeJumpForce;
    private bool isJumping;
    private bool isPreparingJump;
    private bool runInputHeld;
    private float jumpLaunchAt;
    private bool automatedControlEnabled;
    private float automatedHorizontalInput;
    private bool automatedJumpHeld;
    private bool automatedRunHeld;
    private bool automatedParachuteHeld;
    private float automatedGliderVerticalInput;
    private bool previousAutomatedJumpHeld;
    private int blueCrystalCount;
    private int blackBigCrystalCount;
    private bool inputLocked;
    private bool jumpSuppressed;

    public bool IsGrounded { get; private set; }
    public bool IsPreparingJump => isPreparingJump;
    public bool IsRunning => runInputHeld && Mathf.Abs(horizontalInput) >= .5f;
    public bool IsPowerJumping { get; private set; }
    public bool IsJumpSuppressed => jumpSuppressed;
    public float JumpAnticipationSeconds => jumpAnticipationSeconds;
    public float WalkSpeed => speed;
    public float RunSpeed => runSpeed;
    public float JumpForce => jumpForce;
    public float PowerJumpForce => powerJumpForce;
    public float JumpHoldSeconds => jumpTime;
    public float PowerJumpHoldSeconds => powerJumpTime;
    public bool JumpInputHeld { get; private set; }
    public bool ParachuteInputHeld { get; private set; }
    public float HorizontalInput => horizontalInput;
    public bool IsFacingLeft => spriteRenderer != null && spriteRenderer.flipX;
    public float GliderVerticalInput => automatedControlEnabled
        ? automatedGliderVerticalInput
        : MineInput.GliderVertical;
    public int BlueCrystalCount => blueCrystalCount;
    public int BlackBigCrystalCount => blackBigCrystalCount;

    private void Awake()
    {
        heroRb ??= GetComponent<Rigidbody2D>();
        spriteRenderer ??= GetComponent<SpriteRenderer>();
        animator ??= GetComponent<Animator>();
        UpdateCollectibleDisplays();
    }

    private void Update()
    {
        if (inputLocked)
        {
            horizontalInput = 0f;
            runInputHeld = false;
            JumpInputHeld = false;
            ParachuteInputHeld = false;
            isJumping = false;
            animator.SetFloat("Speed", 0f);
            return;
        }

        if (MineLevelMenuController.IsPaused)
        {
            bool pausedJumpHeld = automatedControlEnabled ? automatedJumpHeld : MineInput.JumpHeld;
            horizontalInput = 0f;
            runInputHeld = false;
            JumpInputHeld = pausedJumpHeld;
            ParachuteInputHeld = false;
            if (!pausedJumpHeld) isJumping = false;
            if (automatedControlEnabled) previousAutomatedJumpHeld = automatedJumpHeld;
            animator.SetFloat("Speed", 0f);
            return;
        }
        ReadInput(out bool jumpPressed, out bool jumpHeld, out bool jumpReleased,
            out bool runHeld, out bool parachuteHeld);
        runInputHeld = runHeld;
        JumpInputHeld = jumpHeld;
        ParachuteInputHeld = parachuteHeld;

        if (horizontalInput < 0f)
        {
            spriteRenderer.flipX = true;
        }
        else if (horizontalInput > 0f)
        {
            spriteRenderer.flipX = false;
        }

        IsGrounded = feetPosition != null &&
            Physics2D.OverlapCircle(feetPosition.position, groundCheckCircle, groundLayer);

        if (IsGrounded && jumpPressed && !isPreparingJump && !jumpSuppressed)
        {
            IsPowerJumping = runHeld && Mathf.Abs(horizontalInput) >= .5f;
            isPreparingJump = true;
            isJumping = jumpHeld;
            jumpLaunchAt = Time.time + jumpAnticipationSeconds;
        }

        if (isPreparingJump && Time.time >= jumpLaunchAt)
        {
            isPreparingJump = false;
            isJumping = jumpHeld;
            activeJumpForce = IsPowerJumping ? powerJumpForce : jumpForce;
            heroRb.linearVelocityY = activeJumpForce;
            jumpTimeCounter = IsPowerJumping ? powerJumpTime : jumpTime;
        }

        if (jumpHeld && isJumping)
        {
            if (jumpTimeCounter > 0f)
            {
                heroRb.linearVelocityY = activeJumpForce;
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }

        if (jumpReleased)
        {
            isJumping = false;
        }

        if (IsGrounded && !isPreparingJump && heroRb.linearVelocityY <= .5f)
        {
            IsPowerJumping = false;
        }

        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetFloat("VerticalVelocity", heroRb.linearVelocityY);
        animator.SetBool("IsGrounded", IsGrounded);
    }

    private void FixedUpdate()
    {
        if (inputLocked) return;

        if (Mathf.Abs(horizontalInput) > .01f)
        {
            heroRb.linearVelocityX = horizontalInput * (IsRunning ? runSpeed : speed);
            return;
        }

        RaycastHit2D ground = feetPosition == null
            ? default
            : Physics2D.Raycast(feetPosition.position, Vector2.down, .55f, groundLayer);
        bool standingOnSlope = ground.collider != null && Mathf.Abs(ground.normal.x) > .08f;
        if (!standingOnSlope)
        {
            heroRb.linearVelocityX = Mathf.MoveTowards(heroRb.linearVelocityX, 0f,
                speed * 9f * Time.fixedDeltaTime);
        }
    }

    public void ConfigureMovement(float moveSpeed, float verticalJumpForce, float heldJumpTime,
        float anticipationSeconds = 0.08f, float runningSpeed = 9f,
        float poweredJumpForce = 14.75f, float poweredHeldJumpTime = .26f)
    {
        speed = Mathf.Max(0f, moveSpeed);
        runSpeed = Mathf.Max(speed, runningSpeed);
        jumpForce = Mathf.Max(0f, verticalJumpForce);
        powerJumpForce = Mathf.Max(jumpForce, poweredJumpForce);
        jumpTime = Mathf.Max(0f, heldJumpTime);
        powerJumpTime = Mathf.Max(jumpTime, poweredHeldJumpTime);
        jumpAnticipationSeconds = Mathf.Clamp(anticipationSeconds, 0f, 0.2f);
    }

    public void SetInputLocked(bool locked)
    {
        inputLocked = locked;
        horizontalInput = 0f;
        if (locked)
        {
            CancelJumpState();
            if (heroRb != null) heroRb.linearVelocityX = 0f;
        }
    }

    public void SetJumpSuppressed(bool suppressed)
    {
        jumpSuppressed = suppressed;
        if (suppressed) CancelJumpState();
    }

    public void EnableAutomatedControl(bool enabled)
    {
        automatedControlEnabled = enabled;
        automatedHorizontalInput = 0f;
        automatedJumpHeld = false;
        automatedRunHeld = false;
        automatedParachuteHeld = false;
        automatedGliderVerticalInput = 0f;
        previousAutomatedJumpHeld = false;
        JumpInputHeld = false;
        ParachuteInputHeld = false;
        CancelJumpState();
    }

    public void SetAutomatedInput(float horizontal, bool jumpHeld, bool runHeld = false,
        bool parachuteHeld = false, float gliderVertical = 0f)
    {
        automatedHorizontalInput = Mathf.Clamp(horizontal, -1f, 1f);
        automatedJumpHeld = jumpHeld;
        automatedRunHeld = runHeld;
        automatedParachuteHeld = parachuteHeld;
        automatedGliderVerticalInput = Mathf.Clamp(gliderVertical, -1f, 1f);
    }

    private void ReadInput(out bool jumpPressed, out bool jumpHeld, out bool jumpReleased,
        out bool runHeld, out bool parachuteHeld)
    {
        if (automatedControlEnabled)
        {
            horizontalInput = automatedHorizontalInput;
            jumpHeld = automatedJumpHeld;
            runHeld = automatedRunHeld;
            parachuteHeld = automatedParachuteHeld;
            jumpPressed = automatedJumpHeld && !previousAutomatedJumpHeld;
            jumpReleased = !automatedJumpHeld && previousAutomatedJumpHeld;
            previousAutomatedJumpHeld = automatedJumpHeld;
            return;
        }

        horizontalInput = MineInput.Horizontal;
        runHeld = MineInput.RunHeld;
        jumpPressed = MineInput.JumpPressed;
        jumpHeld = MineInput.JumpHeld;
        jumpReleased = MineInput.JumpReleased;
        parachuteHeld = MineInput.ParachuteHeld;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("BlueCrystal"))
        {
            blueCrystalCount++;
        }
        else if (other.CompareTag("BlackBigCrystal"))
        {
            blackBigCrystalCount++;
        }
        else
        {
            return;
        }

        UpdateCollectibleDisplays();
        Destroy(other.gameObject);
    }

    private void UpdateCollectibleDisplays()
    {
        if (blueCrystalCountDisplay != null)
        {
            blueCrystalCountDisplay.text = blueCrystalCount.ToString();
        }

        if (blackBigCrystalCountDisplay != null)
        {
            blackBigCrystalCountDisplay.text = blackBigCrystalCount.ToString();
        }
    }

    private void CancelJumpState()
    {
        isPreparingJump = false;
        isJumping = false;
        runInputHeld = false;
        IsPowerJumping = false;
        JumpInputHeld = false;
        jumpLaunchAt = 0f;
        jumpTimeCounter = 0f;
        activeJumpForce = jumpForce;
    }

    private void OnDisable()
    {
        CancelJumpState();
    }

    private void OnDrawGizmosSelected()
    {
        if (feetPosition == null)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(feetPosition.position, groundCheckCircle);
    }
}
