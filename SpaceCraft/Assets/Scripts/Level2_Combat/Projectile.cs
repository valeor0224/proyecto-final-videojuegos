using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Proyectil de la pistola de Rise (Nivel 2). Viaja en línea recta y aplica daño
    /// al primer objeto con Health que toca (El Vigía). Se autodestruye al impactar
    /// o tras 'lifeTime' segundos.
    ///
    /// Si tiene Rigidbody2D (Dynamic, gravity 0) se mueve por velocidad, lo que garantiza
    /// que los eventos de trigger se disparen contra el enemigo. Si no, se mueve por transform.
    ///
    /// REQUISITOS (en el prefab): Collider2D con "Is Trigger" activado.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class Projectile : MonoBehaviour
    {
        [SerializeField] private float speed = 14f;
        [SerializeField] private int damage = 1;
        [SerializeField] private float lifeTime = 3f;

        [Tooltip("Tag que el proyectil ignora (para no dañar a quien dispara).")]
        [SerializeField] private string ignoreTag = "Player";

        private Vector2 _direction = Vector2.right;
        private Rigidbody2D _rb;

        private void Awake() => _rb = GetComponent<Rigidbody2D>();

        /// <summary>Lanza el proyectil en la dirección indicada. Lo llama PlayerCombat.</summary>
        public void Launch(Vector2 direction)
        {
            _direction = direction.normalized;

            // Orienta el sprite según la dirección.
            if (_direction.x < 0f)
            {
                Vector3 s = transform.localScale;
                s.x = -Mathf.Abs(s.x);
                transform.localScale = s;
            }

            if (_rb != null)
                _rb.linearVelocity = _direction * speed;

            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            // Solo si no hay Rigidbody2D que lo mueva por física.
            if (_rb == null)
                transform.Translate(_direction * speed * Time.deltaTime, Space.World);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.CompareTag(ignoreTag)) return;

            var health = other.GetComponentInParent<Health>();
            if (health != null)
                health.TakeDamage(damage);

            Destroy(gameObject);
        }
    }
}
