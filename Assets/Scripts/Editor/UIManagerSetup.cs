#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

namespace Warcaby.Editor
{
    /// <summary>
    /// Builds the complete UIManager Canvas hierarchy in the active scene
    /// and wires up every serialized field automatically.
    ///
    /// Run via: Tools → Warcaby → Setup UI Manager
    /// </summary>
    public static class UIManagerSetup
    {
        // ─── Colours used in the generated UI ────────────────────────────
        private static readonly Color ColorPanelBg       = new Color(0.12f, 0.10f, 0.08f, 0.95f);
        private static readonly Color ColorButtonNormal  = new Color(0.56f, 0.35f, 0.18f, 1.00f);
        private static readonly Color ColorButtonHover   = new Color(0.70f, 0.48f, 0.28f, 1.00f);
        private static readonly Color ColorButtonDanger  = new Color(0.65f, 0.15f, 0.15f, 1.00f);
        private static readonly Color ColorButtonSuccess = new Color(0.18f, 0.50f, 0.22f, 1.00f);
        private static readonly Color ColorText          = new Color(0.93f, 0.87f, 0.75f, 1.00f);
        private static readonly Color ColorGold          = new Color(1.00f, 0.85f, 0.20f, 1.00f);
        private static readonly Color ColorToastBg       = new Color(0.10f, 0.10f, 0.10f, 0.85f);

        // ─────────────────────────────────────────────────────────────────

        [MenuItem("Tools/Warcaby/Setup UI Manager")]
        public static void SetupUIManager()
        {
            // ── Find or create UIManager GO ───────────────────────────────
            var uiManagerGo = GameObject.Find("UIManager");
            if (uiManagerGo == null) uiManagerGo = new GameObject("UIManager");
            Undo.RegisterCreatedObjectUndo(uiManagerGo, "Setup UI Manager");

            var uiManager = uiManagerGo.GetComponent<UI.UIManager>();
            if (uiManager == null) uiManager = uiManagerGo.AddComponent<UI.UIManager>();

            // ── Root Canvas ───────────────────────────────────────────────
            var canvasGo = GetOrCreate("Canvas_UI", uiManagerGo.transform);

            // Ensure RectTransform exists first (Canvas requires it)
            if (canvasGo.GetComponent<RectTransform>() == null)
                canvasGo.AddComponent<RectTransform>();

            var canvas = canvasGo.GetComponent<Canvas>();
            if (canvas == null)
                canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null)
                scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            if (canvasGo.GetComponent<GraphicRaycaster>() == null)
                canvasGo.AddComponent<GraphicRaycaster>();

            // ── EventSystem (required for UI interaction) ────────────────
            if (GameObject.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                Undo.RegisterCreatedObjectUndo(esGo, "Create EventSystem");
            }

            // ── Build panels ──────────────────────────────────────────────
            var mainMenuPanel  = BuildMainMenuPanel(canvasGo.transform);
            var gamePanel      = BuildGamePanel(canvasGo.transform);
            var gameOverPanel  = BuildGameOverPanel(canvasGo.transform);
            var onlinePanel    = BuildOnlinePanel(canvasGo.transform);
            var toastLabel     = BuildToast(canvasGo.transform);

            // ── Wire all serialized fields ────────────────────────────────
            var so = new SerializedObject(uiManager);

            // Panels
            so.FindProperty("_mainMenuPanel").objectReferenceValue = mainMenuPanel.root;
            so.FindProperty("_gamePanel").objectReferenceValue     = gamePanel.root;
            so.FindProperty("_gameOverPanel").objectReferenceValue = gameOverPanel.root;
            so.FindProperty("_onlinePanel").objectReferenceValue   = onlinePanel.root;

            // Main Menu buttons & dropdowns
            so.FindProperty("_btnPvP").objectReferenceValue              = mainMenuPanel.btnPvP;
            so.FindProperty("_btnVsAI").objectReferenceValue             = mainMenuPanel.btnVsAI;
            so.FindProperty("_btnOnline").objectReferenceValue           = mainMenuPanel.btnOnline;
            so.FindProperty("_aiColorDropdown").objectReferenceValue     = mainMenuPanel.aiColorDropdown;
            so.FindProperty("_aiDifficultyDropdown").objectReferenceValue= mainMenuPanel.aiDifficultyDropdown;

            // In-Game HUD
            so.FindProperty("_turnLabel").objectReferenceValue           = gamePanel.turnLabel;
            so.FindProperty("_whitePiecesLabel").objectReferenceValue    = gamePanel.whitePiecesLabel;
            so.FindProperty("_blackPiecesLabel").objectReferenceValue    = gamePanel.blackPiecesLabel;
            so.FindProperty("_btnResign").objectReferenceValue           = gamePanel.btnResign;

            // Game Over
            so.FindProperty("_gameOverLabel").objectReferenceValue       = gameOverPanel.gameOverLabel;
            so.FindProperty("_btnPlayAgain").objectReferenceValue        = gameOverPanel.btnPlayAgain;
            so.FindProperty("_btnMainMenu").objectReferenceValue         = gameOverPanel.btnMainMenu;

            // Online
            so.FindProperty("_serverAddressInput").objectReferenceValue  = onlinePanel.serverAddressInput;
            so.FindProperty("_btnHost").objectReferenceValue             = onlinePanel.btnHost;
            so.FindProperty("_btnJoin").objectReferenceValue             = onlinePanel.btnJoin;

            // Toast
            so.FindProperty("_toastLabel").objectReferenceValue          = toastLabel;

            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(uiManager);
            var activeScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(activeScene);
            // Save immediately – without SaveScene the dropdown template reference
            // is lost when entering Play mode (MarkSceneDirty alone is not enough).
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(activeScene);

            Debug.Log("[UIManagerSetup] Canvas hierarchy built, all references wired, scene saved.");
            EditorUtility.DisplayDialog("Warcaby",
                "UI Manager zostal skonfigurowany.\nWszystkie referencje zostaly przypisane.", "OK");
        }

        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Finds every UI element in the existing Canvas hierarchy by name
        /// and assigns all serialized fields on UIManager without rebuilding anything.
        /// </summary>
        [MenuItem("Tools/Warcaby/Assign UI References")]
        public static void AssignUIReferences()
        {
            var uiManager = Object.FindObjectOfType<UI.UIManager>();
            if (uiManager == null)
            {
                EditorUtility.DisplayDialog("Warcaby",
                    "Nie znaleziono UIManager w scenie.\nUruchom najpierw 'Setup UI Manager'.", "OK");
                return;
            }

            // Root canvas path: UIManager/Canvas_UI
            var canvasT = uiManager.transform.Find("Canvas_UI");
            if (canvasT == null)
            {
                EditorUtility.DisplayDialog("Warcaby",
                    "Nie znaleziono Canvas_UI.\nUruchom najpierw 'Setup UI Manager'.", "OK");
                return;
            }

            var so = new SerializedObject(uiManager);
            int assigned = 0;

            // ── Panels ────────────────────────────────────────────────────
            assigned += Assign(so, "_mainMenuPanel",  canvasT, "Panel_MainMenu");
            assigned += Assign(so, "_gamePanel",      canvasT, "Panel_Game");
            assigned += Assign(so, "_gameOverPanel",  canvasT, "Panel_GameOver");
            assigned += Assign(so, "_onlinePanel",    canvasT, "Panel_Online");

            // ── Main Menu ─────────────────────────────────────────────────
            var mm = canvasT.Find("Panel_MainMenu");
            if (mm != null)
            {
                assigned += AssignComp<Button>          (so, "_btnPvP",               mm, "Btn_PvP");
                assigned += AssignComp<Button>          (so, "_btnVsAI",              mm, "Btn_VsAI");
                assigned += AssignComp<Button>          (so, "_btnOnline",            mm, "Btn_Online");
                assigned += AssignComp<TMP_Dropdown>    (so, "_aiColorDropdown",      mm, "AIOptions/Dropdown_AIColor");
                assigned += AssignComp<TMP_Dropdown>    (so, "_aiDifficultyDropdown", mm, "AIOptions/Dropdown_AIDiff");
            }

            // ── In-Game HUD ───────────────────────────────────────────────
            var gp = canvasT.Find("Panel_Game");
            if (gp != null)
            {
                assigned += AssignComp<TextMeshProUGUI> (so, "_turnLabel",            gp, "TopBar/Label_Turn");
                assigned += AssignComp<TextMeshProUGUI> (so, "_whitePiecesLabel",     gp, "TopBar/Label_White");
                assigned += AssignComp<TextMeshProUGUI> (so, "_blackPiecesLabel",     gp, "TopBar/Label_Black");
                assigned += AssignComp<Button>          (so, "_btnResign",            gp, "BottomBar/Btn_Resign");
            }

            // ── Game Over ─────────────────────────────────────────────────
            var go2 = canvasT.Find("Panel_GameOver");
            if (go2 != null)
            {
                assigned += AssignComp<TextMeshProUGUI> (so, "_gameOverLabel",        go2, "Label_GameOver");
                assigned += AssignComp<Button>          (so, "_btnPlayAgain",         go2, "Btn_PlayAgain");
                assigned += AssignComp<Button>          (so, "_btnMainMenu",          go2, "Btn_MainMenu");
            }

            // ── Online ────────────────────────────────────────────────────
            var op = canvasT.Find("Panel_Online");
            if (op != null)
            {
                assigned += AssignComp<TMP_InputField>  (so, "_serverAddressInput",   op, "Input_ServerAddress");
                assigned += AssignComp<Button>          (so, "_btnHost",              op, "Btn_Host");
                assigned += AssignComp<Button>          (so, "_btnJoin",              op, "Btn_Join");
            }

            // ── Toast ─────────────────────────────────────────────────────
            // TMP lives on the child "ToastText"; parent "Toast" is the background Image GO
            assigned += AssignComp<TextMeshProUGUI>     (so, "_toastLabel",       canvasT, "Toast/ToastText");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(uiManager);
            var assignScene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(assignScene);
            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(assignScene);

            string msg = $"Przypisano {assigned} referencji do UIManager. Scena zapisana.";
            Debug.Log($"[UIManagerSetup] {msg}");
            EditorUtility.DisplayDialog("Warcaby", msg, "OK");
        }

        // ── Assign helpers ────────────────────────────────────────────────

        /// <summary>Assigns a GameObject reference by searching child path.</summary>
        private static int Assign(SerializedObject so, string fieldName,
            Transform root, string childPath)
        {
            var t = root.Find(childPath);
            if (t == null) { Debug.LogWarning($"[UIManagerSetup] Not found: {childPath}"); return 0; }
            var prop = so.FindProperty(fieldName);
            if (prop == null) { Debug.LogWarning($"[UIManagerSetup] Property not found: {fieldName}"); return 0; }
            prop.objectReferenceValue = t.gameObject;
            return 1;
        }

        /// <summary>Assigns a Component reference by searching child path.</summary>
        private static int AssignComp<T>(SerializedObject so, string fieldName,
            Transform root, string childPath) where T : Component
        {
            var t = root.Find(childPath);
            if (t == null) { Debug.LogWarning($"[UIManagerSetup] Not found: {childPath}"); return 0; }
            var comp = t.GetComponent<T>();
            if (comp == null) { Debug.LogWarning($"[UIManagerSetup] Component {typeof(T).Name} missing on: {childPath}"); return 0; }
            var prop = so.FindProperty(fieldName);
            if (prop == null) { Debug.LogWarning($"[UIManagerSetup] Property not found: {fieldName}"); return 0; }
            prop.objectReferenceValue = comp;
            return 1;
        }

        // ═════════════════════════════════════════════════════════════════
        // Panel builders
        // ═════════════════════════════════════════════════════════════════

        // ── Main Menu ─────────────────────────────────────────────────────

        private struct MainMenuRefs
        {
            public GameObject root;
            public Button btnPvP, btnVsAI, btnOnline;
            public TMP_Dropdown aiColorDropdown, aiDifficultyDropdown;
        }

        private static MainMenuRefs BuildMainMenuPanel(Transform parent)
        {
            var refs = new MainMenuRefs();
            refs.root = BuildFullscreenPanel(parent, "Panel_MainMenu", ColorPanelBg);
            var t = refs.root.transform;

            // Title
            var title = MakeText(t, "Title_Label", "WARCABY", 72, ColorGold);
            StretchHorizontal(title, 0, 0, 300, 80, 300);

            // PvP button
            refs.btnPvP   = MakeButton(t, "Btn_PvP",    "Gracz vs Gracz",  ColorButtonNormal,  0, 80,  280, 60);
            refs.btnVsAI  = MakeButton(t, "Btn_VsAI",   "Gracz vs AI",     ColorButtonNormal,  0, 10,  280, 60);
            refs.btnOnline= MakeButton(t, "Btn_Online",  "Online",          ColorButtonNormal,  0,-60,  280, 60);

            // AI options section
            var aiSection = GetOrCreate("AIOptions", t);
            CenterRect(aiSection.GetOrAddComponent<RectTransform>(), 0, -140, 300, 130);
            SetImage(aiSection, new Color(0, 0, 0, 0.3f));

            var aiLabel = MakeText(aiSection.transform, "AILabel", "Ustawienia AI", 18, ColorText);
            StretchHorizontal(aiLabel, 0, 0, 134, 30, 10);

            refs.aiColorDropdown     = MakeDropdown(aiSection.transform, "Dropdown_AIColor",
                "Kolor gracza", new[] { "Białe", "Czarne" }, 0, 82, 260, 35);
            refs.aiDifficultyDropdown= MakeDropdown(aiSection.transform, "Dropdown_AIDiff",
                "Trudność AI",  new[] { "Łatwy", "Normalny", "Trudny" }, 0, 40, 260, 35);

            return refs;
        }

        // ── Game Panel (HUD) ──────────────────────────────────────────────

        private struct GamePanelRefs
        {
            public GameObject root;
            public TextMeshProUGUI turnLabel, whitePiecesLabel, blackPiecesLabel;
            public Button btnResign;
        }

        private static GamePanelRefs BuildGamePanel(Transform parent)
        {
            var refs = new GamePanelRefs();
            refs.root = BuildFullscreenPanel(parent, "Panel_Game", new Color(0, 0, 0, 0));
            var t = refs.root.transform;

            // Top HUD bar
            var topBar = GetOrCreate("TopBar", t);
            var topRt  = topBar.GetOrAddComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0, 1); topRt.anchorMax = new Vector2(1, 1);
            topRt.pivot     = new Vector2(0.5f, 1);
            topRt.offsetMin = new Vector2(0, -60); topRt.offsetMax = Vector2.zero;
            SetImage(topBar, ColorPanelBg);

            refs.whitePiecesLabel = MakeText(topBar.transform, "Label_White",
                "Białe: 12", 22, ColorWhite());
            PlaceAnchoredLeft(refs.whitePiecesLabel.rectTransform, 20, 0, 200, 50);

            refs.turnLabel = MakeText(topBar.transform, "Label_Turn",
                "Ruch: Białe", 26, ColorGold);
            CenterRect(refs.turnLabel.rectTransform, 0, 0, 300, 50);

            refs.blackPiecesLabel = MakeText(topBar.transform, "Label_Black",
                "Czarne: 12", 22, ColorWhite());
            PlaceAnchoredRight(refs.blackPiecesLabel.rectTransform, 20, 0, 200, 50);

            // Bottom bar
            var botBar = GetOrCreate("BottomBar", t);
            var botRt  = botBar.GetOrAddComponent<RectTransform>();
            botRt.anchorMin = new Vector2(0, 0); botRt.anchorMax = new Vector2(1, 0);
            botRt.pivot     = new Vector2(0.5f, 0);
            botRt.offsetMin = Vector2.zero; botRt.offsetMax = new Vector2(0, 60);
            SetImage(botBar, ColorPanelBg);

            refs.btnResign = MakeButton(botBar.transform, "Btn_Resign",
                "Poddaj", ColorButtonDanger, 0, 0, 160, 44);

            return refs;
        }

        // ── Game Over ─────────────────────────────────────────────────────

        private struct GameOverRefs
        {
            public GameObject root;
            public TextMeshProUGUI gameOverLabel;
            public Button btnPlayAgain, btnMainMenu;
        }

        private static GameOverRefs BuildGameOverPanel(Transform parent)
        {
            var refs = new GameOverRefs();
            refs.root = BuildFullscreenPanel(parent, "Panel_GameOver", new Color(0, 0, 0, 0.75f));
            var t = refs.root.transform;

            refs.gameOverLabel = MakeText(t, "Label_GameOver", "Białe wygrywają!", 60, ColorGold);
            CenterRect(refs.gameOverLabel.rectTransform, 0, 60, 600, 80);

            refs.btnPlayAgain = MakeButton(t, "Btn_PlayAgain", "Zagraj jeszcze raz",
                ColorButtonSuccess, 0, -20, 280, 60);
            refs.btnMainMenu  = MakeButton(t, "Btn_MainMenu",  "Menu główne",
                ColorButtonNormal,  0, -90, 280, 60);

            return refs;
        }

        // ── Online Panel ──────────────────────────────────────────────────

        private struct OnlinePanelRefs
        {
            public GameObject root;
            public TMP_InputField serverAddressInput;
            public Button btnHost, btnJoin;
        }

        private static OnlinePanelRefs BuildOnlinePanel(Transform parent)
        {
            var refs = new OnlinePanelRefs();
            refs.root = BuildFullscreenPanel(parent, "Panel_Online", ColorPanelBg);
            var t = refs.root.transform;

            MakeText(t, "Title_Online", "MULTIPLAYER ONLINE", 48, ColorGold);

            refs.serverAddressInput = MakeInputField(t, "Input_ServerAddress",
                "Adres serwera (np. 192.168.1.10)", 0, 20, 400, 50);

            refs.btnHost = MakeButton(t, "Btn_Host", "Hostuj grę",
                ColorButtonSuccess, -150, -60, 180, 55);
            refs.btnJoin = MakeButton(t, "Btn_Join", "Dołącz",
                ColorButtonNormal,   150, -60, 180, 55);

            MakeButton(t, "Btn_Back", "← Wstecz", ColorButtonDanger, 0, -140, 180, 50)
                .onClick.AddListener(() =>
                    Object.FindObjectOfType<UI.UIManager>()?.ShowMainMenu());

            return refs;
        }

        // ── Toast ─────────────────────────────────────────────────────────

        private static TextMeshProUGUI BuildToast(Transform parent)
        {
            // Background GO (Image only – cannot share with TextMeshProUGUI)
            var go = GetOrCreate("Toast", parent);
            var rt = go.GetOrAddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0); rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot     = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, 80);
            rt.sizeDelta = new Vector2(500, 55);

            SetImage(go, ColorToastBg, cornerRadius: true);

            // Text child GO – Unity does not allow Image + TextMeshProUGUI on the same GO
            var textGo = GetOrCreate("ToastText", go.transform);
            StretchFull(textGo.GetOrAddComponent<RectTransform>());

            var tmp = textGo.GetOrAddComponent<TextMeshProUGUI>();
            tmp.text      = "";
            tmp.fontSize  = 22;
            tmp.color     = ColorText;
            tmp.alignment = TextAlignmentOptions.Center;

            go.SetActive(false);
            return tmp;
        }

        // ═════════════════════════════════════════════════════════════════
        // UI element factories
        // ═════════════════════════════════════════════════════════════════

        private static GameObject BuildFullscreenPanel(Transform parent, string name, Color bg)
        {
            var go = GetOrCreate(name, parent);
            var rt = go.GetOrAddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            SetImage(go, bg);
            return go;
        }

        private static Button MakeButton(Transform parent, string name, string label,
            Color bg, float x, float y, float w, float h)
        {
            var go  = GetOrCreate(name, parent);
            CenterRect(go.GetOrAddComponent<RectTransform>(), x, y, w, h);

            var img = go.GetOrAddComponent<Image>();
            img.color = bg;

            var btn = go.GetOrAddComponent<Button>();
            var cs  = btn.colors;
            cs.normalColor      = bg;
            cs.highlightedColor = Color.Lerp(bg, Color.white, 0.2f);
            cs.pressedColor     = Color.Lerp(bg, Color.black, 0.2f);
            btn.colors = cs;

            var textGo = GetOrCreate("Text", go.transform);
            StretchFull(textGo.GetOrAddComponent<RectTransform>());

            var tmp       = textGo.GetOrAddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 20;
            tmp.color     = ColorText;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        private static TextMeshProUGUI MakeText(Transform parent, string name,
            string text, float fontSize, Color color)
        {
            var go  = GetOrCreate(name, parent);
            var tmp = go.GetOrAddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static TMP_Dropdown MakeDropdown(Transform parent, string name,
            string placeholder, string[] options, float x, float y, float w, float h)
        {
            // ── Root button GO ────────────────────────────────────────────
            var go = GetOrCreate(name, parent);
            CenterRect(go.GetOrAddComponent<RectTransform>(), x, y, w, h);
            SetImage(go, new Color(0.2f, 0.2f, 0.2f, 1f));

            var drop = go.GetOrAddComponent<TMP_Dropdown>();
            drop.ClearOptions();
            var optList = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            foreach (var o in options) optList.Add(new TMP_Dropdown.OptionData(o));
            drop.AddOptions(optList);

            // ── Caption Label ─────────────────────────────────────────────
            var labelGo = GetOrCreate("Label", go.transform);
            StretchFull(labelGo.GetOrAddComponent<RectTransform>(), new RectOffset(10, 30, 0, 0));
            var lbl      = labelGo.GetOrAddComponent<TextMeshProUGUI>();
            lbl.text     = placeholder;
            lbl.fontSize = 16;
            lbl.color    = ColorText;
            drop.captionText = lbl;

            // ── Arrow ─────────────────────────────────────────────────────
            var arrowGo = GetOrCreate("Arrow", go.transform);
            var arrowRt = arrowGo.GetOrAddComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(1, 0.5f); arrowRt.anchorMax = new Vector2(1, 0.5f);
            arrowRt.pivot     = new Vector2(1, 0.5f);
            arrowRt.anchoredPosition = new Vector2(-5, 0);
            arrowRt.sizeDelta = new Vector2(20, 20);
            var arrowTmp = arrowGo.GetOrAddComponent<TextMeshProUGUI>();
            arrowTmp.text      = "▼";
            arrowTmp.fontSize  = 14;
            arrowTmp.color     = ColorText;
            arrowTmp.alignment = TextAlignmentOptions.Right;

            // ── Template (required by TMP_Dropdown to show the list) ──────
            var templateGo = GetOrCreate("Template", go.transform);
            var templateRt = templateGo.GetOrAddComponent<RectTransform>();
            templateRt.anchorMin = new Vector2(0, 0);
            templateRt.anchorMax = new Vector2(1, 0);
            templateRt.pivot     = new Vector2(0.5f, 1);
            templateRt.anchoredPosition = new Vector2(0, 2);
            templateRt.sizeDelta = new Vector2(0, 150);
            SetImage(templateGo, new Color(0.15f, 0.15f, 0.15f, 1f));

            var scrollRect = templateGo.GetOrAddComponent<UnityEngine.UI.ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.movementType = UnityEngine.UI.ScrollRect.MovementType.Clamped;

            // Viewport
            var viewportGo = GetOrCreate("Viewport", templateGo.transform);
            var viewportRt = viewportGo.GetOrAddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero; viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero; viewportRt.offsetMax = new Vector2(-18, 0);
            viewportGo.GetOrAddComponent<UnityEngine.UI.Mask>().showMaskGraphic = false;
            SetImage(viewportGo, Color.white);
            scrollRect.viewport = viewportRt;

            // Content
            var contentGo = GetOrCreate("Content", viewportGo.transform);
            var contentRt = contentGo.GetOrAddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1); contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0, 28);
            scrollRect.content = contentRt;

            // Item (Toggle – required by TMP_Dropdown)
            var itemGo = GetOrCreate("Item", contentGo.transform);
            var itemRt = itemGo.GetOrAddComponent<RectTransform>();
            itemRt.anchorMin = new Vector2(0, 0.5f); itemRt.anchorMax = new Vector2(1, 0.5f);
            itemRt.sizeDelta = new Vector2(0, 28);
            SetImage(itemGo, new Color(0.1f, 0.1f, 0.1f, 0));

            var itemToggle = itemGo.GetOrAddComponent<UnityEngine.UI.Toggle>();

            // Item Background
            var bgGo = GetOrCreate("Item Background", itemGo.transform);
            var bgRt = bgGo.GetOrAddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = Vector2.zero; bgRt.offsetMax = Vector2.zero;
            SetImage(bgGo, new Color(0.25f, 0.25f, 0.25f, 1f));
            itemToggle.targetGraphic = bgGo.GetComponent<UnityEngine.UI.Image>();

            // Item Checkmark
            var checkGo = GetOrCreate("Item Checkmark", itemGo.transform);
            var checkRt = checkGo.GetOrAddComponent<RectTransform>();
            checkRt.anchorMin = new Vector2(0, 0.5f); checkRt.anchorMax = new Vector2(0, 0.5f);
            checkRt.pivot     = new Vector2(0.5f, 0.5f);
            checkRt.anchoredPosition = new Vector2(10, 0);
            checkRt.sizeDelta = new Vector2(20, 20);
            SetImage(checkGo, ColorGold);
            itemToggle.graphic = checkGo.GetComponent<UnityEngine.UI.Image>();

            // Item Label
            var itemLabelGo = GetOrCreate("Item Label", itemGo.transform);
            StretchFull(itemLabelGo.GetOrAddComponent<RectTransform>(),
                new RectOffset(25, 5, 0, 0));
            var itemTmp      = itemLabelGo.GetOrAddComponent<TextMeshProUGUI>();
            itemTmp.text     = "Option";
            itemTmp.fontSize = 14;
            itemTmp.color    = ColorText;
            itemTmp.alignment = TextAlignmentOptions.Left;

            // Wire TMP_Dropdown template references using both reflection (sets the backing
            // field directly) and the public property setter (updates runtime state).
            // SetDirty + SaveScene in SetupUIManager will flush everything to disk.
            Undo.RecordObject(drop, "Wire dropdown template");
            SetDropdownField(drop, "m_Template",    templateRt);
            SetDropdownField(drop, "m_ItemText",    itemTmp);
            SetDropdownField(drop, "m_CaptionText", lbl);
            drop.template    = templateRt;
            drop.itemText    = itemTmp;
            drop.captionText = lbl;
            EditorUtility.SetDirty(drop);

            // Template must be inactive at runtime (TMP_Dropdown activates it on Show())
            templateGo.SetActive(false);

            return drop;
        }

        private static TMP_InputField MakeInputField(Transform parent, string name,
            string placeholder, float x, float y, float w, float h)
        {
            var go = GetOrCreate(name, parent);
            CenterRect(go.GetOrAddComponent<RectTransform>(), x, y, w, h);
            SetImage(go, new Color(0.18f, 0.18f, 0.18f, 1f));

            var field = go.GetOrAddComponent<TMP_InputField>();

            // Text area
            var textAreaGo = GetOrCreate("TextArea", go.transform);
            StretchFull(textAreaGo.GetOrAddComponent<RectTransform>(),
                new RectOffset(10, 10, 5, 5));
            textAreaGo.GetOrAddComponent<RectMask2D>();

            // Placeholder
            var phGo      = GetOrCreate("Placeholder", textAreaGo.transform);
            StretchFull(phGo.GetOrAddComponent<RectTransform>());
            var phTmp     = phGo.GetOrAddComponent<TextMeshProUGUI>();
            phTmp.text    = placeholder;
            phTmp.color   = new Color(0.6f, 0.6f, 0.6f);
            phTmp.fontSize = 18;
            field.placeholder = phTmp;

            // Input text
            var inputTextGo  = GetOrCreate("Text", textAreaGo.transform);
            StretchFull(inputTextGo.GetOrAddComponent<RectTransform>());
            var inputTmp     = inputTextGo.GetOrAddComponent<TextMeshProUGUI>();
            inputTmp.color   = ColorText;
            inputTmp.fontSize = 18;
            field.textComponent = inputTmp;

            field.text = "";
            return field;
        }

        // ═════════════════════════════════════════════════════════════════
        // RectTransform helpers
        // ═════════════════════════════════════════════════════════════════

        private static void CenterRect(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot     = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void StretchHorizontal(TextMeshProUGUI tmp,
            float x, float y, float w, float h, float topOffset)
        {
            var rt = tmp.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1); rt.anchorMax = new Vector2(0.5f, 1);
            rt.pivot     = new Vector2(0.5f, 1);
            rt.anchoredPosition = new Vector2(x, -topOffset);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void StretchFull(RectTransform rt, RectOffset padding = null)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.pivot     = new Vector2(0.5f, 0.5f);
            if (padding != null)
            {
                rt.offsetMin = new Vector2(padding.left, padding.bottom);
                rt.offsetMax = new Vector2(-padding.right, -padding.top);
            }
            else { rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; }
        }

        private static void PlaceAnchoredLeft(RectTransform rt,
            float offsetX, float offsetY, float w, float h)
        {
            rt.anchorMin = new Vector2(0, 0.5f); rt.anchorMax = new Vector2(0, 0.5f);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(offsetX, offsetY);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void PlaceAnchoredRight(RectTransform rt,
            float offsetX, float offsetY, float w, float h)
        {
            rt.anchorMin = new Vector2(1, 0.5f); rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot     = new Vector2(1, 0.5f);
            rt.anchoredPosition = new Vector2(-offsetX, offsetY);
            rt.sizeDelta = new Vector2(w, h);
        }

        // ═════════════════════════════════════════════════════════════════
        // Utility helpers
        // ═════════════════════════════════════════════════════════════════

        /// <summary>
        /// Sets a private serialized field on TMP_Dropdown via reflection, then marks
        /// the component dirty so Unity persists the change to the scene file.
        /// This is more reliable than SerializedObject.FindProperty() because the
        /// internal field name may differ across TMP package versions.
        /// </summary>
        private static void SetDropdownField(TMP_Dropdown drop, string fieldName, object value)
        {
            const System.Reflection.BindingFlags Flags =
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance;

            var type = typeof(TMP_Dropdown);
            while (type != null)
            {
                var fi = type.GetField(fieldName, Flags);
                if (fi != null) { fi.SetValue(drop, value); return; }
                type = type.BaseType;
            }
            Debug.LogWarning($"[UIManagerSetup] TMP_Dropdown field '{fieldName}' not found " +
                             $"– wiring via property only.");
        }

        private static GameObject GetOrCreate(string name, Transform parent)
        {
            var existing = parent.Find(name);
            if (existing != null) return existing.gameObject;
            // Create with RectTransform from the start – UI GameObjects require it
            // and AddComponent<RectTransform> fails when a plain Transform already exists.
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void SetImage(GameObject go, Color color, bool cornerRadius = false)
        {
            var img = go.GetOrAddComponent<Image>();
            img.color = color;
        }

        private static Color ColorWhite() => new Color(0.95f, 0.95f, 0.92f);
    }

    // ── Extension method so GetOrAddComponent stays tidy ─────────────────
    internal static class GameObjectExtensions
    {
        /// <summary>
        /// Returns existing component or adds a new one.
        /// Uses plain AddComponent – Undo.AddComponent returns null on freshly
        /// created GameObjects that have not yet been registered with Undo.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var existing = go.GetComponent<T>();
            if (existing != null) return existing;
            return go.AddComponent<T>();
        }
    }
}
#endif
