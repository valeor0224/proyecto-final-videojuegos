using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// IA básica de "El Vigía" para el Nivel 2 (Ciudad en el Desierto).
    /// Patrulla horizontalmente entre dos límites calculados a partir de su
    /// posición inicial y daña a Rise por contacto (con cooldown).
    ///
    /// REQUISITOS:
    ///  - Rigidbody2D (Body Type: Kinematic recomendado para un patrullaje estable,
    ///    o Dynamic con Freeze Rotation Z si quieres físicas).
    ///  - Un Collider2D.
    ///  - Un componente Health (para que el jugador pueda destruirlo).
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class EnemyAI : MonoBehaviour
    {
        [Header("Patrulla")]
        [Tooltip("Distancia TOTAL del recorrido, centrada en la posición inicial.")]
        [SerializeField] private float patrolDistance = 4f;
        [SerializeField] private float moveSpeed = 2f;
        [SerializeField] private bool startMovingRight = true;

        [Header("Daño por contacto")]
        [SerializeField] private int contactDamage = 1;
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private float damageCooldown = 1f;

        private Rigidbody2D _rb;
        private float _leftBound;
        private float _rightBound;
        private int _direction;
        private float _lastDamageTime = -999f;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();

            float startX = transform.position.x;
            _leftBound = startX - patrolDistance * 0.5f;
            _rightBound = startX + patrolDistance * 0.5f;
            _direction = startMovingRight ? 1 : -1;
            ApplyFacing();
        }

        private void FixedUpdate()
        {
            // Avanza en la dirección actual.
            Vector2 pos = _rb.position;
            pos.x += _direction * moveSpeed * Time.fixedDeltaTime;

            // Al alcanzar un límite, ajusta y da media vuelta.
            if (pos.x <= _leftBound)
            {
                pos.x = _leftBound;
                _direction = 1;
                ApplyFacing();
            }
            else if (pos.x >= _rightBound)
            {
                pos.x = _rightBound;
                _direction = -1;
                ApplyFacing();
            }

            _rb.MovePosition(pos);
        }

        // Orienta el sprite hacia la dirección de movimiento.
        private void ApplyFacing()
        {
            Vector3 s = transform.localScale;
            s.x = Mathf.Abs(s.x) * (_direction >= 0 ? 1 : -1);
            transform.localScale = s;
        }

        private void OnCollisionEnter2D(Collision2D collision) => TryDamage(collision.collider);
        private void OnCollisionStay2D(Collision2D collision) => TryDamage(collision.collider);
        private void OnTriggerEnter2D(Collider2D other) => TryDamage(other);
        private void OnTriggerStay2D(Collider2D other) => TryDamage(other);

        private void TryDamage(Collider2D other)
        {
            if (!other.CompareTag(playerTag)) return;
            if (Time.time - _lastDamageTime < damageCooldown) return;

            var health = other.GetComponentInParent<Health>();
            if (health != null)
            {
                health.TakeDamage(contactDamage);
                _lastDamageTime = Time.time;
            }
        }

        // Dibuja el rango de patrulla en la vista de escena para ajustarlo fácilmente.
        private void OnDrawGizmosSelected()
        {
            float centerX = Application.isPlaying ? (_leftBound + _rightBound) * 0.5f : transform.position.x;
            Vector3 left  = new Vector3(centerX - patrolDistance * 0.5f, transform.position.y, 0f);
            Vector3 right = new Vector3(centerX + patrolDistance * 0.5f, transform.position.y, 0f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(left, right);
            Gizmos.DrawWireSphere(left, 0.15f);
            Gizmos.DrawWireSphere(right, 0.15f);
        }
    }
}
