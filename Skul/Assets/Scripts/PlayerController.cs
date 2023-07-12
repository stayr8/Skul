using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Player Variables")]
    private float movementInputDirction;
    private float jumpTimer;
    private float turnTimer;
    private float wallJumpTimer;

    [SerializeField] private float movementSpeed = 10.0f;
    [SerializeField] private float jumpForce = 12.0f;
    [SerializeField] private float groundCheckRadius = 0.34f;
    [SerializeField] private float wallCheckDistance = 0.45f;
    [SerializeField] private float wallSlidSpeed = 2.0f;
    //[SerializeField] private float movementForceInAir = 50.0f;
    [SerializeField] private float airDragMultiplier = 0.95f;
    [SerializeField] private float variableJumpHeightMultiplier = 0.5f;
    //[SerializeField] private float wallHopForce = 10.0f;
    [SerializeField] private float wallJumpForce = 20.0f;
    [SerializeField] private float jumpTimerSet = 0.15f;
    [SerializeField] private float turnTimerSet = 0.1f;
    [SerializeField] private float wallJumpTimerSet;

    private int amountOfJumpsLeft;
    private int facingDirection = 1;
    private int amountOfJumps = 2;
    private int lastWallJumpDirection;

    [Header("Player Physics")]
    [SerializeField] private Vector2 wallHopDirection;
    [SerializeField] private Vector2 wallJumpDirection;

    [Header("Player True/False")]
    private bool isFacingRight = true;
    private bool isWalking;
    private bool canNormalJump;
    private bool canWallJump;
    private bool isWallSliding;
    private bool isAttemptingToJump;
    private bool checkJumpMultiplier;
    private bool canMove;
    private bool canFlip;
    private bool hasWallJumped;
    [SerializeField] private bool isGrounded;
    [SerializeField] private bool isTouchingWall;

    // 고정 값
    [SerializeField] private Transform groundCheck;
    [SerializeField] private Transform wallCheck;

    [SerializeField] LayerMask whatIsGround;

    private Rigidbody2D rb;
    private Animator anim;
    
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        amountOfJumpsLeft = amountOfJumps;
        wallHopDirection.Normalize();
        wallJumpDirection.Normalize();
    }

    
    void Update()
    {
        CheckInput();
        CheckMovementDirection();
        UpdateAnimations();
        CheckIfCanJump();
        CheckIfWallSliding();
        CheckJump();
    }

    private void FixedUpdate()
    {
        ApplyMonvement();
        CheckSurroundings();
    }

    // 벽 타기 체크
    private void CheckIfWallSliding()
    {
        if(isTouchingWall && movementInputDirction == facingDirection && rb.velocity.y < 0)
        {
            isWallSliding = true;
        }
        else
        {
            isWallSliding = false;
        }
    }

    // 움직임 체크
    private void CheckMovementDirection()
    {
        if(isFacingRight && movementInputDirction < 0)
        {
            Flip();
        }
        else if(!isFacingRight && movementInputDirction > 0)
        {
            Flip();
        }

        if (Mathf.Abs(rb.velocity.x) < 0.3)
        {
            isWalking = false;
        }
        else
        {
            isWalking = true;
        }
    }
    
    // 애니메이션 체크
    private void UpdateAnimations()
    {
        anim.SetBool("IsWalking", isWalking);
        anim.SetBool("IsGrounded", isGrounded);
        anim.SetFloat("yVelocity", rb.velocity.y);
        anim.SetBool("IsWallSliding", isWallSliding);
    }

    // 입력 값 체크 (이동, 점프 등등)
    private void CheckInput()
    {
        movementInputDirction = Input.GetAxisRaw("Horizontal");

        // 점프 Down시 큰 점프, Down 중 때면 약 점프
        if(Input.GetButtonDown("Jump"))
        {
            if(isGrounded || (amountOfJumpsLeft > 0 && isTouchingWall))
            {
                NormalJump();
            }
            else
            {
                jumpTimer = jumpTimerSet;
                isAttemptingToJump = true;
            }
        }

        if(Input.GetButtonDown("Horizontal") && isTouchingWall)
        {
            if(!isGrounded && movementInputDirction != facingDirection)
            {
                canMove= false;
                canFlip= false;

                turnTimer = turnTimerSet;
            }
        }

        if(!canMove)
        {
            turnTimer -= Time.deltaTime;

            if(turnTimer <= 0)
            {
                canMove = true;
                canFlip = true;
            }
        }

        if(checkJumpMultiplier && !Input.GetButton("Jump"))
        {
            checkJumpMultiplier = false;
            rb.velocity = new Vector2(rb.velocity.x, rb.velocity.y * variableJumpHeightMultiplier);
        }
    }

    // 점프
    private void CheckJump()
    {
        if(jumpTimer > 0)
        {
            //WallJump
            if(!isGrounded && isTouchingWall && movementInputDirction != 0 && movementInputDirction != facingDirection)
            {
                WallJump();
            }
            else if(isGrounded)
            {
                NormalJump();
            }
        }
        
        if(isAttemptingToJump)
        {
            jumpTimer -= Time.deltaTime;
        }

        if(wallJumpTimer > 0)
        {
            if(hasWallJumped && movementInputDirction == -lastWallJumpDirection)
            {
                rb.velocity = new Vector2(rb.velocity.x, 0.0f);
                hasWallJumped = false;
            }
            else if(wallJumpTimer <= 0)
            {
                hasWallJumped = false;
            }
            else
            {
                wallJumpTimer -= Time.deltaTime;
            }
        }
    }

    private void NormalJump()
    {
        if (canNormalJump) // 기본 점프
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            amountOfJumpsLeft--;
            jumpTimer = 0;
            isAttemptingToJump = false;
            checkJumpMultiplier = true;
        }
    }

    private void WallJump()
    {
         if (canWallJump) // 벽에 붙은 상태에서 방향키 입력 후 점프
         {
            rb.velocity = new Vector2(rb.velocity.x, 0.0f);
            isWallSliding = false;
            amountOfJumpsLeft = amountOfJumps;
            amountOfJumpsLeft--;
            Vector2 forceToAdd = new Vector2(wallJumpForce * wallJumpDirection.x * movementInputDirction, wallJumpForce * wallJumpDirection.y);
            rb.AddForce(forceToAdd, ForceMode2D.Impulse);
            jumpTimer = 0;
            isAttemptingToJump = false;
            checkJumpMultiplier = true;
            turnTimer = 0;
            canMove = true;
            canFlip = true;
            hasWallJumped = true;
            wallJumpTimer = wallJumpTimerSet;
            lastWallJumpDirection = -facingDirection;
        }
    }

    // 점프 횟수 및 점프 체크
    private void CheckIfCanJump()
    {
        if(isGrounded && rb.velocity.y <= 0.01f)
        {
            amountOfJumpsLeft = amountOfJumps;
        }

        if(isTouchingWall)
        {
            canWallJump = true;
        }
        
        if(amountOfJumpsLeft <= 0)
        {
            canNormalJump = false;
        }
        else
        {
            canNormalJump = true;
        }
    }

    // 이동 속도
    private void ApplyMonvement()
    {
        if(!isGrounded && !isWallSliding && movementInputDirction == 0)
        {
            rb.velocity = new Vector2(rb.velocity.x * airDragMultiplier, rb.velocity.y);
        }
        else if(canMove)
        {
            rb.velocity = new Vector2(movementSpeed * movementInputDirction, rb.velocity.y);
        }

        if(isWallSliding)
        {
            if(rb.velocity.y < -wallSlidSpeed)
            {
                rb.velocity = new Vector2(rb.velocity.x, -wallSlidSpeed);
            }
        }
    }

    // 애니메이션 반전
    private void Flip()
    {
        if(!isWallSliding && canFlip)
        {
            facingDirection *= -1;
            isFacingRight = !isFacingRight;
            transform.Rotate(0.0f, 180.0f, 0.0f);
        }
    }

    // 충돌 체크 ( 점프, 벽 )
    private void CheckSurroundings()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, whatIsGround);

        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, whatIsGround);
    }

    // Draw
    private void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);

        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y, wallCheck.position.z));
    }
}
