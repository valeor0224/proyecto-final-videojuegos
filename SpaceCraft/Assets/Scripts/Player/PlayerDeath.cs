using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Reinicia el nivel cuando Rise muere (su Health llega a 0), por ejemplo
    /// tras recibir suficiente daño de "El Vigía" en el Nivel 2.
    /// Trabaja junto al componente Health del jugador.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class PlayerDeath : MonoBehaviour
    {
        private void Awake()
        {
            GetComponent<Health>().onDeath.AddListener(HandleDeath);
        }

        private void HandleDeath()
        {
            Debug.Log("[PlayerDeath] Rise ha muerto. Reiniciando nivel...");
            if (GameManager.Instance != null)
                GameManager.Instance.RestartCurrentLevel();
        }
    }
}
