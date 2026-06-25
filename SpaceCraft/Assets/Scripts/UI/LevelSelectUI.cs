using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Conecta los 3 botones del Canvas de la escena SeleccionNivel con el GameManager.
    ///
    /// CONFIGURACIÓN EN EL INSPECTOR:
    /// 1. Añade este script a un GameObject vacío de la escena (p. ej. "UI_Controller").
    /// 2. En el componente Button de cada botón, en el evento OnClick():
    ///    - Arrastra el objeto con este script.
    ///    - Selecciona la función LevelSelectUI -> OnLevelButton(int).
    ///    - Escribe 1, 2 o 3 según el botón.
    /// </summary>
    public class LevelSelectUI : MonoBehaviour
    {
        /// <summary>Recibe el número de nivel desde el OnClick del botón.</summary>
        public void OnLevelButton(int level)
        {
            if (GameManager.Instance == null)
            {
                Debug.LogError("[LevelSelectUI] No existe GameManager en la escena. " +
                               "Asegúrate de tener el prefab GameManager en SeleccionNivel.");
                return;
            }

            GameManager.Instance.SelectLevelAndTravel(level);
        }

        /// <summary>Botón opcional de salir del juego.</summary>
        public void OnQuitButton()
        {
            Debug.Log("[LevelSelectUI] Saliendo del juego...");
            Application.Quit();
        }
    }
}
