using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Controlador de movimiento 2D BÁSICO para los Niveles 1 y 2 (correr + salto simple).
    /// Mantiene aislada la mecánica destacada de cada nivel: el jetpack avanzado vive
    /// únicamente en JetpackController2D (Nivel 3).
    ///
    /// REQUISITOS:
    ///  - Rigidbody2D (Gravity Scale > 0, Freeze Rotation Z).
    ///  - Un Collider2D.
    ///  - Un hijo vacío 'groundCheck' en los pies y una LayerMask 'groundLayer'.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerMovement2D : MonoBehaviour
    {
        [Header("Movimiento")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float jumpForce = 11f;

        [Header("Detección de suelo")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.15f;
        [SerializeField] private LayerMask groundLayer;

        [Header("Controles")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;

        private Rigidbody2D _rb;
        private float _horizontalInput;
        private bool _isGrounded;
        private bool _jumpQueued;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        private void Update()
        {
            _horizontalInput = Input.GetAxisRaw("Horizontal");

            // Orienta el sprite hacia la dirección de movimiento.
            if (_horizontalInput != 0f)
            {
                Vector3 s = transform.localScale;
                s.x = Mathf.Abs(s.x) * Mathf.Sign(_horizontalInput);
                transform.localScale = s;
            }

            if (Input.GetKeyDown(jumpKey))
                _jumpQueued = true;
        }

        private void FixedUpdate()
        {
            _isGrounded = groundCheck != null &&
                          Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

            _rb.linearVelocity = new Vector2(_horizontalInput * moveSpeed, _rb.linearVelocity.y);

            if (_jumpQueued && _isGrounded)
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, jumpForce);

            _jumpQueued = false;
        }

        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null) return;
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
