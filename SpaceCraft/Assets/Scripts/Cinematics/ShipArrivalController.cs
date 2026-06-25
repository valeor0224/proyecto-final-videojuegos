using System.Collections;
using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Controla la escena de transición "LlegadaNave".
    /// Reproduce la animación de aterrizaje de la nave de Rise y, al terminar,
    /// carga automáticamente el nivel que se seleccionó previamente en el menú.
    ///
    /// Dos modos de finalización:
    ///  - autoLoadByTimer = true  -> espera 'cinematicDuration' y carga el nivel.
    ///  - autoLoadByTimer = false -> llama a OnAnimationFinished() desde un
    ///                               Animation Event al final del clip de aterrizaje.
    /// </summary>
    public class ShipArrivalController : MonoBehaviour
    {
        [Header("Cinemática")]
        [Tooltip("Duración (segundos) antes de cargar el nivel cuando se usa el temporizador.")]
        [SerializeField] private float cinematicDuration = 3f;

        [Tooltip("ON: carga por temporizador. OFF: espera un Animation Event que llame a OnAnimationFinished().")]
        [SerializeField] private bool autoLoadByTimer = true;

        private bool _loading;

        private void Start()
        {
            if (autoLoadByTimer)
                StartCoroutine(WaitAndLoad());
        }

        private IEnumerator WaitAndLoad()
        {
            yield return new WaitForSeconds(cinematicDuration);
            LoadLevel();
        }

        /// <summary>
        /// Conéctalo a un Animation Event situado en el último frame del clip
        /// de aterrizaje para cargar el nivel justo cuando termina la animación.
        /// </summary>
        public void OnAnimationFinished()
        {
            LoadLevel();
        }

        private void LoadLevel()
        {
            if (_loading) return; // evita doble carga (timer + animation event)
            _loading = true;

            if (GameManager.Instance != null)
            {
                GameManager.Instance.LoadSelectedLevel();
            }
            else
            {
                Debug.LogError("[ShipArrivalController] No existe GameManager. " +
                               "¿Iniciaste el juego desde SeleccionNivel?");
            }
        }
    }
}
