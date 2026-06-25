using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Menú de pausa de los niveles. Al pulsar ESC pausa el juego (Time.timeScale = 0)
    /// y muestra un panel con dos opciones: Continuar y volver al Menú (SeleccionNivel).
    ///
    /// Conecta el panel en 'pausePanel' y, en los botones del panel:
    ///  - Botón Continuar -> PauseMenu.Resume()
    ///  - Botón Menú      -> PauseMenu.ReturnToMenu()
    /// </summary>
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private KeyCode pauseKey = KeyCode.Escape;

        private bool _paused;

        private void Awake()
        {
            // Asegura que el panel empieza oculto y el tiempo a velocidad normal.
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        private void Update()
        {
            // Update se ejecuta aunque Time.timeScale sea 0, así que ESC alterna la pausa.
            if (Input.GetKeyDown(pauseKey))
                Toggle();
        }

        public void Toggle()
        {
            if (_paused) Resume();
            else Pause();
        }

        public void Pause()
        {
            _paused = true;
            if (pausePanel != null) pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            _paused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        public void ReturnToMenu()
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToLevelSelect();
        }
    }
}
