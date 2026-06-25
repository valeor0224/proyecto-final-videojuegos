using UnityEngine;
using UnityEngine.SceneManagement;

namespace SpaceCraft
{
    /// <summary>
    /// Gestor central de SpaceCraft.
    /// Persiste entre escenas (Singleton + DontDestroyOnLoad) y recuerda qué nivel
    /// debe cargarse DESPUÉS de la cinemática de llegada de la nave (LlegadaNave).
    ///
    /// Flujo: SeleccionNivel -> (clic) -> LlegadaNave -> NivelN -> (objetivo) -> Ganaste -> SeleccionNivel
    /// </summary>
    [DisallowMultipleComponent]
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Nombres de escenas (deben coincidir EXACTAMENTE con Build Settings)")]
        [SerializeField] private string levelSelectScene = "SeleccionNivel";
        [SerializeField] private string arrivalScene = "LlegadaNave";
        [SerializeField] private string winScene = "Ganaste";

        [Tooltip("Índice 0 = Nivel 1, índice 1 = Nivel 2, índice 2 = Nivel 3")]
        [SerializeField] private string[] levelScenes = { "Nivel1", "Nivel2", "Nivel3" };

        /// <summary>Nivel seleccionado en el menú (1, 2 o 3). 0 = ninguno seleccionado.</summary>
        public int SelectedLevel { get; private set; }

        private void Awake()
        {
            // Patrón Singleton persistente: si ya existe una instancia, esta sobra.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// Red de seguridad: garantiza que SIEMPRE exista un GameManager, aunque pulses Play
        /// directamente en un nivel suelto (sin pasar por SeleccionNivel).
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void EnsureExists()
        {
            if (Instance != null) return;
            var go = new GameObject("GameManager (Auto)");
            go.AddComponent<GameManager>(); // Awake se encarga de Instance + DontDestroyOnLoad
        }

        /// <summary>
        /// Llamado desde los botones del menú (SeleccionNivel).
        /// Guarda el nivel elegido y lanza la cinemática de llegada de la nave.
        /// </summary>
        public void SelectLevelAndTravel(int level)
        {
            if (level < 1 || level > levelScenes.Length)
            {
                Debug.LogError($"[GameManager] Nivel inválido: {level}. Rango válido: 1..{levelScenes.Length}");
                return;
            }

            SelectedLevel = level;
            Time.timeScale = 1f;
            SceneManager.LoadScene(arrivalScene);
        }

        /// <summary>
        /// Llamado al terminar la animación de la nave (desde ShipArrivalController).
        /// Carga el nivel que se seleccionó previamente en el menú.
        /// </summary>
        public void LoadSelectedLevel()
        {
            Time.timeScale = 1f;

            if (SelectedLevel < 1)
            {
                Debug.LogWarning("[GameManager] No hay nivel seleccionado. Volviendo al menú.");
                SceneManager.LoadScene(levelSelectScene);
                return;
            }

            SceneManager.LoadScene(levelScenes[SelectedLevel - 1]);
        }

        /// <summary>Carga la escena de victoria. La llaman los objetivos de cada nivel.</summary>
        public void LoadWinScene()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(winScene);
        }

        /// <summary>Reinicia el nivel actual (muerte por caída, derrota, etc.).</summary>
        public void RestartCurrentLevel()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        /// <summary>Vuelve al menú de selección y limpia el nivel elegido.</summary>
        public void ReturnToLevelSelect()
        {
            Time.timeScale = 1f;
            SelectedLevel = 0;
            SceneManager.LoadScene(levelSelectScene);
        }
    }
}
