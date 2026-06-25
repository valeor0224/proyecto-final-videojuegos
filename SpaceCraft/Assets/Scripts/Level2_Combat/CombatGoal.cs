using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Objetivo del Nivel 2: derrotar al enemigo ("El Vigía").
    /// Se suscribe al evento de muerte del Health del enemigo y, cuando muere,
    /// carga la escena de victoria ("Ganaste").
    /// Coloca este componente en un objeto vacío y asigna 'targetEnemy'.
    /// </summary>
    public class CombatGoal : MonoBehaviour
    {
        [Tooltip("Health del enemigo cuya muerte completa el nivel.")]
        [SerializeField] private Health targetEnemy;

        private void Start()
        {
            if (targetEnemy != null)
                targetEnemy.onDeath.AddListener(Win);
            else
                Debug.LogWarning("[CombatGoal] Falta asignar 'targetEnemy'.");
        }

        private void Win()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.LoadWinScene();
        }
    }
}
