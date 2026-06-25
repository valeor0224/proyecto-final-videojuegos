using UnityEngine;
using UnityEngine.Events;

namespace SpaceCraft
{
    /// <summary>
    /// Meta del nivel: la "plataforma lejana" que Rise debe alcanzar (Nivel 3).
    /// Cuando el jugador entra en el trigger, dispara onReached y carga la escena
    /// de victoria ("Ganaste").
    ///
    /// REQUISITOS: un Collider2D con "Is Trigger" activado.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelGoal : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";

        [Tooltip("Eventos al alcanzar la meta (sonido, efectos, etc.).")]
        public UnityEvent onReached;

        private bool _reached;

        private void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_reached || !other.CompareTag(playerTag)) return;

            _reached = true;
            Debug.Log("[LevelGoal] ¡Nivel completado!");
            onReached?.Invoke();

            if (GameManager.Instance != null)
                GameManager.Instance.LoadWinScene();
        }
    }
}
