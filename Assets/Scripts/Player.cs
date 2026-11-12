using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 15f;
    [SerializeField] private float acceleration = 50f;
    [SerializeField] private float deceleration = 60f;

    [Header("Jump")]
    [SerializeField] private float jumpSpeed = 5f;
    [SerializeField] private float delayBeforeDoubleJump = 0.15f;
    [SerializeField] private float gravityScale = 3f;
    [SerializeField] private float fallMultiplier = 2.5f;
    [SerializeField] private float lowJumpMultiplier = 2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;

    // Mobile controls
    private bool moveLeft;
    private bool moveRight;

    // Keyboard controls
    private bool keyboardLeft;
    private bool keyboardRight;

    private bool isGrounded;
    private bool canDoubleJump;
    private int groundContacts;

    private bool facingRight = true;

    private float targetSpeed;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;

        // Procura o Animator automaticamente
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }
    }

    private void Update()
    {
        HandleKeyboardInput();
        UpdateGravity();
        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        HandleMovement();
    }

    private void HandleMovement()
    {
        // Combina controles mobile e teclado
        bool movingLeft = moveLeft || keyboardLeft;
        bool movingRight = moveRight || keyboardRight;

        // Define a velocidade desejada
        if (movingLeft && !movingRight)
        {
            targetSpeed = -speed;
        }
        else if (movingRight && !movingLeft)
        {
            targetSpeed = speed;
        }
        else
        {
            targetSpeed = 0f;
        }

        // Escolhe aceleração ou desaceleração
        float rate = Mathf.Abs(targetSpeed) > 0.01f ? acceleration : deceleration;

        // Movimento horizontal suave
        float newVelocityX = Mathf.MoveTowards(rb.velocity.x, targetSpeed, rate * Time.fixedDeltaTime);

        rb.velocity = new Vector2(newVelocityX, rb.velocity.y);

        // Vira o personagem conforme a direção
        if (newVelocityX > 0.1f && !facingRight)
        {
            Flip();
        }
        else if (newVelocityX < -0.1f && facingRight)
        {
            Flip();
        }
    }

    private void UpdateGravity()
    {
        if (rb.velocity.y < 0f)
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1f) * Time.deltaTime;
        }
        else if (rb.velocity.y > 0f && !Input.GetKey(KeyCode.Space))
        {
            rb.velocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1f) * Time.deltaTime;
        }
    }

    private void UpdateAnimations()
    {
        if (animator == null)
            return;

        // Velocidade horizontal absoluta.
        // Idle = 0
        // Movimento = maior que 0
        animator.SetFloat("Speed", Mathf.Abs(rb.velocity.x));

        // Estado no chão?
        animator.SetBool("Grounded", isGrounded);

        // Velocidade vertical.
        // Positivo = subindo
        // Negativo = caindo
        animator.SetFloat("VerticalVelocity", rb.velocity.y);
    }

    #region Keyboard Controls 

    private void HandleKeyboardInput()
    {
        // Movimento para esquerda
        keyboardLeft = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);

        // Movimento para direita
        keyboardRight = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);

        // Pulo e pulo duplo
        if (Input.GetKeyDown(KeyCode.Space))
        {
            JumpButton();
        }
    }

    #endregion

    #region Mobile Controls

    public void PointerDownLeft()
    {
        moveLeft = true;
    }

    public void PointerUpLeft()
    {
        moveLeft = false;
    }

    public void PointerDownRight()
    {
        moveRight = true;
    }

    public void PointerUpRight()
    {
        moveRight = false;
    }

    #endregion

    #region Jump

    public void JumpButton()
    {
        if (isGrounded)
        {
            PerformJump();

            isGrounded = false;
            canDoubleJump = false;

            CancelInvoke(nameof(EnableDoubleJump));
            Invoke(nameof(EnableDoubleJump), delayBeforeDoubleJump);
            return;
        }

        if (canDoubleJump)
        {
            PerformJump();
            canDoubleJump = false;
        }
    }

    private void PerformJump()
    {
        // Mantém a velocidade horizontal
        // e altera apenas a velocidade vertical.
        rb.velocity = new Vector2(rb.velocity.x, jumpSpeed);

        // A animação será controlada automaticamente
        // pelo VerticalVelocity + Grounded.
    }

    private void EnableDoubleJump()
    {
        if (!isGrounded && groundContacts <= 0)
        {
            canDoubleJump = true;
        }
    }

    #endregion

    #region Collision

    private bool HasGroundContact(Collision2D other)
    {
        foreach (ContactPoint2D contact in other.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                return true;
            }
        }

        return false;
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (!other.gameObject.CompareTag("Ground"))
            return;

        if (HasGroundContact(other))
        {
            groundContacts++;
            isGrounded = true;
            canDoubleJump = false;
            CancelInvoke(nameof(EnableDoubleJump));
        }
    }

    private void OnCollisionStay2D(Collision2D other)
    {
        if (!other.gameObject.CompareTag("Ground"))
            return;

        if (HasGroundContact(other))
        {
            groundContacts = Mathf.Max(groundContacts, 1);
            isGrounded = true;
            canDoubleJump = false;
            CancelInvoke(nameof(EnableDoubleJump));
        }
    }

    private void OnCollisionExit2D(Collision2D other)
    {
        if (!other.gameObject.CompareTag("Ground"))
            return;

        if (groundContacts > 0)
        {
            groundContacts--;
        }

        isGrounded = groundContacts > 0;

        if (!isGrounded)
        {
            CancelInvoke(nameof(EnableDoubleJump));
            Invoke(nameof(EnableDoubleJump), delayBeforeDoubleJump);
        }
    }

    #endregion

    #region Flip

    private void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1f;

        transform.localScale = scale;
    }

    #endregion
}