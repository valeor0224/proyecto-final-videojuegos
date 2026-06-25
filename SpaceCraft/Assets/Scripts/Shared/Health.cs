using UnityEngine;
using UnityEngine.Events;

namespace SpaceCraft
{
    /// <summary>
    /// Sistema de salud/daño reutilizable.
    /// Lo usan tanto el enemigo "El Vigía" (Nivel 2) como Rise.
    /// Cuando la vida llega a 0 dispara onDeath y (opcionalmente) destruye el objeto.
    /// </summary>
    [DisallowMultipleComponent]
    public class Health : MonoBehaviour
    {
        [Header("Vida")]
        [SerializeField] private int maxHealth = 3;
        [SerializeField] private bool destroyOnDeath = true;

        [System.Serializable] public class HealthChangedEvent : UnityEvent<int, int> { } // (actual, max)

        [Header("Eventos")]
        public HealthChangedEvent onHealthChanged;
        public UnityEvent onDeath;

        public int Current { get; private set; }
        public int Max => maxHealth;
        public bool IsDead => Current <= 0;

        private void Awake() => Current = maxHealth;

        public void TakeDamage(int amount)
        {
            if (IsDead || amount <= 0) return;

            Current = Mathf.Max(0, Current - amount);
            onHealthChanged?.Invoke(Current, maxHealth);

            if (IsDead)
            {
                onDeath?.Invoke();
                if (destroyOnDeath)
                    Destroy(gameObject);
            }
        }

        public void Heal(int amount)
        {
            if (IsDead || amount <= 0) return;

            Current = Mathf.Min(maxHealth, Current + amount);
            onHealthChanged?.Invoke(Current, maxHealth);
        }
    }
}
