using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(SpriteRenderer), typeof(Animator))]
public class HeroMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private Rigidbody2D heroRb;
    [SerializeField, Min(0f)] private float speed = 10f;
    [SerializeField, Min(0f)] private float jumpForce = 15f;
    [SerializeField, Min(0f)] private float jumpTime = 0.35f;

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
    private bool isJumping;
    private bool automatedControlEnabled;
    private float automatedHorizontalInput;
    private bool automatedJumpHeld;
    private bool previousAutomatedJumpHeld;
    private int blueCrystalCount;
    private int blackBigCrystalCount;

    public bool IsGrounded { get; private set; }
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
        ReadInput(out bool jumpPressed, out bool jumpHeld, out bool jumpReleased);

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

        if (IsGrounded && jumpPressed)
        {
            isJumping = true;
            heroRb.linearVelocityY = jumpForce;
            jumpTimeCounter = jumpTime;
        }

        if (jumpHeld && isJumping)
        {
            if (jumpTimeCounter > 0f)
            {
                heroRb.linearVelocityY = jumpForce;
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

        animator.SetFloat("Speed", Mathf.Abs(horizontalInput));
        animator.SetFloat("VerticalVelocity", heroRb.linearVelocityY);
        animator.SetBool("IsGrounded", IsGrounded);
    }

    private void FixedUpdate()
    {
        heroRb.linearVelocityX = horizontalInput * speed;
    }

    public void EnableAutomatedControl(bool enabled)
    {
        automatedControlEnabled = enabled;
        automatedHorizontalInput = 0f;
        automatedJumpHeld = false;
        previousAutomatedJumpHeld = false;
    }

    public void SetAutomatedInput(float horizontal, bool jumpHeld)
    {
        automatedHorizontalInput = Mathf.Clamp(horizontal, -1f, 1f);
        automatedJumpHeld = jumpHeld;
    }

    private void ReadInput(out bool jumpPressed, out bool jumpHeld, out bool jumpReleased)
    {
        if (automatedControlEnabled)
        {
            horizontalInput = automatedHorizontalInput;
            jumpHeld = automatedJumpHeld;
            jumpPressed = automatedJumpHeld && !previousAutomatedJumpHeld;
            jumpReleased = !automatedJumpHeld && previousAutomatedJumpHeld;
            previousAutomatedJumpHeld = automatedJumpHeld;
            return;
        }

        horizontalInput = Input.GetAxisRaw("Horizontal");
        jumpPressed = Input.GetButtonDown("Jump");
        jumpHeld = Input.GetButton("Jump");
        jumpReleased = Input.GetButtonUp("Jump");
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
