using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Zona de muerte: el "vacío" bajo el nivel. Cuando Rise cae en ella,
    /// muere y el nivel se reinicia desde cero.
    /// Coloca un trigger ancho y largo por debajo de todas las plataformas.
    ///
    /// REQUISITOS: un Collider2D con "Is Trigger" activado.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class KillZone : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";

        private bool _triggered;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_triggered || !other.CompareTag(playerTag)) return;

            _triggered = true;
            Debug.Log("[KillZone] Rise cayó al vacío. Reiniciando nivel...");

            if (GameManager.Instance != null)
                GameManager.Instance.RestartCurrentLevel();
        }
    }
}
