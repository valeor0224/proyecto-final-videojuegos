using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// "Movement Engine" del Nivel 3: controlador de personaje 2D con jetpack de energía limitada.
    ///
    /// Mecánicas:
    ///  - Movimiento horizontal con aceleración y control aéreo reducido.
    ///  - Salto básico (con coyote time y jump buffering para un tacto pulido).
    ///  - Doble salto / impulso de jetpack en el aire (consume energía).
    ///  - Dash lateral (impulso horizontal rápido, consume energía, con cooldown).
    ///  - Planeo controlado (reduce la gravedad y limita la caída, drena energía).
    ///  - Energía limitada que solo se recarga al tocar el suelo.
    ///
    /// API: este código usa Unity 6 (Rigidbody2D.linearVelocity).
    ///      En Unity 2022 o anterior, reemplaza 'linearVelocity' por 'velocity'.
    ///
    /// REQUISITOS:
    ///  - Rigidbody2D (Gravity Scale > 0, Freeze Rotation Z activado).
    ///  - Un Collider2D (CapsuleCollider2D recomendado).
    ///  - Un objeto vacío hijo 'groundCheck' en los pies del personaje.
    ///  - Una LayerMask 'groundLayer' que marque el suelo/plataformas.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class JetpackController2D : MonoBehaviour
    {
        [Header("Movimiento horizontal")]
        [SerializeField] private float moveSpeed = 7f;
        [SerializeField] private float acceleration = 60f;
        [Range(0f, 1f)]
        [Tooltip("Multiplicador de control mientras está en el aire (1 = igual que en suelo).")]
        [SerializeField] private float airControl = 0.6f;

        [Header("Salto")]
        [SerializeField] private float jumpForce = 12f;
        [Tooltip("Saltos extra en el aire. 1 = doble salto.")]
        [SerializeField] private int maxAirJumps = 1;
        [Tooltip("Margen para saltar justo después de salir de una plataforma.")]
        [SerializeField] private float coyoteTime = 0.10f;
        [Tooltip("Margen para registrar el salto un poco antes de aterrizar.")]
        [SerializeField] private float jumpBufferTime = 0.10f;

        [Header("Dash (impulso lateral)")]
        [SerializeField] private float dashSpeed = 18f;
        [SerializeField] private float dashDuration = 0.18f;
        [SerializeField] private float dashCooldown = 0.6f;
        [SerializeField] private float dashEnergyCost = 25f;

        [Header("Planeo")]
        [Tooltip("Gravedad reducida mientras se planea.")]
        [SerializeField] private float glideGravityScale = 0.35f;
        [Tooltip("Velocidad máxima de caída durante el planeo.")]
        [SerializeField] private float glideMaxFallSpeed = 2.5f;
        [Tooltip("Energía consumida por segundo mientras se planea.")]
        [SerializeField] private float glideEnergyDrain = 15f;

        [Header("Energía del jetpack")]
        [SerializeField] private float maxEnergy = 100f;
        [SerializeField] private float airJumpEnergyCost = 20f;
        [SerializeField] private float energyRegenPerSecond = 40f;

        [Header("Detección de suelo")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Controles")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [SerializeField] private KeyCode dashKey = KeyCode.LeftShift;
        [SerializeField] private KeyCode glideKey = KeyCode.LeftControl;

        // --- Estado interno ---
        private Rigidbody2D _rb;
        private float _defaultGravity;
        private float _horizontalInput;

        private bool _isGrounded;
        private int _airJumpsUsed;
        private float _coyoteCounter;
        private float _jumpBufferCounter;

        private bool _isDashing;
        private float _dashTimeLeft;
        private float _dashCooldownLeft;
        private int _facing = 1; // 1 = derecha, -1 = izquierda

        private bool _glideHeld;

        /// <summary>Energía actual del jetpack (0..maxEnergy). Útil para una barra en el HUD.</summary>
        public float Energy { get; private set; }
        public float MaxEnergy => maxEnergy;
        public float EnergyNormalized => maxEnergy > 0 ? Energy / maxEnergy : 0f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _defaultGravity = _rb.gravityScale;
            Energy = maxEnergy;
        }

        private void Update()
        {
            ReadInput();
            UpdateTimers();
        }

        private void FixedUpdate()
        {
            CheckGround();
            HandleDash();

            // Mientras se hace dash ignoramos el resto del control de movimiento.
            if (!_isDashing)
            {
                HandleHorizontalMovement();
                HandleJump();
                HandleGlide();
            }

            RegenerateEnergy();
        }

        private void ReadInput()
        {
            _horizontalInput = Input.GetAxisRaw("Horizontal");
            if (_horizontalInput != 0f)
                _facing = _horizontalInput > 0f ? 1 : -1;

            if (Input.GetKeyDown(jumpKey))
                _jumpBufferCounter = jumpBufferTime;

            if (Input.GetKeyDown(dashKey))
                TryStartDash();

            _glideHeld = Input.GetKey(glideKey);
        }

        private void UpdateTimers()
        {
            if (_jumpBufferCounter > 0f) _jumpBufferCounter -= Time.deltaTime;
            if (_dashCooldownLeft > 0f) _dashCooldownLeft -= Time.deltaTime;
            if (_coyoteCounter > 0f)    _coyoteCounter    -= Time.deltaTime;
        }

        private void CheckGround()
        {
            bool wasGrounded = _isGrounded;
            _isGrounded = groundCheck != null &&
                          Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            if (_isGrounded)
            {
                _coyoteCounter = coyoteTime;
                if (!wasGrounded) // acaba de aterrizar -> reinicia saltos aéreos
                    _airJumpsUsed = 0;
            }
        }

        private void HandleHorizontalMovement()
        {
            float targetSpeed = _horizontalInput * moveSpeed;
            float control = _isGrounded ? 1f : airControl;

            float newX = Mathf.MoveTowards(_rb.linearVelocity.x, targetSpeed,
                                           acceleration * control * Time.fixedDeltaTime);
            _rb.linearVelocity = new Vector2(newX, _rb.linearVelocity.y);

            // Orienta el sprite hacia la dirección de movimiento.
            if (_horizontalInput != 0f)
            {
                Vector3 s = transform.localScale;
                s.x = Mathf.Abs(s.x) * _facing;
                transform.localScale = s;
            }
        }

        private void HandleJump()
        {
            if (_jumpBufferCounter <= 0f) return;

            // 1) Salto desde el suelo (o dentro del coyote time): gratis.
            if (_coyoteCounter > 0f)
            {
                DoJump();
                _jumpBufferCounter = 0f;
                _coyoteCounter = 0f;
            }
            // 2) Doble salto / impulso de jetpack en el aire: consume energía.
            else if (_airJumpsUsed < maxAirJumps && Energy >= airJumpEnergyCost)
            {
                Energy -= airJumpEnergyCost;
                _airJumpsUsed++;
                DoJump();
                _jumpBufferCounter = 0f;
            }
        }

        private void DoJump()
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);
        }

        private void TryStartDash()
        {
            if (_isDashing || _dashCooldownLeft > 0f || Energy < dashEnergyCost) return;

            Energy -= dashEnergyCost;
            _isDashing = true;
            _dashTimeLeft = dashDuration;
            _dashCooldownLeft = dashCooldown;
        }

        private void HandleDash()
        {
            if (!_isDashing) return;

            // Durante el dash anulamos la gravedad y forzamos velocidad horizontal constante.
            _rb.gravityScale = 0f;
            _rb.linearVelocity = new Vector2(_facing * dashSpeed, 0f);

            _dashTimeLeft -= Time.fixedDeltaTime;
            if (_dashTimeLeft <= 0f)
            {
                _isDashing = false;
                _rb.gravityScale = _defaultGravity;
            }
        }

        private void HandleGlide()
        {
            // Solo se puede planear en el aire, cayendo y con energía disponible.
            bool canGlide = _glideHeld && !_isGrounded && _rb.linearVelocity.y < 0f && Energy > 0f;

            if (canGlide)
            {
                _rb.gravityScale = glideGravityScale;

                // Limita la velocidad de caída para un descenso suave.
                if (_rb.linearVelocity.y < -glideMaxFallSpeed)
                    _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, -glideMaxFallSpeed);

                Energy = Mathf.Max(0f, Energy - glideEnergyDrain * Time.fixedDeltaTime);
            }
            else
            {
                // Restaura la gravedad normal cuando no se planea.
                _rb.gravityScale = _defaultGravity;
            }
        }

        private void RegenerateEnergy()
        {
            // La energía del jetpack solo se recarga estando en el suelo.
            if (_isGrounded && !_isDashing)
                Energy = Mathf.Min(maxEnergy, Energy + energyRegenPerSecond * Time.fixedDeltaTime);
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
