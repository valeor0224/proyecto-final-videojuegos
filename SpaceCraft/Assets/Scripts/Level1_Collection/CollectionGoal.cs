using UnityEngine;

namespace SpaceCraft
{
    /// <summary>
    /// Objetivo del Nivel 1: recolectar TODOS los minerales de la escena.
    /// Cuenta los minerales presentes al iniciar y, cuando se han recolectado todos,
    /// carga la escena de victoria ("Ganaste").
    /// Coloca este componente en un objeto vacío de la escena (p. ej. "Objetivo").
    /// </summary>
    public class CollectionGoal : MonoBehaviour
    {
        private int _remaining;

        private void Start()
        {
            _remaining = Object.FindObjectsByType<MineralCollectible>(FindObjectsSortMode.None).Length;
            MineralCollectible.OnAnyCollected += HandleCollected;

            if (_remaining <= 0)
                Debug.LogWarning("[CollectionGoal] No hay minerales en la escena.");
        }

        private void OnDestroy()
        {
            MineralCollectible.OnAnyCollected -= HandleCollected;
        }

        private void HandleCollected()
        {
            _remaining--;
            Debug.Log($"[CollectionGoal] Minerales restantes: {_remaining}");

            if (_remaining <= 0 && GameManager.Instance != null)
                GameManager.Instance.LoadWinScene();
        }
    }
}
