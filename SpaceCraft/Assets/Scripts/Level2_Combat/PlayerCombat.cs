using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Sistema de ataque de Rise para el Nivel 2.
    /// Dispara balas con el CLIC IZQUIERDO del mouse o la tecla J, en la dirección
    /// a la que mira el personaje. Cada bala aplica daño a cualquier objeto con Health
    /// (El Vigía), hasta eliminarlo.
    ///
    /// CONFIGURACIÓN:
    ///  - Asigna 'projectilePrefab' (un prefab con el script Projectile).
    ///  - Crea un objeto vacío hijo del arma como 'firePoint' (boca del cañón).
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Disparo")]
        [SerializeField] private Projectile projectilePrefab;
        [SerializeField] private Transform firePoint;
        [Tooltip("Balas por segundo (manteniendo pulsado).")]
        [SerializeField] private float fireRate = 3f;

        [Header("Audio")]
        [SerializeField] private AudioClip shootSfx;

        [Header("Controles")]
        [SerializeField] private bool fireWithMouse = true;
        [SerializeField] private KeyCode fireKey = KeyCode.J;

        private float _nextFireTime;

        private void Update()
        {
            bool firing = Input.GetKey(fireKey) ||
                          (fireWithMouse && Input.GetButton("Fire1"));

            if (firing && Time.time >= _nextFireTime)
            {
                Shoot();
                _nextFireTime = Time.time + 1f / Mathf.Max(0.01f, fireRate);
            }
        }

        private void Shoot()
        {
            if (projectilePrefab == null || firePoint == null)
            {
                Debug.LogWarning("[PlayerCombat] Falta asignar 'projectilePrefab' o 'firePoint'.");
                return;
            }

            // Dirección según hacia dónde mira el personaje (escala X: 1 = derecha, -1 = izquierda).
            float dir = Mathf.Sign(transform.localScale.x);
            Projectile projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            projectile.Launch(new Vector2(dir, 0f));

            if (shootSfx != null)
                AudioSource.PlayClipAtPoint(shootSfx, firePoint.position);
        }
    }
}
