using UnityEngine;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;
using UnityEngine.UI;

public class SoccerSetupWindow : EditorWindow
{
    string wsUrl = "wss://soccer-ws.1qaz.jp";

    [MenuItem("Soccer/⚽ Open Setup")]
    public static void Open() => GetWindow<SoccerSetupWindow>("⚽ Soccer Setup");

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Soccer - Rocket League Style", EditorStyles.boldLabel);
        GUILayout.Space(6);
        wsUrl = EditorGUILayout.TextField("WebSocket URL", wsUrl);
        GUILayout.Space(14);

        GUI.backgroundColor = new Color(0.35f, 0.8f, 0.35f);
        if (GUILayout.Button("▶  BUILD SCENE", GUILayout.Height(50)))
            BuildScene();

        GUI.backgroundColor = new Color(0.85f, 0.35f, 0.35f);
        GUILayout.Space(6);
        if (GUILayout.Button("Clear Setup", GUILayout.Height(28)))
            ClearScene();

        GUI.backgroundColor = Color.white;
    }

    const string ROOT = "[Soccer Setup]";

    void ClearScene()
    {
        var root = GameObject.Find(ROOT);
        if (root) Undo.DestroyObjectImmediate(root);
    }

    void BuildScene()
    {
        ClearScene();
        EnsureTag("Ball");

        var root = new GameObject(ROOT);
        Undo.RegisterCreatedObjectUndo(root, "Soccer Setup");

        // ── FIELD ────────────────────────────────────────────────
        var fieldRoot = Child("Field", root);
        Cube("Ground",     new Vector3(0,     -0.05f, 0),   new Vector3(20, 0.1f, 30), new Color(0.18f, 0.45f, 0.18f), fieldRoot);
        Cube("WallE",      new Vector3(10.5f,  1.5f,  0),   new Vector3(1, 3, 30),     Color.gray * 1.4f, fieldRoot);
        Cube("WallW",      new Vector3(-10.5f, 1.5f,  0),   new Vector3(1, 3, 30),     Color.gray * 1.4f, fieldRoot);
        Cube("EndZ+L",     new Vector3(-7f,  1.5f,  15.5f), new Vector3(7, 3, 1),      Color.gray * 1.4f, fieldRoot);
        Cube("EndZ+R",     new Vector3( 7f,  1.5f,  15.5f), new Vector3(7, 3, 1),      Color.gray * 1.4f, fieldRoot);
        Cube("GoalZ+Back", new Vector3( 0,   2f,    17f),   new Vector3(9, 5, 1),      Color.white * 0.9f, fieldRoot);
        Cube("EndZ-L",     new Vector3(-7f,  1.5f, -15.5f), new Vector3(7, 3, 1),      Color.gray * 1.4f, fieldRoot);
        Cube("EndZ-R",     new Vector3( 7f,  1.5f, -15.5f), new Vector3(7, 3, 1),      Color.gray * 1.4f, fieldRoot);
        Cube("GoalZ-Back", new Vector3( 0,   2f,   -17f),   new Vector3(9, 5, 1),      Color.white * 0.9f, fieldRoot);
        var cl = Cube("CenterLine", new Vector3(0, 0.01f, 0), new Vector3(20, 0.02f, 0.15f), Color.white * 0.7f, fieldRoot);
        cl.GetComponent<Collider>().enabled = false;

        // ── GOAL TRIGGERS ────────────────────────────────────────
        var goalsRoot = Child("Goals", root);
        var gt1 = MakeGoalTrigger("GoalTrigger_P1", new Vector3(0, 2f,  15.5f), new Vector3(7, 4, 2), goalsRoot);
        gt1.scoringPlayerId = 1;
        var gt2 = MakeGoalTrigger("GoalTrigger_P2", new Vector3(0, 2f, -15.5f), new Vector3(7, 4, 2), goalsRoot);
        gt2.scoringPlayerId = 2;

        // ── GOAL EFFECTS ─────────────────────────────────────────
        var fxRoot = Child("GoalEffects", root);
        var fxP1 = MakeGoalEffect("GoalEffect_P1", new Vector3(0, 3f,  14f), new Color(0.3f, 0.5f, 1f),   fxRoot);
        var fxP2 = MakeGoalEffect("GoalEffect_P2", new Vector3(0, 3f, -14f), new Color(1f, 0.3f, 0.3f),   fxRoot);

        // ── SPAWN POINTS ─────────────────────────────────────────
        var spawnRoot = Child("SpawnPoints", root);
        var spawnP1   = Empty("SpawnP1",   new Vector3(0, 1, -8), Quaternion.identity,       spawnRoot);
        var spawnP2   = Empty("SpawnP2",   new Vector3(0, 1,  8), Quaternion.Euler(0,180,0), spawnRoot);
        var ballSpawn = Empty("BallSpawn", new Vector3(0, 0.5f, 0), Quaternion.identity,     spawnRoot);

        // ── BALL ─────────────────────────────────────────────────
        var ballGO = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        ballGO.name = "Ball"; ballGO.tag = "Ball";
        ballGO.transform.SetParent(root.transform);
        ballGO.transform.position = new Vector3(0, 0.5f, 0);
        Undo.RegisterCreatedObjectUndo(ballGO, "Create Ball");
        SetColor(ballGO, Color.white);
        var ballRb = ballGO.AddComponent<Rigidbody>();
        ballRb.mass = 0.4f; ballRb.linearDamping = 0.2f; ballRb.angularDamping = 0.1f;
        var pmPath = "Assets/BallBounce.physicsmaterial";
        var pm = AssetDatabase.LoadAssetAtPath<PhysicsMaterial>(pmPath);
        if (pm == null)
        {
            pm = new PhysicsMaterial("BallBounce") { bounciness = 0.75f, bounceCombine = PhysicsMaterialCombine.Maximum, frictionCombine = PhysicsMaterialCombine.Minimum };
            AssetDatabase.CreateAsset(pm, pmPath);
        }
        ballGO.GetComponent<SphereCollider>().material = pm;
        var ballSync = ballGO.AddComponent<BallSync>();

        // ── PLAYERS ──────────────────────────────────────────────
        var (p1, p1Label) = MakePlayer("Player1", new Vector3(0, 1, -8), Quaternion.identity,       new Color(0.2f, 0.4f, 0.9f), "Player1", root);
        var (p2, p2Label) = MakePlayer("Player2", new Vector3(0, 1,  8), Quaternion.Euler(0,180,0), new Color(0.9f, 0.2f, 0.2f), "Player2", root);

        // ── CAMERA ───────────────────────────────────────────────
        var cam       = Camera.main.gameObject;
        var camFollow = cam.GetComponent<CameraFollow>() ?? cam.AddComponent<CameraFollow>();
        cam.transform.SetPositionAndRotation(new Vector3(0, 10, -20), Quaternion.Euler(25, 0, 0));

        // ── NETWORK MANAGER ───────────────────────────────────────
        var netGO  = Child("GameNetworkManager", root);
        var netMgr = netGO.AddComponent<GameNetworkManager>();
        netMgr.serverUrl = wsUrl;

        // ── AUDIO MANAGER ─────────────────────────────────────────
        var audioGO = Child("AudioManager", root);
        audioGO.AddComponent<AudioSource>(); // BGM slot
        audioGO.AddComponent<AudioSource>(); // SFX slot
        audioGO.AddComponent<AudioManager>();

        // ── GAME MANAGER ─────────────────────────────────────────
        var gmGO = Child("GameManager", root);
        var gm   = gmGO.AddComponent<GameManager>();
        gm.localPlayer         = p1.GetComponent<PlayerController>();
        gm.remotePlayer        = p2.GetComponent<PlayerController>();
        gm.ball                = ballSync;
        gm.cameraFollow        = camFollow;
        gm.spawnP1             = spawnP1.transform;
        gm.spawnP2             = spawnP2.transform;
        gm.ballSpawn           = ballSpawn.transform;
        gm.goalEffectP1        = fxP1;
        gm.goalEffectP2        = fxP2;
        gm.localNicknameLabel  = p1Label;
        gm.remoteNicknameLabel = p2Label;

        // ── EVENT SYSTEM ─────────────────────────────────────────
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = Child("EventSystem", root);
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
        }

        // ── CANVAS ───────────────────────────────────────────────
        var canvasGO = Child("Canvas", root);
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create Canvas");
        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── HUD ──────────────────────────────────────────────────
        var (scoreText, statusText, nicknamePanel, nicknameInput) = BuildHUD(canvasGO, gm);
        gm.scoreText     = scoreText;
        gm.statusText    = statusText;
        gm.nicknamePanel = nicknamePanel;
        gm.nicknameInput = nicknameInput;

        // ── TOUCH JOYSTICK ────────────────────────────────────────
        BuildTouchJoystick(canvasGO);

        // ── WIN OVERLAY ───────────────────────────────────────────
        BuildWinOverlay(canvasGO, gm);

        // ── SCREEN FLASH ──────────────────────────────────────────
        var flashGO  = new GameObject("ScreenFlash");
        flashGO.transform.SetParent(canvasGO.transform, false);
        var flashImg = flashGO.AddComponent<Image>();
        flashImg.color = Color.clear;
        flashImg.raycastTarget = false;
        var flashRt   = (RectTransform)flashGO.transform;
        flashRt.anchorMin = Vector2.zero; flashRt.anchorMax = Vector2.one;
        flashRt.offsetMin = Vector2.zero; flashRt.offsetMax = Vector2.zero;
        gm.screenFlash = flashImg;

        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Soccer scene built! Press Play to test.");
        Selection.activeGameObject = gmGO;
    }

    // ================================================================
    //  HUD (Score, Status, Nickname Panel)
    // ================================================================

    (TextMeshProUGUI score, TextMeshProUGUI status, GameObject panel, TMP_InputField input)
        BuildHUD(GameObject canvasGO, GameManager gm)
    {
        var scoreTxt = TMPText(canvasGO, "ScoreText", "0  -  0", 72, Color.white,
            new Vector2(0.5f,1f), new Vector2(0.5f,1f), new Vector2(0,-70), new Vector2(500,110));
        ApplyGameStyle(scoreTxt);

        var statusTxt = TMPText(canvasGO, "StatusText", "Connecting...", 40, new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(700,80));
        ApplyGameStyle(statusTxt);

        // ── NICKNAME PANEL ────────────────────────────────────────
        var panelGO = new GameObject("NicknamePanel");
        panelGO.transform.SetParent(canvasGO.transform, false);
        var panelImg = panelGO.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.05f, 0.12f, 0.92f);
        var panelRt    = (RectTransform)panelGO.transform;
        panelRt.anchorMin = Vector2.zero; panelRt.anchorMax = Vector2.one;
        panelRt.offsetMin = Vector2.zero; panelRt.offsetMax = Vector2.zero;

        var titleTxt = TMPText(panelGO, "Title", "ENTER YOUR NICKNAME", 40, Color.white,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0,100), new Vector2(700,70));
        ApplyGameStyle(titleTxt);

        // Input field
        var inputGO  = new GameObject("NicknameInput");
        inputGO.transform.SetParent(panelGO.transform, false);
        var inputImg = inputGO.AddComponent<Image>();
        inputImg.color = new Color(1,1,1,0.12f);
        var inputRt  = (RectTransform)inputGO.transform;
        inputRt.anchorMin = new Vector2(0.5f,0.5f); inputRt.anchorMax = new Vector2(0.5f,0.5f);
        inputRt.anchoredPosition = Vector2.zero; inputRt.sizeDelta = new Vector2(420, 62);

        var inputTextGO  = new GameObject("Text");
        inputTextGO.transform.SetParent(inputGO.transform, false);
        var inputTMP     = inputTextGO.AddComponent<TextMeshProUGUI>();
        inputTMP.fontSize = 32; inputTMP.color = Color.white;
        inputTMP.fontStyle = FontStyles.Bold;
        inputTMP.alignment = TextAlignmentOptions.Center;
        var inputTextRt  = (RectTransform)inputTextGO.transform;
        inputTextRt.anchorMin = Vector2.zero; inputTextRt.anchorMax = Vector2.one;
        inputTextRt.offsetMin = new Vector2(10,0); inputTextRt.offsetMax = new Vector2(-10,0);

        var phGO  = new GameObject("Placeholder");
        phGO.transform.SetParent(inputGO.transform, false);
        var phTMP = phGO.AddComponent<TextMeshProUGUI>();
        phTMP.text = "YOUR NAME..."; phTMP.fontSize = 28;
        phTMP.color = new Color(1,1,1,0.35f);
        phTMP.fontStyle = FontStyles.Italic | FontStyles.Bold;
        phTMP.alignment = TextAlignmentOptions.Center;
        var phRt  = (RectTransform)phGO.transform;
        phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
        phRt.offsetMin = new Vector2(10,0); phRt.offsetMax = new Vector2(-10,0);

        var inputField = inputGO.AddComponent<TMP_InputField>();
        inputField.textComponent = inputTMP; inputField.placeholder = phTMP;
        inputField.characterLimit = 12; inputField.contentType = TMP_InputField.ContentType.Standard;

        // START button
        var btnGO  = new GameObject("StartButton");
        btnGO.transform.SetParent(panelGO.transform, false);
        var btnImg = btnGO.AddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.6f, 0.15f, 1f);
        var btnRt  = (RectTransform)btnGO.transform;
        btnRt.anchorMin = new Vector2(0.5f,0.5f); btnRt.anchorMax = new Vector2(0.5f,0.5f);
        btnRt.anchoredPosition = new Vector2(0, -90); btnRt.sizeDelta = new Vector2(220, 60);

        var btnTxtGO = new GameObject("Text");
        btnTxtGO.transform.SetParent(btnGO.transform, false);
        var btnTmp   = btnTxtGO.AddComponent<TextMeshProUGUI>();
        btnTmp.text = "START"; btnTmp.fontSize = 32; btnTmp.color = Color.white;
        btnTmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        btnTmp.alignment = TextAlignmentOptions.Center;
        btnTmp.outlineWidth = 0.2f; btnTmp.outlineColor = new Color32(0, 80, 0, 255);
        var btnTxtRt = (RectTransform)btnTxtGO.transform;
        btnTxtRt.anchorMin = Vector2.zero; btnTxtRt.anchorMax = Vector2.one;
        btnTxtRt.offsetMin = Vector2.zero; btnTxtRt.offsetMax = Vector2.zero;

        var btn = btnGO.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(btn.onClick, gm.SubmitNickname);

        return (scoreTxt, statusTxt, panelGO, inputField);
    }

    // ================================================================
    //  Touch Joystick
    // ================================================================

    void BuildTouchJoystick(GameObject canvasGO)
    {
        var joystickGO = new GameObject("TouchJoystick");
        joystickGO.transform.SetParent(canvasGO.transform, false);

        // Background circle
        var bgImg   = joystickGO.AddComponent<Image>();
        bgImg.color = new Color(1,1,1,0.15f);
        var bgRt    = (RectTransform)joystickGO.transform;
        bgRt.anchorMin = new Vector2(0f, 0f);
        bgRt.anchorMax = new Vector2(0f, 0f);
        bgRt.pivot     = new Vector2(0.5f, 0.5f);
        bgRt.anchoredPosition = new Vector2(130f, 130f);
        bgRt.sizeDelta        = new Vector2(200f, 200f);

        // Knob
        var knobGO  = new GameObject("Knob");
        knobGO.transform.SetParent(joystickGO.transform, false);
        var knobImg = knobGO.AddComponent<Image>();
        knobImg.color = new Color(1,1,1,0.4f);
        var knobRt  = (RectTransform)knobGO.transform;
        knobRt.anchorMin = new Vector2(0.5f,0.5f); knobRt.anchorMax = new Vector2(0.5f,0.5f);
        knobRt.anchoredPosition = Vector2.zero; knobRt.sizeDelta = new Vector2(90f, 90f);

        var touch      = joystickGO.AddComponent<TouchInput>();
        touch.knob     = knobRt;
        touch.maxRadius = 80f;
    }

    // ================================================================
    //  Win Overlay
    // ================================================================

    void BuildWinOverlay(GameObject canvasGO, GameManager gm)
    {
        var overlayGO = new GameObject("WinOverlay");
        overlayGO.transform.SetParent(canvasGO.transform, false);
        var overlayImg = overlayGO.AddComponent<Image>();
        overlayImg.color = new Color(0f, 0f, 0f, 0.88f);
        var overlayRt  = (RectTransform)overlayGO.transform;
        overlayRt.anchorMin = Vector2.zero; overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero; overlayRt.offsetMax = Vector2.zero;

        // Win text
        var winTxt = TMPText(overlayGO, "WinText", "★ PLAYER 1 WINS! ★", 60, new Color(1f, 0.85f, 0.1f),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0, 80), new Vector2(800, 100));
        ApplyGameStyle(winTxt);
        winTxt.outlineColor = new Color32(120, 70, 0, 255);

        // Score text
        var scoreTxt = TMPText(overlayGO, "WinScoreText", "3  -  0", 52, Color.white,
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0, 0), new Vector2(400, 80));
        ApplyGameStyle(scoreTxt);

        // Rematch button (host only)
        var rematchGO  = new GameObject("RematchButton");
        rematchGO.transform.SetParent(overlayGO.transform, false);
        var rematchImg = rematchGO.AddComponent<Image>();
        rematchImg.color = new Color(0.1f, 0.6f, 0.1f, 1f);
        var rematchRt  = (RectTransform)rematchGO.transform;
        rematchRt.anchorMin = new Vector2(0.5f,0.5f); rematchRt.anchorMax = new Vector2(0.5f,0.5f);
        rematchRt.anchoredPosition = new Vector2(0, -90); rematchRt.sizeDelta = new Vector2(280, 70);

        var rematchTxtGO = new GameObject("Text");
        rematchTxtGO.transform.SetParent(rematchGO.transform, false);
        var rematchTmp   = rematchTxtGO.AddComponent<TextMeshProUGUI>();
        rematchTmp.text = "REMATCH"; rematchTmp.fontSize = 36; rematchTmp.color = Color.white;
        rematchTmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        rematchTmp.alignment = TextAlignmentOptions.Center;
        rematchTmp.outlineWidth = 0.2f; rematchTmp.outlineColor = new Color32(0, 80, 0, 255);
        var rematchTxtRt = (RectTransform)rematchTxtGO.transform;
        rematchTxtRt.anchorMin = Vector2.zero; rematchTxtRt.anchorMax = Vector2.one;
        rematchTxtRt.offsetMin = Vector2.zero; rematchTxtRt.offsetMax = Vector2.zero;

        var rematchBtn = rematchGO.AddComponent<Button>();
        UnityEventTools.AddPersistentListener(rematchBtn.onClick, gm.DoRematch);

        // Wait text (guest only)
        var waitTxt = TMPText(overlayGO, "WaitText", "Waiting for rematch...", 30, new Color(0.8f,0.8f,0.8f),
            new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0, -90), new Vector2(500, 50));

        overlayGO.SetActive(false);

        gm.winOverlay    = overlayGO;
        gm.winText       = winTxt;
        gm.winScoreText  = scoreTxt;
        gm.rematchButton = rematchGO;
        gm.waitText      = waitTxt;
    }

    // ================================================================
    //  Player
    // ================================================================

    (GameObject go, TMP_Text label) MakePlayer(string name, Vector3 pos, Quaternion rot, Color color, string defaultName, GameObject parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.SetPositionAndRotation(pos, rot);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        SetColor(go, color);

        var rb = go.AddComponent<Rigidbody>();
        rb.mass = 1f; rb.linearDamping = 2f; rb.angularDamping = 1f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        go.AddComponent<PlayerController>();

        var labelGO = new GameObject("NicknameLabel");
        labelGO.transform.SetParent(go.transform);
        labelGO.transform.localPosition = new Vector3(0, 2.8f, 0);
        labelGO.transform.localScale    = Vector3.one;
        var tmp       = labelGO.AddComponent<TextMeshPro>();
        tmp.text      = defaultName;
        tmp.fontSize  = 5f;
        tmp.color     = Color.white;
        tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = Color.black;
        labelGO.AddComponent<FaceCamera>();

        return (go, tmp);
    }

    // ================================================================
    //  Goal Effect
    // ================================================================

    GoalEffect MakeGoalEffect(string name, Vector3 pos, Color color, GameObject parent)
    {
        var go = Empty(name, pos, Quaternion.identity, parent);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);

        var ps   = go.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake     = false;
        main.loop            = false;
        main.maxParticles    = 150;
        main.startLifetime   = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSpeed      = new ParticleSystem.MinMaxCurve(4f, 10f);
        main.startSize       = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startColor      = new ParticleSystem.MinMaxGradient(color, Color.white);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = 0.3f;

        var emission = ps.emission;
        emission.rateOverTime = 0;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 120) });

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius    = 4f;

        var psRenderer = go.GetComponent<ParticleSystemRenderer>();
        var psMat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")
            ?? Shader.Find("Sprites/Default"));
        psMat.color = Color.white;
        psRenderer.material = psMat;

        var lightGO = new GameObject("FlashLight");
        lightGO.transform.SetParent(go.transform);
        lightGO.transform.localPosition = Vector3.zero;
        var lt       = lightGO.AddComponent<Light>();
        lt.type      = LightType.Point;
        lt.color     = color;
        lt.range     = 25f;
        lt.intensity = 0f;

        var effect       = go.AddComponent<GoalEffect>();
        effect.particles  = ps;
        effect.flashLight = lt;
        return effect;
    }

    // ================================================================
    //  Low-level helpers
    // ================================================================

    GoalTrigger MakeGoalTrigger(string name, Vector3 pos, Vector3 size, GameObject parent)
    {
        var go  = Empty(name, pos, Quaternion.identity, parent);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        var col = go.AddComponent<BoxCollider>();
        col.isTrigger = true; col.size = size;
        return go.AddComponent<GoalTrigger>();
    }

    GameObject Child(string name, GameObject parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform);
        return go;
    }

    GameObject Empty(string name, Vector3 pos, Quaternion rot, GameObject parent)
    {
        var go = Child(name, parent);
        go.transform.SetPositionAndRotation(pos, rot);
        return go;
    }

    GameObject Cube(string name, Vector3 pos, Vector3 scale, Color color, GameObject parent)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent.transform);
        go.transform.position   = pos;
        go.transform.localScale = scale;
        SetColor(go, color);
        Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        return go;
    }

    void SetColor(GameObject go, Color color)
    {
        var r = go.GetComponent<Renderer>();
        if (r == null) return;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard"));
        mat.color = color;
        r.material = mat;
    }

    TextMeshProUGUI TMPText(GameObject parent, string name, string text, float fontSize, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 sizeDelta)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
        rt.anchoredPosition = anchoredPos; rt.sizeDelta = sizeDelta;
        return tmp;
    }

    void ApplyGameStyle(TextMeshProUGUI tmp)
    {
        tmp.fontStyle    = FontStyles.Bold | FontStyles.UpperCase;
        tmp.outlineWidth = 0.25f;
        tmp.outlineColor = new Color32(0, 0, 0, 255);
    }

    void EnsureTag(string tag)
    {
        var tagMgr = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var tags   = tagMgr.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
            if (tags.GetArrayElementAtIndex(i).stringValue == tag) return;
        tags.InsertArrayElementAtIndex(tags.arraySize);
        tags.GetArrayElementAtIndex(tags.arraySize - 1).stringValue = tag;
        tagMgr.ApplyModifiedProperties();
    }
}
