using System.Collections;
using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Controla la escena de victoria "Ganaste".
    /// Muestra el mensaje y, tras un breve retardo, redirige automáticamente a la
    /// pantalla de selección de nivel. También expone GoToMenu() para un botón "Continuar".
    /// </summary>
    public class WinScreen : MonoBehaviour
    {
        [Tooltip("Segundos antes de volver al menú automáticamente. 0 = solo con el botón.")]
        [SerializeField] private float autoReturnDelay = 3f;

        private bool _returning;

        private void Start()
        {
            Time.timeScale = 1f; // por si venimos de una pausa
            if (autoReturnDelay > 0f)
                StartCoroutine(AutoReturn());
        }

        private IEnumerator AutoReturn()
        {
            yield return new WaitForSecondsRealtime(autoReturnDelay);
            GoToMenu();
        }

        /// <summary>Conéctalo al botón "Continuar". También se llama solo tras el retardo.</summary>
        public void GoToMenu()
        {
            if (_returning) return;
            _returning = true;

            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToLevelSelect();
        }
    }
}
