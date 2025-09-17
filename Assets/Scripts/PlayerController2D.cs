using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 6f;
    public float jumpForce = 11f;
    public float fallMultiplier = 1.5f;
    public float lowJumpMultiplier = 1.2f;

    [Header("Dash")]
    public float dashSpeed = 15f;
    public float dashTime = 0.15f;
    public float dashCooldown = 1.5f;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f;
    public float attackCooldown = 0.5f;
    public LayerMask enemyLayer;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundCheckRadius = 0.3f;
    public LayerMask groundLayer;

    [Header("Wall")]
    public float wallSlideSpeed = 3f;
    [Header("Wall Check")]
    public Transform wallCheckLeft;   // 왼쪽 벽 체크용
    public Transform wallCheckRight;  // 오른쪽 벽 체크용
    public float wallCheckDistance = 0.6f;

    [Header("Realism Settings")]
    public bool enableDoubleJump = false;
    public float airDrag = 0.5f;
    public float gravityScale = 2f;

    // 컴포넌트
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private Animator animator;

    // 이동 상태
    private float moveInput;
    private bool facingRight = true;

    // 점프 상태
    private bool isGrounded = false;
    private int jumpCount = 0;
    private int maxJumps = 2;

    // 벽 상태
    private bool isTouchingWallLeft = false;
    private bool isTouchingWallRight = false;
    private bool isWallSliding = false;

    // 대쉬 상태
    private bool isDashing = false;
    private bool canDash = true;
    private float dashCounter;
    private float dashCooldownCounter;

    // 공격 상태
    private bool canAttack = true;
    private float attackCooldownCounter;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();

        // 현실적인 물리 설정 적용
        rb.gravityScale = gravityScale;
        rb.drag = airDrag;

        // 2단 점프 설정
        maxJumps = enableDoubleJump ? 2 : 1;

        Debug.Log($"플레이어 설정: 2단점프={enableDoubleJump}, 중력={gravityScale}, 공기저항={airDrag}");
    }

    void Update()
    {
        GetInput();
        CheckGrounded();
        CheckWallTouch();
        UpdateTimers();

        if (animator != null)
            UpdateAnimations();
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            return;
        }

        Move();
        CheckWallSlide();
        BetterJump();
    }

    void GetInput()
    {
        moveInput = Input.GetAxisRaw("Horizontal");

        // 점프
        if (Input.GetButtonDown("Jump"))
        {
            Jump();
        }

        // 대쉬
        if (Input.GetKeyDown(KeyCode.LeftShift) && canDash)
        {
            StartDash();
        }

        // 공격
        if (Input.GetKeyDown(KeyCode.X) && canAttack)
        {
            Attack();
        }
    }

    void Move()
    {
        rb.velocity = new Vector2(moveInput * moveSpeed, rb.velocity.y);

        // 방향 전환
        if (moveInput > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput < 0 && facingRight)
        {
            Flip();
        }
    }

    void Flip()
    {
        facingRight = !facingRight;
        spriteRenderer.flipX = !facingRight;
    }

    void Jump()
    {
        // 벽에 붙어있으면 점프 불가
        if (isTouchingWallLeft || isTouchingWallRight)
        {
            Debug.Log("벽에 붙어있어서 점프 불가!");
            return;
        }

        // 첫 번째 점프 (땅에서 또는 jumpCount가 0이면)
        if (jumpCount == 0)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            jumpCount = 1;
            Debug.Log($"첫 번째 점프! jumpCount: {jumpCount}");
            return;
        }

        // 2단 점프 (enableDoubleJump가 true이고, jumpCount가 1일 때)
        if (enableDoubleJump && jumpCount == 1)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce * 0.9f);
            jumpCount = 2;
            Debug.Log($"2단 점프! jumpCount: {jumpCount}");
            return;
        }

        Debug.Log("점프 불가!");
    }

    void StartDash()
    {
        isDashing = true;
        canDash = false;
        dashCounter = dashTime;
        dashCooldownCounter = dashCooldown;

        rb.gravityScale = 0f;
        rb.velocity = new Vector2(dashSpeed * (facingRight ? 1 : -1), 0f);
    }

    void Attack()
    {
        canAttack = false;
        attackCooldownCounter = attackCooldown;

        Vector2 attackPos = transform.position;
        attackPos.x += facingRight ? attackRange : -attackRange;

        Collider2D[] enemies = Physics2D.OverlapCircleAll(attackPos, attackRange, enemyLayer);

        for (int i = 0; i < enemies.Length; i++)
        {
            Enemy enemy = enemies[i].GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(attackDamage);
            }
        }
    }

    void CheckGrounded()
    {
        bool wasGrounded = isGrounded;

        // 땅 감지
        if (groundCheck != null)
        {
            isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);
        }
        else
        {
            isGrounded = Physics2D.Raycast(transform.position, Vector2.down, groundCheckRadius, groundLayer);
        }

        // 착지한 순간에 점프 카운트 리셋
        if (isGrounded && !wasGrounded)
        {
            jumpCount = 0;
            canDash = true;
            Debug.Log("착지! jumpCount 리셋");
        }
    }

    void CheckWallTouch()
    {
        if (wallCheckLeft == null || wallCheckRight == null)
        {
            return;
        }

        // 왼쪽 벽 체크
        RaycastHit2D hitLeft = Physics2D.Raycast(wallCheckLeft.position, Vector2.left, wallCheckDistance, groundLayer);
        isTouchingWallLeft = hitLeft.collider != null;

        // 오른쪽 벽 체크
        RaycastHit2D hitRight = Physics2D.Raycast(wallCheckRight.position, Vector2.right, wallCheckDistance, groundLayer);
        isTouchingWallRight = hitRight.collider != null;

        // 🎯 핵심 기능: 벽에 붙으면 점프 카운트 리셋
        if ((isTouchingWallLeft || isTouchingWallRight) && !isGrounded && jumpCount > 0)
        {
            jumpCount = 0;
            Debug.Log("벽에 붙음! jumpCount 리셋 - 더블점프 재활성화!");
        }

        // 디버그 로그
        if (isTouchingWallLeft || isTouchingWallRight)
        {
            Debug.Log($"벽 상태: 왼쪽={isTouchingWallLeft}, 오른쪽={isTouchingWallRight}, 슬라이딩={isWallSliding}, jumpCount={jumpCount}");
        }
    }

    void BetterJump()
    {
        // 현실적인 점프 물리
        if (rb.velocity.y < 0)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.velocity.y > 0 && !Input.GetButton("Jump"))
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }
    }

    void CheckWallSlide()
    {
        // 벽슬라이딩 조건: 벽에 붙어있고, 공중에 있고, 아래로 떨어지고 있을 때
        bool shouldWallSlide = (isTouchingWallLeft || isTouchingWallRight) && !isGrounded && rb.velocity.y < 0;

        if (shouldWallSlide)
        {
            isWallSliding = true;
            rb.velocity = new Vector2(rb.velocity.x, Mathf.Clamp(rb.velocity.y, -wallSlideSpeed, float.MaxValue));
            Debug.Log($"벽슬라이드 중! velocity.y: {rb.velocity.y:F2}");
        }
        else
        {
            if (isWallSliding)
            {
                Debug.Log("벽슬라이드 종료");
            }
            isWallSliding = false;
        }
    }

    void UpdateTimers()
    {
        // 대쉬 타이머
        if (isDashing)
        {
            dashCounter -= Time.deltaTime;
            if (dashCounter <= 0)
            {
                isDashing = false;
                rb.gravityScale = gravityScale;
            }
        }

        // 대쉬 쿨다운
        if (!canDash)
        {
            dashCooldownCounter -= Time.deltaTime;
            if (dashCooldownCounter <= 0)
            {
                canDash = true;
            }
        }

        // 공격 쿨다운
        if (!canAttack)
        {
            attackCooldownCounter -= Time.deltaTime;
            if (attackCooldownCounter <= 0)
            {
                canAttack = true;
            }
        }
    }

    void UpdateAnimations()
    {
        animator.SetFloat("Speed", Mathf.Abs(moveInput));
        animator.SetBool("IsGrounded", isGrounded);
        animator.SetBool("IsWallSliding", isWallSliding);
        animator.SetBool("IsDashing", isDashing);
        animator.SetFloat("VelocityY", rb.velocity.y);

        if (!canAttack && attackCooldownCounter > attackCooldown - 0.1f)
        {
            animator.SetTrigger("Attack");
        }
    }

    void OnDrawGizmosSelected()
    {
        // 땅 체크 시각화
        if (groundCheck != null)
        {
            Gizmos.color = isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }

        // 벽 체크 시각화
        if (wallCheckLeft != null)
        {
            if (isWallSliding && isTouchingWallLeft)
                Gizmos.color = Color.magenta; // 벽슬라이딩 중
            else if (isTouchingWallLeft)
                Gizmos.color = Color.blue;    // 벽 감지
            else
                Gizmos.color = Color.white;   // 벽 없음

            Gizmos.DrawRay(wallCheckLeft.position, Vector3.left * wallCheckDistance);
        }

        if (wallCheckRight != null)
        {
            if (isWallSliding && isTouchingWallRight)
                Gizmos.color = Color.magenta; // 벽슬라이딩 중
            else if (isTouchingWallRight)
                Gizmos.color = Color.blue;    // 벽 감지
            else
                Gizmos.color = Color.white;   // 벽 없음

            Gizmos.DrawRay(wallCheckRight.position, Vector3.right * wallCheckDistance);
        }

        // 공격 범위 시각화
        Gizmos.color = Color.yellow;
        Vector3 attackPos = transform.position;
        attackPos.x += facingRight ? attackRange : -attackRange;
        Gizmos.DrawWireSphere(attackPos, attackRange);
    }
}

// 간단한 적 클래스
public class Enemy : MonoBehaviour
{
    public float maxHealth = 30f;
    private float currentHealth;

    void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        Debug.Log($"{gameObject.name}이(가) {damage} 데미지를 받았습니다. 체력: {currentHealth}");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log($"{gameObject.name}이(가) 사망했습니다!");
        Destroy(gameObject);
    }
}