using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Mineral recolectable del Nivel 1 (Cavernas Mineras).
    /// Cuando Rise lo toca con su herramienta de extracción, suma el recurso
    /// al inventario del jugador, avisa al objetivo del nivel y se destruye.
    ///
    /// REQUISITOS:
    ///  - Este GameObject necesita un Collider2D con "Is Trigger" activado.
    ///  - El jugador debe tener el tag indicado (por defecto "Player") y un componente Inventory.
    ///  - El jugador necesita un Rigidbody2D para que se disparen los triggers 2D.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class MineralCollectible : MonoBehaviour
    {
        /// <summary>Se dispara cada vez que se recolecta cualquier mineral (lo escucha CollectionGoal).</summary>
        public static event System.Action OnAnyCollected;

        [Header("Recurso que otorga")]
        [SerializeField] private string resourceId = "Cristal";
        [SerializeField] private int amount = 1;

        [Header("Filtro de jugador")]
        [SerializeField] private string playerTag = "Player";

        [Header("Feedback (opcional)")]
        [SerializeField] private GameObject collectVfx;
        [SerializeField] private AudioClip collectSfx;

        private bool _collected;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_collected || !other.CompareTag(playerTag)) return;

            var inventory = other.GetComponentInParent<Inventory>();
            if (inventory != null)
                inventory.Add(resourceId, amount);

            _collected = true;
            OnAnyCollected?.Invoke();

            if (collectVfx != null)
                Instantiate(collectVfx, transform.position, Quaternion.identity);
            if (collectSfx != null)
                AudioSource.PlayClipAtPoint(collectSfx, transform.position);

            Destroy(gameObject);
        }
    }
}
