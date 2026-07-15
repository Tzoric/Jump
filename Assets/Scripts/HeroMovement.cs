using System.Numerics;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class HeroMovement : MonoBehaviour
{
    public Rigidbody2D heroRb;
    public float speed;
    public float input;
    public float jumpForce;
    public SpriteRenderer spriteRenderer;
    public LayerMask groundLayer;
    public bool isGrounded;
    public Transform feetPosition;
    public float groundCheckCircle;
    public float jumpTime = 0.35f;
    private float jumpTimeCounter;
    private bool isJumping = false;
    public Animator animator;
    private int blueCrystalCount=0;
    private int blackBigCrystalCount=0;

    public TextMeshProUGUI blueCrystalCountDisplay;
    public TextMeshProUGUI blackBigCrystalCountDisplay;

    // Update is called once per frame
    void Update()
    {
        
        input = Input.GetAxisRaw("Horizontal");
        
        
        if(input < 0)
        {
            spriteRenderer.flipX=true;
        }
        else if (input>0)
        {
            spriteRenderer.flipX=false;
        }
        isGrounded = Physics2D.OverlapCircle(feetPosition.position, groundCheckCircle, groundLayer);
        
        if(isGrounded == true && Input.GetButtonDown("Jump"))
        {
            isJumping = true;
            heroRb.linearVelocityY =  jumpForce;
            jumpTimeCounter = jumpTime;
        }
        if(Input.GetButton("Jump") && isJumping)
        {
            if(jumpTimeCounter > 0)
            {
                heroRb.linearVelocityY = jumpForce;
                jumpTimeCounter -= Time.deltaTime;
            }
            else
            {
                isJumping = false;
            }
        }
        if(Input.GetButtonUp("Jump"))
        {
            isJumping = false;
        }
        animator.SetFloat("Speed", Mathf.Abs(input));
        animator.SetFloat("VerticalVelocity", heroRb.linearVelocityY);
        animator.SetBool("IsGrounded", isGrounded);
    }

    void FixedUpdate()
    {
       heroRb.linearVelocityX = input * speed; 
    }

    void OnTriggerEnter2D(Collider2D collider)
    {
        if(collider.gameObject.tag == "BlueCrystal")
        {
            blueCrystalCount++;
            blueCrystalCountDisplay.text = blueCrystalCount.ToString();
            Debug.Log("Blue Crytsal Count = " + blueCrystalCount.ToString());
            Destroy(collider.gameObject);
        }
        else if ( collider.gameObject.tag == "BlackBigCrystal")
        {
            blackBigCrystalCount++;
            blackBigCrystalCountDisplay.text = blackBigCrystalCount.ToString();
            Debug.Log("Black Big Crytsal Count = " + blackBigCrystalCount.ToString());
            Destroy(collider.gameObject);
        }
        
    }
}
