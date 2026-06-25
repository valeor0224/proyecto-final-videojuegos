using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace SpaceCraft
{
    /// <summary>
    /// Inventario básico de recursos para el Nivel 1 (Cavernas Mineras).
    /// Guarda cuántas unidades de cada material ha extraído Rise.
    /// Colócalo en el GameObject del jugador.
    /// </summary>
    public class Inventory : MonoBehaviour
    {
        [System.Serializable] public class ResourceChangedEvent : UnityEvent<string, int> { } // (id, totalActual)

        [Tooltip("Se dispara cada vez que cambia la cantidad de un recurso. Útil para actualizar la UI/HUD.")]
        public ResourceChangedEvent onResourceChanged;

        // Diccionario id-de-recurso -> cantidad.
        private readonly Dictionary<string, int> _resources = new();

        /// <summary>Añade 'amount' unidades del recurso indicado.</summary>
        public void Add(string resourceId, int amount = 1)
        {
            if (string.IsNullOrEmpty(resourceId) || amount <= 0) return;

            _resources.TryGetValue(resourceId, out int current);
            current += amount;
            _resources[resourceId] = current;

            onResourceChanged?.Invoke(resourceId, current);
            Debug.Log($"[Inventory] {resourceId} = {current}");
        }

        /// <summary>Devuelve cuántas unidades hay del recurso (0 si no existe).</summary>
        public int GetAmount(string resourceId)
        {
            _resources.TryGetValue(resourceId, out int current);
            return current;
        }

        /// <summary>Acceso de solo lectura a todo el inventario.</summary>
        public IReadOnlyDictionary<string, int> All => _resources;
    }
}
