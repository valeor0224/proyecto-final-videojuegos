#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using SpaceCraft;

/// <summary>
/// Generador automático de las 6 escenas de SpaceCraft (Beta).
/// Construye la jerarquía 2D de cada escena (cámara, límites del mapa, zona de muerte,
/// menú de pausa, jugador, objetivos), genera sprites/prefabs placeholder, cablea las
/// referencias [SerializeField] y registra las escenas en Build Settings EN ORDEN.
///
/// Ejecútalo desde: Tools > SpaceCraft > Construir escenas (Auto).
/// Es idempotente: cada ejecución regenera las escenas desde cero.
///
/// El GameManager NO se coloca en ninguna escena: se autoinstancia en runtime.
/// </summary>
public static class SpaceCraftSceneBuilder
{
    private const string ScenesDir = "Assets/Scenes";
    private const string GenDir    = "Assets/_Generated";
    private const string SpriteDir = GenDir + "/Sprites";
    private const string PrefabDir = GenDir + "/Prefabs";

    // Marco del área jugable (encaja con cámara ortográfica size 6 en 16:9).
    private const float WallX   = 10.3f;
    private const float CeilingY = 6.8f;
    private const float KillY   = -9f;

    private static Sprite _square;
    private static Font _uiFont;
    private static PhysicsMaterial2D _frictionless;

    [MenuItem("Tools/SpaceCraft/Construir escenas (Auto)")]
    public static void BuildAll()
    {
        EnsureTagsAndLayers();
        EnsureFolders();

        var built = new List<string>
        {
            BuildSeleccionNivel(),
            BuildLlegadaNave(),
            BuildNivel1(),
            BuildNivel2(),
            BuildNivel3(),
            BuildGanaste(),
        };

        var buildScenes = new EditorBuildSettingsScene[built.Count];
        for (int i = 0; i < built.Count; i++)
            buildScenes[i] = new EditorBuildSettingsScene(built[i], true);
        EditorBuildSettings.scenes = buildScenes;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorSceneManager.OpenScene(built[0], OpenSceneMode.Single);

        Debug.Log("<color=cyan><b>[SpaceCraft]</b></color> 6 escenas construidas y registradas en Build Settings. " +
                  "Abre <b>SeleccionNivel</b> y pulsa Play.");
        EditorUtility.DisplayDialog("SpaceCraft",
            "6 escenas construidas y añadidas a Build Settings:\n\n" +
            "  0. SeleccionNivel\n  1. LlegadaNave\n  2. Nivel1\n  3. Nivel2\n  4. Nivel3\n  5. Ganaste\n\n" +
            "Abre SeleccionNivel y pulsa Play.", "Genial");
    }

    // ===================================================================
    //  ESCENA — SeleccionNivel
    // ===================================================================
    private static string BuildSeleccionNivel()
    {
        Scene scene = NewScene();
        MakeCamera2D(new Color(0.05f, 0.06f, 0.12f));
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGo = MakeCanvas("Canvas");
        MakeUIText(canvasGo.transform, "Title", new Vector2(0.5f, 1f), new Vector2(0f, -120f),
                   new Vector2(900f, 120f), "SPACECRAFT", 72, TextAnchor.MiddleCenter);

        var uiCtrl = new GameObject("UI_Controller").AddComponent<LevelSelectUI>();
        for (int i = 1; i <= 3; i++)
        {
            float y = 60f - (i - 1) * 130f;
            Button btn = MakeUIButton(canvasGo.transform, "Btn_Nivel" + i, new Vector2(0f, y), "NIVEL " + i);
            UnityEventTools.AddIntPersistentListener(btn.onClick, new UnityAction<int>(uiCtrl.OnLevelButton), i);
        }

        return SaveScene(scene, "SeleccionNivel");
    }

    // ===================================================================
    //  ESCENA — LlegadaNave
    // ===================================================================
    private static string BuildLlegadaNave()
    {
        Scene scene = NewScene();
        MakeCamera2D(new Color(0.10f, 0.08f, 0.14f));

        MakeGround("Suelo", new Vector3(0f, -4f, 0f), new Vector2(30f, 1.5f), new Color(0.25f, 0.22f, 0.20f));
        MakeSpriteGO("Nave", new Color(0.80f, 0.82f, 0.88f), null, new Vector3(0f, 2f, 0f), new Vector3(3f, 2f, 1f));
        new GameObject("ArrivalController").AddComponent<ShipArrivalController>();

        var canvasGo = MakeCanvas("Canvas");
        MakeUIText(canvasGo.transform, "Status", new Vector2(0.5f, 0f), new Vector2(0f, 120f),
                   new Vector2(900f, 100f), "Aterrizando...", 48, TextAnchor.MiddleCenter);

        return SaveScene(scene, "LlegadaNave");
    }

    // ===================================================================
    //  ESCENA — Nivel1 (Recolección / Cavernas Mineras, rediseñado)
    // ===================================================================
    private static string BuildNivel1()
    {
        Scene scene = NewScene();
        MakeCamera2D(new Color(0.05f, 0.06f, 0.09f));
        BuildBounds();
        BuildKillZone();
        BuildPauseMenu();

        BuildHudHint("Espacio: salto/doble salto  ·  Shift: dash  ·  Ctrl: planeo  ·  Recolecta TODOS los minerales");

        Color rock  = new Color(0.28f, 0.24f, 0.22f);
        Color ledge = new Color(0.34f, 0.30f, 0.26f);
        Color deco  = new Color(0.15f, 0.12f, 0.11f);

        // Suelos laterales DESPEJADOS (sin plataformas bajas encima) + precipicio central.
        MakeGround("Suelo_Izq", new Vector3(-7f, -4f, 0f), new Vector2(6f, 1f), rock); // x -10..-4
        MakeGround("Suelo_Der", new Vector3(7f, -4f, 0f), new Vector2(6f, 1f), rock);  // x 4..10

        // Pirámide suave SOBRE el precipicio: cada escalón sube solo 1.1 (el salto llega a ~2.0)
        // y los escalones se solapan en X. El premio queda a altura cómoda y desplazado.
        var plats = new GameObject("Plataformas").transform;
        MakeGround("Escalon_1",   new Vector3(-3.0f, -2.65f, 0f), new Vector2(2.0f, 0.5f), ledge, plats);
        MakeGround("Escalon_2",   new Vector3(-1.2f, -1.55f, 0f), new Vector2(2.0f, 0.5f), ledge, plats);
        MakeGround("Escalon_3",   new Vector3(0.5f, -0.45f, 0f),  new Vector2(2.0f, 0.5f), ledge, plats); // cima
        MakeGround("Escalon_4",   new Vector3(2.3f, -1.55f, 0f),  new Vector2(2.0f, 0.5f), ledge, plats);
        MakeGround("Escalon_5",   new Vector3(3.5f, -2.65f, 0f),  new Vector2(2.0f, 0.5f), ledge, plats); // puente al suelo der
        MakeGround("Repisa_Alta", new Vector3(-1.5f, 0.65f, 0f),  new Vector2(1.8f, 0.5f), ledge, plats); // premio (cómodo desde la cima)

        // Decoración de cueva (sin colisión, fuera del camino).
        var decos = new GameObject("Decoracion").transform;
        MakeDecoration("Estalactita_1", decos, new Vector3(-6f, 5.4f, 0f), new Vector3(0.8f, 2.2f, 1f), deco);
        MakeDecoration("Estalactita_2", decos, new Vector3(-2f, 5.7f, 0f), new Vector3(0.9f, 1.6f, 1f), deco);
        MakeDecoration("Estalactita_3", decos, new Vector3(5f, 5.3f, 0f),  new Vector3(0.7f, 2.6f, 1f), deco);
        MakeDecoration("Estalagmita_1", decos, new Vector3(-9.4f, -3.0f, 0f), new Vector3(0.7f, 1.2f, 1f), deco);
        MakeDecoration("Estalagmita_2", decos, new Vector3(9.4f, -3.0f, 0f),  new Vector3(0.7f, 1.2f, 1f), deco);

        // Minerales (9): 2 suelo izq + 4 escalera + 1 premio alto + 2 suelo der. Todos alcanzables.
        var minerals = new GameObject("Minerales").transform;
        Vector3[] mp =
        {
            new Vector3(-8.5f, -3.0f, 0f),  // suelo izq
            new Vector3(-5.5f, -3.0f, 0f),  // suelo izq
            new Vector3(-3.0f, -1.85f, 0f), // sobre Escalon_1
            new Vector3(-1.2f, -0.75f, 0f), // sobre Escalon_2
            new Vector3(0.5f,  0.35f, 0f),  // sobre Escalon_3 (cima)
            new Vector3(-1.5f, 1.45f, 0f),  // sobre Repisa_Alta (premio)
            new Vector3(2.3f, -0.75f, 0f),  // sobre Escalon_4
            new Vector3(5.5f, -3.0f, 0f),   // suelo der
            new Vector3(8.5f, -3.0f, 0f),   // suelo der
        };
        for (int i = 0; i < mp.Length; i++)
            MakeMineral("Mineral_" + (i + 1).ToString("00"), minerals, mp[i]);

        // Jugador con JETPACK (doble salto, dash, planeo) + inventario para recolectar.
        GameObject player = BuildJetpackPlayer(new Vector3(-8f, -2.6f, 0f), new Color(0.20f, 0.55f, 0.90f));
        player.AddComponent<Inventory>();
        var health = player.AddComponent<Health>();
        SetBool(health, "destroyOnDeath", false);
        player.AddComponent<PlayerDeath>();

        new GameObject("Objetivo").AddComponent<CollectionGoal>();

        return SaveScene(scene, "Nivel1");
    }

    // ===================================================================
    //  ESCENA — Nivel2 (Combate / Ciudad en el Desierto + pistola)
    // ===================================================================
    private static string BuildNivel2()
    {
        Scene scene = NewScene();
        MakeCamera2D(new Color(0.55f, 0.42f, 0.25f));
        BuildBounds();
        BuildKillZone();
        BuildPauseMenu();
        BuildHudHint("Clic o J: disparar   ·   Derrota a El Vigia");

        Color sand     = new Color(0.62f, 0.50f, 0.32f);
        Color building = new Color(0.40f, 0.33f, 0.26f);

        // Suelo con precipicio: izquierda (jugador) | hueco | derecha (enemigo).
        MakeGround("Suelo_Izq", new Vector3(-5f, -4f, 0f), new Vector2(10f, 1f), sand); // x -10..0
        MakeGround("Suelo_Der", new Vector3(6f, -4f, 0f), new Vector2(8f, 1f), sand);   // x 2..10

        // Edificios de fondo (decoración).
        var decos = new GameObject("Ciudad").transform;
        MakeDecoration("Edificio_1", decos, new Vector3(-7f, -0.5f, 0f), new Vector3(2.2f, 6f, 1f), building);
        MakeDecoration("Edificio_2", decos, new Vector3(-2.5f, 0.5f, 0f), new Vector3(1.6f, 8f, 1f), building);
        MakeDecoration("Edificio_3", decos, new Vector3(8.5f, -0.2f, 0f), new Vector3(2.4f, 7f, 1f), building);

        // Jugador con pistola.
        GameObject player = BuildBasicPlayer(new Vector3(-8f, -2.5f, 0f), new Color(0.20f, 0.55f, 0.90f));

        GameObject gun = MakeSpriteGO("Pistola", new Color(0.12f, 0.12f, 0.14f), player.transform,
                                      player.transform.position + new Vector3(0.55f, -0.1f, 0f),
                                      new Vector3(0.7f, 0.25f, 1f));
        gun.GetComponent<SpriteRenderer>().sortingOrder = 2;

        var firePoint = new GameObject("FirePoint").transform;
        firePoint.SetParent(gun.transform);
        firePoint.position = gun.transform.position + new Vector3(0.45f, 0f, 0f);

        Projectile projPrefab = BuildProjectilePrefab();
        var combat = player.AddComponent<PlayerCombat>();
        SetRef(combat, "firePoint", firePoint);
        SetRef(combat, "projectilePrefab", projPrefab);

        // "El Vigía" en la plataforma derecha.
        GameObject enemy = MakeSpriteGO("ElVigia", new Color(0.85f, 0.22f, 0.22f), null,
                                        new Vector3(6f, -2.8f, 0f), new Vector3(1.2f, 1.6f, 1f));
        enemy.GetComponent<SpriteRenderer>().sortingOrder = 1;
        var erb = enemy.AddComponent<Rigidbody2D>();
        erb.bodyType = RigidbodyType2D.Kinematic;
        var ecol = enemy.AddComponent<BoxCollider2D>();
        ecol.size = Vector2.one;
        var eai = enemy.AddComponent<EnemyAI>();
        SetFloat(eai, "patrolDistance", 3f);
        var ehealth = enemy.AddComponent<Health>();

        var goal = new GameObject("Objetivo").AddComponent<CombatGoal>();
        SetRef(goal, "targetEnemy", ehealth);

        return SaveScene(scene, "Nivel2");
    }

    // ===================================================================
    //  ESCENA — Nivel3 (Plataformeo / Jetpack)
    // ===================================================================
    private static string BuildNivel3()
    {
        Scene scene = NewScene();
        MakeCamera2D(new Color(0.06f, 0.07f, 0.10f));
        BuildBounds();
        BuildKillZone();
        BuildPauseMenu();
        BuildHudHint("Espacio: salto/doble salto  ·  Shift: dash  ·  Ctrl: planeo  ·  Llega a la meta");

        Color plat    = new Color(0.30f, 0.32f, 0.38f);
        Color goalCol = new Color(0.20f, 0.55f, 0.30f);

        var plats = new GameObject("Plataformas").transform;
        MakeGround("Plataforma_Inicio", new Vector3(-8f, -3f, 0f), new Vector2(3f, 1f), plat, plats);
        MakeGround("Plataforma_1", new Vector3(-3f, -1.5f, 0f), new Vector2(2f, 0.6f), plat, plats);
        MakeGround("Plataforma_2", new Vector3(2f, 0.5f, 0f), new Vector2(2f, 0.6f), plat, plats);
        MakeGround("Plataforma_Meta", new Vector3(7f, 2.5f, 0f), new Vector2(3f, 0.6f), goalCol, plats);

        GameObject goal = MakeSpriteGO("Meta", new Color(0.30f, 0.95f, 0.45f), null,
                                       new Vector3(7f, 3.7f, 0f), new Vector3(1.2f, 2f, 1f));
        goal.GetComponent<SpriteRenderer>().sortingOrder = 2;
        var gcol = goal.AddComponent<BoxCollider2D>();
        gcol.isTrigger = true;
        gcol.size = Vector2.one;
        goal.AddComponent<LevelGoal>();

        BuildJetpackPlayer(new Vector3(-8f, -1f, 0f), new Color(0.20f, 0.55f, 0.90f));

        return SaveScene(scene, "Nivel3");
    }

    // ===================================================================
    //  ESCENA — Ganaste
    // ===================================================================
    private static string BuildGanaste()
    {
        Scene scene = NewScene();
        MakeCamera2D(new Color(0.06f, 0.10f, 0.07f));
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGo = MakeCanvas("Canvas");
        MakeUIText(canvasGo.transform, "Titulo", new Vector2(0.5f, 0.5f), new Vector2(0f, 120f),
                   new Vector2(1000f, 200f), "¡GANASTE!", 96, TextAnchor.MiddleCenter);

        var win = new GameObject("WinScreen").AddComponent<WinScreen>();
        Button btn = MakeUIButton(canvasGo.transform, "Btn_Continuar", new Vector2(0f, -80f), "CONTINUAR");
        UnityEventTools.AddVoidPersistentListener(btn.onClick, new UnityAction(win.GoToMenu));

        return SaveScene(scene, "Ganaste");
    }

    // ===================================================================
    //  LÍMITES, ZONA DE MUERTE Y PAUSA (comunes a los niveles)
    // ===================================================================
    private static void BuildBounds()
    {
        Color wall = new Color(0.10f, 0.10f, 0.13f);
        MakeWall("Muro_Izq", new Vector3(-WallX, 0f, 0f), new Vector2(0.6f, 16f), wall);
        MakeWall("Muro_Der", new Vector3(WallX, 0f, 0f), new Vector2(0.6f, 16f), wall);
        MakeWall("Techo",    new Vector3(0f, CeilingY, 0f), new Vector2(2f * WallX + 0.6f, 0.6f), wall);
    }

    private static void BuildKillZone()
    {
        GameObject kill = MakeSpriteGO("ZonaDeMuerte", new Color(0.6f, 0.1f, 0.1f, 0.18f), null,
                                       new Vector3(0f, KillY, 0f), new Vector3(40f, 2.5f, 1f));
        kill.GetComponent<SpriteRenderer>().sortingOrder = -5;
        var col = kill.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = Vector2.one;
        kill.AddComponent<KillZone>();
    }

    private static void BuildPauseMenu()
    {
        // EventSystem para que respondan los botones del panel.
        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        var canvasGo = MakeCanvas("PauseCanvas");
        canvasGo.GetComponent<Canvas>().sortingOrder = 100;

        var panel = new GameObject("PausePanel", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvasGo.transform, false);
        var prt = panel.GetComponent<RectTransform>();
        prt.anchorMin = Vector2.zero; prt.anchorMax = Vector2.one;
        prt.offsetMin = Vector2.zero; prt.offsetMax = Vector2.zero;
        panel.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.78f);

        MakeUIText(panel.transform, "Titulo", new Vector2(0.5f, 0.5f), new Vector2(0f, 180f),
                   new Vector2(800f, 120f), "PAUSA", 72, TextAnchor.MiddleCenter);

        var pause = new GameObject("PauseMenu").AddComponent<PauseMenu>();
        Button btnResume = MakeUIButton(panel.transform, "Btn_Continuar", new Vector2(0f, 20f), "CONTINUAR");
        Button btnMenu   = MakeUIButton(panel.transform, "Btn_Menu", new Vector2(0f, -110f), "MENÚ");
        UnityEventTools.AddVoidPersistentListener(btnResume.onClick, new UnityAction(pause.Resume));
        UnityEventTools.AddVoidPersistentListener(btnMenu.onClick, new UnityAction(pause.ReturnToMenu));

        SetRef(pause, "pausePanel", panel);
        panel.SetActive(false); // se serializa oculto; ESC lo muestra
    }

    // ===================================================================
    //  JUGADORES
    // ===================================================================
    private static GameObject BuildBasicPlayer(Vector3 pos, Color color)
    {
        GameObject player = MakeSpriteGO("Player", color, null, pos, new Vector3(0.9f, 1.4f, 1f));
        player.tag = "Player";
        player.GetComponent<SpriteRenderer>().sortingOrder = 1;

        ConfigurePlayerBody(player);
        var col = player.AddComponent<CapsuleCollider2D>();
        col.size = Vector2.one;
        col.sharedMaterial = FrictionlessMaterial(); // evita quedarse pegado a las paredes

        Transform groundCheck = MakeGroundCheck(player);

        var move = player.AddComponent<PlayerMovement2D>();
        SetRef(move, "groundCheck", groundCheck);
        SetMask(move, "groundLayer", GroundMask());

        player.AddComponent<Inventory>();
        var health = player.AddComponent<Health>();
        SetBool(health, "destroyOnDeath", false);
        player.AddComponent<PlayerDeath>();
        return player;
    }

    private static GameObject BuildJetpackPlayer(Vector3 pos, Color color)
    {
        GameObject player = MakeSpriteGO("Player", color, null, pos, new Vector3(0.9f, 1.4f, 1f));
        player.tag = "Player";
        player.GetComponent<SpriteRenderer>().sortingOrder = 1;

        ConfigurePlayerBody(player);
        var col = player.AddComponent<CapsuleCollider2D>();
        col.size = Vector2.one;
        col.sharedMaterial = FrictionlessMaterial(); // evita quedarse pegado a las paredes

        Transform groundCheck = MakeGroundCheck(player);

        var jet = player.AddComponent<JetpackController2D>();
        SetRef(jet, "groundCheck", groundCheck);
        SetMask(jet, "groundLayer", GroundMask());
        return player;
    }

    private static Rigidbody2D ConfigurePlayerBody(GameObject player)
    {
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        return rb;
    }

    private static Transform MakeGroundCheck(GameObject player)
    {
        var groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform);
        groundCheck.localPosition = new Vector3(0f, -0.55f, 0f);
        return groundCheck;
    }

    private static Projectile BuildProjectilePrefab()
    {
        GameObject go = MakeSpriteGO("Projectile", new Color(1f, 0.92f, 0.25f), null,
                                     Vector3.zero, new Vector3(0.55f, 0.22f, 1f));
        go.GetComponent<SpriteRenderer>().sortingOrder = 3;
        var col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.5f;
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        go.AddComponent<Projectile>();

        string path = PrefabDir + "/Projectile.prefab";
        PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        AssetDatabase.ImportAsset(path);
        return AssetDatabase.LoadAssetAtPath<Projectile>(path);
    }

    // ===================================================================
    //  HELPERS DE CREACIÓN
    // ===================================================================
    private static Scene NewScene() =>
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

    private static void MakeCamera2D(Color background)
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 6f;
        cam.backgroundColor = background;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();
    }

    private static GameObject MakeSpriteGO(string name, Color color, Transform parent, Vector3 pos, Vector3 scale)
    {
        var go = new GameObject(name);
        if (parent != null) go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = SquareSprite();
        sr.color = color;
        return go;
    }

    private static GameObject MakeGround(string name, Vector3 pos, Vector2 size, Color color, Transform parent = null)
    {
        GameObject go = MakeSpriteGO(name, color, parent, pos, new Vector3(size.x, size.y, 1f));
        go.layer = LayerMask.NameToLayer("Ground");
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one; // el sprite mide 1x1 unidad; la escala da el tamaño real
        return go;
    }

    private static GameObject MakeWall(string name, Vector3 pos, Vector2 size, Color color)
    {
        // Capa Default: bloquea al jugador pero NO cuenta como suelo (evita wall-jump).
        GameObject go = MakeSpriteGO(name, color, null, pos, new Vector3(size.x, size.y, 1f));
        var col = go.AddComponent<BoxCollider2D>();
        col.size = Vector2.one;
        return go;
    }

    private static void MakeDecoration(string name, Transform parent, Vector3 pos, Vector3 scale, Color color)
    {
        GameObject d = MakeSpriteGO(name, color, parent, pos, scale);
        d.GetComponent<SpriteRenderer>().sortingOrder = -2; // detrás de todo
    }

    private static void MakeMineral(string name, Transform parent, Vector3 pos)
    {
        GameObject m = MakeSpriteGO(name, new Color(0.25f, 0.90f, 0.90f), parent, pos, Vector3.one * 0.55f);
        m.GetComponent<SpriteRenderer>().sortingOrder = 2;
        var col = m.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.6f;
        m.AddComponent<MineralCollectible>();
    }

    private static void BuildHudHint(string text)
    {
        var canvasGo = MakeCanvas("HUD");
        MakeUIText(canvasGo.transform, "Hint", new Vector2(0.5f, 1f), new Vector2(0f, -40f),
                   new Vector2(1400f, 70f), text, 32, TextAnchor.MiddleCenter);
    }

    private static GameObject MakeCanvas(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        return go;
    }

    private static Button MakeUIButton(Transform parent, string name, Vector2 anchoredPos, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(360f, 100f);
        rt.anchoredPosition = anchoredPos;
        go.GetComponent<Image>().color = new Color(0.18f, 0.40f, 0.78f);

        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        txtGo.transform.SetParent(go.transform, false);
        var trt = txtGo.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var txt = txtGo.GetComponent<Text>();
        txt.text = label;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.fontSize = 36;
        txt.font = UIFont();

        return go.GetComponent<Button>();
    }

    private static void MakeUIText(Transform parent, string name, Vector2 anchor, Vector2 pos,
                                   Vector2 sizeDelta, string value, int fontSize, TextAnchor align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = pos;
        rt.sizeDelta = sizeDelta;
        var txt = go.GetComponent<Text>();
        txt.text = value;
        txt.alignment = align;
        txt.color = Color.white;
        txt.fontSize = fontSize;
        txt.font = UIFont();
    }

    // ===================================================================
    //  ASSETS GENERADOS Y SETUP
    // ===================================================================
    private static Sprite SquareSprite()
    {
        if (_square != null) return _square;

        string path = SpriteDir + "/Square.png";
        if (!File.Exists(path))
        {
            var tex = new Texture2D(8, 8);
            var pixels = new Color[8 * 8];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        if (importer != null && (importer.textureType != TextureImporterType.Sprite || importer.spritePixelsPerUnit != 8))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spritePixelsPerUnit = 8f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        _square = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        return _square;
    }

    private static Font UIFont()
    {
        if (_uiFont != null) return _uiFont;
        _uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                  ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        return _uiFont;
    }

    /// <summary>
    /// Material físico SIN fricción para el collider del jugador. Evita que Rise se
    /// "pegue" a las paredes/plataformas: con fricción 0, resbala en vez de quedarse colgada.
    /// </summary>
    private static PhysicsMaterial2D FrictionlessMaterial()
    {
        if (_frictionless != null) return _frictionless;

        string path = GenDir + "/Frictionless.physicsMaterial2D";
        _frictionless = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
        if (_frictionless == null)
        {
            _frictionless = new PhysicsMaterial2D("Frictionless") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(_frictionless, path);
        }
        return _frictionless;
    }

    private static int GroundMask() => 1 << LayerMask.NameToLayer("Ground");

    private static string SaveScene(Scene scene, string name)
    {
        string path = ScenesDir + "/" + name + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        return path;
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Scenes");
        EnsureFolder("Assets", "_Generated");
        EnsureFolder(GenDir, "Sprites");
        EnsureFolder(GenDir, "Prefabs");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string full = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(full))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void EnsureTagsAndLayers()
    {
        EnsureLayer("Ground");
        AssetDatabase.SaveAssets();
    }

    private static void EnsureLayer(string layer)
    {
        if (LayerMask.NameToLayer(layer) != -1) return;
        var tm = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tm.FindProperty("layers");
        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty el = layers.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(el.stringValue))
            {
                el.stringValue = layer;
                tm.ApplyModifiedProperties();
                return;
            }
        }
        Debug.LogWarning("[SpaceCraft] No hay slots de layer libres para: " + layer);
    }

    // --- Helpers para asignar campos privados [SerializeField] ---
    private static void SetRef(Object comp, string field, Object value)
    {
        var so = new SerializedObject(comp);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[SpaceCraft] Campo no encontrado: {field} en {comp}"); return; }
        p.objectReferenceValue = value;
        so.ApplyModifiedProperties();
    }

    private static void SetMask(Object comp, string field, int mask)
    {
        var so = new SerializedObject(comp);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[SpaceCraft] Campo no encontrado: {field}"); return; }
        p.intValue = mask;
        so.ApplyModifiedProperties();
    }

    private static void SetFloat(Object comp, string field, float value)
    {
        var so = new SerializedObject(comp);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[SpaceCraft] Campo no encontrado: {field}"); return; }
        p.floatValue = value;
        so.ApplyModifiedProperties();
    }

    private static void SetBool(Object comp, string field, bool value)
    {
        var so = new SerializedObject(comp);
        SerializedProperty p = so.FindProperty(field);
        if (p == null) { Debug.LogWarning($"[SpaceCraft] Campo no encontrado: {field}"); return; }
        p.boolValue = value;
        so.ApplyModifiedProperties();
    }
}
#endif
