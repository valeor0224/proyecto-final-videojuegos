# SpaceCraft — Beta

Plataformas de precisión 2D ambientado en un futuro postapocalíptico.
Controlas a **Rise**, la última astronauta, con un traje espacial avanzado.

Esta Beta valida el **flujo de escenas** y **una mecánica aislada por nivel**.

> **Unity:** 6 (6000.4.x) · **Render:** 2D (URP o Built-in) · **Lenguaje:** C#

---

## 0. Inicio rápido (recomendado)

Hay un generador automático que crea las 5 escenas, coloca todo (cámara, jugador,
minerales, enemigo, plataformas, meta), genera sprites/prefabs placeholder, cablea
las referencias y registra las escenas en Build Settings EN ORDEN:

1. Abre el proyecto en Unity 6.
2. Menú **`Tools > SpaceCraft > Construir escenas (Auto)`**.
3. Se abre **SeleccionNivel** → pulsa **Play**.

El builder es idempotente (puedes re-ejecutarlo) y vive en
`Assets/Scripts/Editor/SpaceCraftSceneBuilder.cs`. Los assets generados van a
`Assets/_Generated/`. Si prefieres montarlo a mano, sigue la sección 3.

> El **GameManager se autoinstancia en runtime** (`GameManager.EnsureExists`,
> `[RuntimeInitializeOnLoadMethod]`), así que el flujo funciona aunque pulses Play
> directamente en un nivel suelto. No hace falta colocarlo en ninguna escena.

---

## 1. Flujo de escenas

```
SeleccionNivel ─(clic N1/2/3)─► LlegadaNave ─(fin animación)─► NivelN ─(objetivo)─► Ganaste ─► SeleccionNivel
```

El `GameManager` persiste entre escenas (`DontDestroyOnLoad`) y recuerda qué nivel
cargar tras la cinemática de la nave. Al completar el objetivo de un nivel se carga
la escena **Ganaste**, que vuelve sola al menú.

### Objetivo de cada nivel
- **Nivel 1:** recolectar TODOS los minerales (`CollectionGoal`).
- **Nivel 2:** derrotar a "El Vigía" con la pistola (`CombatGoal`).
- **Nivel 3:** llegar a la plataforma-meta (`LevelGoal`).

### Reglas comunes (los 3 niveles)
- **Límites del mapa:** muros a los lados y techo; no puedes salirte.
- **Caer = morir:** bajo el nivel hay una `ZonaDeMuerte` que reinicia el nivel.
- **Pausa:** tecla **ESC** abre un panel con **Continuar** y **Menú**.

### Build Settings (orden obligatorio)
`File > Build Settings... > Scenes In Build`:

| Índice | Escena         |
|:------:|----------------|
| 0      | SeleccionNivel |
| 1      | LlegadaNave    |
| 2      | Nivel1         |
| 3      | Nivel2         |
| 4      | Nivel3         |
| 5      | Ganaste        |

Los nombres deben coincidir **exactamente** con los del `GameManager` (sensible a mayúsculas).

---

## 2. Estructura de scripts

```
Assets/Scripts/
├── Core/
│   └── GameManager.cs            ← Singleton persistente + flujo de escenas (autoinstanciado)
├── UI/
│   ├── LevelSelectUI.cs          ← Botones del menú → GameManager
│   ├── PauseMenu.cs              ← Pausa con ESC (Continuar / Menú)
│   └── WinScreen.cs              ← Escena Ganaste: vuelve al menú
├── Cinematics/
│   └── ShipArrivalController.cs  ← LlegadaNave: anima y carga el nivel
├── Player/
│   ├── PlayerMovement2D.cs       ← Movimiento básico (correr + salto) para Niveles 1 y 2
│   └── PlayerDeath.cs            ← Reinicia el nivel cuando Rise muere
├── Shared/
│   ├── Health.cs                 ← Vida/daño reutilizable (enemigo y Rise)
│   └── KillZone.cs               ← Zona de muerte: caer reinicia el nivel
├── Level1_Collection/
│   ├── Inventory.cs              ← Inventario básico de recursos
│   ├── MineralCollectible.cs     ← Mineral: colisión → suma recurso → se destruye
│   └── CollectionGoal.cs         ← Gana al recolectar todos los minerales
├── Level2_Combat/
│   ├── EnemyAI.cs                ← "El Vigía": patrulla + daño por contacto
│   ├── PlayerCombat.cs           ← Disparo de la pistola de Rise
│   ├── Projectile.cs             ← Proyectil que aplica daño a Health
│   └── CombatGoal.cs             ← Gana al derrotar al enemigo
├── Level3_Platforming/
│   ├── JetpackController2D.cs    ← Movement Engine: salto doble, dash, planeo, energía
│   └── LevelGoal.cs              ← Plataforma meta del nivel
└── Editor/
    └── SpaceCraftSceneBuilder.cs ← Tools > SpaceCraft > Construir escenas (Auto)
```

> **Mecánica aislada por nivel:** los Niveles 1 y 2 usan `PlayerMovement2D` (solo
> correr + saltar) para que su mecánica destacada (recolección / combate) sea la
> protagonista. El jetpack avanzado (doble salto, dash, planeo) vive **solo** en el
> Nivel 3 con `JetpackController2D`.

---

## 3. Montaje paso a paso

### Tags requeridos
`Edit > Project Settings > Tags and Layers`:
- Tag **`Player`** en el GameObject de Rise.
- Layer **`Ground`** para suelo y plataformas (usado por el ground check del Nivel 3).

### GameManager
Se **autoinstancia en runtime** (`[RuntimeInitializeOnLoadMethod]`), así que normalmente
no tienes que hacer nada. Si quieres ajustar los nombres de escena desde el Inspector,
crea un GameObject `GameManager` en **SeleccionNivel** con `GameManager.cs` (el duplicado
se descarta solo) o edita los valores por defecto en el script.

### SeleccionNivel
1. `GameObject > UI > Canvas` y crea 3 **Button**.
2. Añade `LevelSelectUI.cs` a un objeto vacío (p. ej. `UI_Controller`).
3. En cada Button → `OnClick()`:
   - Arrastra `UI_Controller`.
   - Función: `LevelSelectUI.OnLevelButton`.
   - Valor: `1`, `2` o `3`.

### LlegadaNave
1. Coloca la nave con un `Animator` (animación de aterrizaje) o un movimiento simple.
2. Añade `ShipArrivalController.cs` a un objeto de la escena.
   - **Modo temporizador:** deja `Auto Load By Timer = ON` y ajusta `Cinematic Duration`.
   - **Modo Animation Event:** ponlo en `OFF` y añade un Animation Event en el último
     frame del clip que llame a `OnAnimationFinished()`.

### Nivel 1 — Recolección (Cavernas Mineras)
- **Rise:** `Rigidbody2D` + `Collider2D` + `Inventory.cs` + tag `Player`.
- **Minerales:** sprite + `Collider2D` (Is Trigger) + `MineralCollectible.cs`.
  Ajusta `Resource Id` (p. ej. "Cristal", "Hierro") y `Amount`. Espárcelos por el mapa.
- Al tocarlos: suman al inventario y se destruyen. Mira la consola para ver el conteo.

### Nivel 2 — Combate (Ciudad en el Desierto)
- **El Vigía:** sprite + `Rigidbody2D` (Kinematic) + `Collider2D` + `EnemyAI.cs` + `Health.cs`.
  Ajusta `Patrol Distance` (verás el rango como gizmo amarillo).
- **Rise:** `PlayerCombat.cs` + un hijo vacío `FirePoint` como punto de disparo.
- **Proyectil:** crea un prefab con sprite + `Collider2D` (Is Trigger) + `Projectile.cs`
  y asígnalo en `PlayerCombat`. Dispara con **J** por defecto.
- El Vigía con `Health` se destruye al recibir suficiente daño (`onDeath`).

### Nivel 3 — Plataformeo (Movement Engine / Jetpack)
- **Rise:** `Rigidbody2D` (Gravity Scale > 0, Freeze Rotation Z) + `Collider2D`
  (Capsule recomendado) + `JetpackController2D.cs`.
- Crea un hijo vacío `GroundCheck` en los pies y asígnalo. Pon `Ground Layer = Ground`.
- Plataformas en la layer `Ground`. Añade la meta lejana con `LevelGoal.cs`
  (Collider2D Is Trigger) y un `KillZone.cs` largo bajo el vacío.

**Controles del Nivel 3 (por defecto):**

| Acción            | Tecla            | Notas                                   |
|-------------------|------------------|-----------------------------------------|
| Mover             | A/D o ←/→        | Eje "Horizontal"                        |
| Saltar / Doble salto | Espacio       | El 2.º salto gasta energía              |
| Dash lateral      | Shift Izq.       | Impulso rápido, gasta energía + cooldown|
| Planeo            | Ctrl Izq. (mantener) | Caída lenta, drena energía           |

La energía solo se recarga **tocando el suelo** (`Energy` / `EnergyNormalized`
están expuestas para una barra de HUD).

---

## 4. Notas de compatibilidad

- El código usa la API de **Unity 6**: `Rigidbody2D.linearVelocity`.
  En **Unity 2022 o anterior**, reemplaza `linearVelocity` por `velocity`
  (en `JetpackController2D.cs`, `PlayerMovement2D.cs` y `KillZone.cs`).
- **Input:** se usa el **Input Manager clásico** (`Input.GetAxis/GetKey`). Asegúrate de que
  `Edit > Project Settings > Player > Active Input Handling` esté en **"Input Manager (Old)"**
  o **"Both"** (si está solo en el nuevo Input System, ni el movimiento ni los botones de UI
  responderán).
- Todos los scripts están en el namespace **`SpaceCraft`**.

---

## 5. Prueba rápida

1. Abre **SeleccionNivel**, pulsa Play.
2. Clic en un nivel → debería ir a **LlegadaNave** → tras la animación, al nivel elegido.
3. Si vas directo a un nivel sin pasar por el menú, el gameplay funciona, pero el flujo
   de "volver al menú" necesita que el `GameManager` exista (empieza siempre por SeleccionNivel).
