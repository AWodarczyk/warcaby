#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

namespace Warcaby.Editor
{
    /// <summary>
    /// Creates the MainMenu scene with a fully wired Canvas and MainMenuManager.
    /// Run via: Tools → Warcaby → Setup MainMenu Scene
    /// </summary>
    public static class MainMenuSceneSetup
    {
        private const string ScenePath = "Assets/Scenes/MainMenu.unity";

        private static readonly Color ColorBg            = new Color(0.08f, 0.07f, 0.06f, 1.00f);
        private static readonly Color ColorPanelBg       = new Color(0.12f, 0.10f, 0.08f, 0.97f);
        private static readonly Color ColorButtonNormal  = new Color(0.56f, 0.35f, 0.18f, 1.00f);
        private static readonly Color ColorButtonDanger  = new Color(0.65f, 0.15f, 0.15f, 1.00f);
        private static readonly Color ColorButtonSuccess = new Color(0.18f, 0.50f, 0.22f, 1.00f);
        private static readonly Color ColorButtonOnline  = new Color(0.15f, 0.35f, 0.60f, 1.00f);
        private static readonly Color ColorText          = new Color(0.93f, 0.87f, 0.75f, 1.00f);
        private static readonly Color ColorGold          = new Color(1.00f, 0.85f, 0.20f, 1.00f);
        private static readonly Color ColorSubtitle      = new Color(0.70f, 0.65f, 0.55f, 1.00f);

        // ─────────────────────────────────────────────────────────────────

        [MenuItem("Tools/Warcaby/Setup MainMenu Scene")]
        public static void SetupMainMenuScene()
        {
            // Create or open scene
            EnsureScenesFolderExists();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Background camera colour
            var cam = Object.FindObjectOfType<Camera>();
            if (cam != null) cam.backgroundColor = ColorBg;

            // EventSystem
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // ── Root MenuController GO ─────────────────────────────────
            var controllerGo = new GameObject("MainMenuController");
            var mmm = controllerGo.AddComponent<UI.MainMenuManager>();

            // ── Canvas ────────────────────────────────────────────────
            var canvasGo = new GameObject("Canvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();

            var canvasT = canvasGo.transform;

            // ── Background tint panel ─────────────────────────────────
            CreateImage("BG", canvasT, ColorBg, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero);

            // ── Logo / Title area ─────────────────────────────────────
            var titleGo = CreateText("Title", canvasT, "WARCABY", 96, ColorGold,
                new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(700, 110));

            var subtitleGo = CreateText("Subtitle", canvasT, "Polskie warcaby 8×8", 28, ColorSubtitle,
                new Vector2(0.5f, 1f), new Vector2(0, -200), new Vector2(500, 50));

            // ── Center card panel ─────────────────────────────────────
            var cardGo = new GameObject("Card");
            cardGo.transform.SetParent(canvasT, false);
            var cardRt = cardGo.AddComponent<RectTransform>();
            cardRt.anchorMin = cardRt.anchorMax = cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0, -30);
            cardRt.sizeDelta = new Vector2(360, 420);
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color = ColorPanelBg;
            var cardT = cardGo.transform;

            // ── Main buttons ──────────────────────────────────────────
            var btnPvP    = CreateButton("Btn_PvP",    cardT, "⚔  Gracz vs Gracz",  ColorButtonNormal,  0,  140, 300, 64);
            var btnVsAI   = CreateButton("Btn_VsAI",   cardT, "🤖  Gracz vs AI",     ColorButtonNormal,  0,  60,  300, 64);
            var btnOnline = CreateButton("Btn_Online",  cardT, "🌐  Online",           ColorButtonOnline,  0, -20,  300, 64);
            var btnQuit   = CreateButton("Btn_Quit",    cardT, "Wyjdź",               ColorButtonDanger,  0, -140, 200, 48);

            // ── AI Options sub-panel ──────────────────────────────────
            var aiPanel = new GameObject("Panel_AIOptions");
            aiPanel.transform.SetParent(canvasT, false);
            var aiPanelRt = aiPanel.AddComponent<RectTransform>();
            aiPanelRt.anchorMin = aiPanelRt.anchorMax = aiPanelRt.pivot = new Vector2(0.5f, 0.5f);
            aiPanelRt.anchoredPosition = new Vector2(0, -30);
            aiPanelRt.sizeDelta = new Vector2(380, 420);
            var aiPanelImg = aiPanel.AddComponent<Image>();
            aiPanelImg.color = ColorPanelBg;
            var aiT = aiPanel.transform;

            CreateText("Title_AI", aiT, "Ustawienia AI", 36, ColorGold,
                new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(340, 50));

            var labelColor = CreateText("Label_Color", aiT, "Twój kolor:", 20, ColorText,
                new Vector2(0.5f, 1f), new Vector2(0, -105), new Vector2(300, 30));

            var aiColorDrop = CreateDropdown("DD_Color", aiT,
                new[] { "Białe (ruszają pierwsze)", "Czarne" }, 0, -145, 300, 44);

            var labelDiff = CreateText("Label_Diff", aiT, "Trudność:", 20, ColorText,
                new Vector2(0.5f, 1f), new Vector2(0, -210), new Vector2(300, 30));

            var aiDiffDrop = CreateDropdown("DD_Diff", aiT,
                new[] { "Łatwy", "Normalny", "Trudny" }, 0, -250, 300, 44);

            var btnStartAI = CreateButton("Btn_StartAI", aiT, "Graj!", ColorButtonSuccess, 0, -330, 220, 58);
            var btnBackAI  = CreateButton("Btn_BackAI",  aiT, "← Wstecz", ColorButtonDanger, 0, -395, 160, 42);

            aiPanel.SetActive(false);

            // ── Online sub-panel ──────────────────────────────────────
            var onlinePanel = new GameObject("Panel_Online");
            onlinePanel.transform.SetParent(canvasT, false);
            var onlineRt = onlinePanel.AddComponent<RectTransform>();
            onlineRt.anchorMin = onlineRt.anchorMax = onlineRt.pivot = new Vector2(0.5f, 0.5f);
            onlineRt.anchoredPosition = new Vector2(0, -30);
            onlineRt.sizeDelta = new Vector2(400, 360);
            var onlineImg = onlinePanel.AddComponent<Image>();
            onlineImg.color = ColorPanelBg;
            var onlineT = onlinePanel.transform;

            CreateText("Title_Online", onlineT, "Multiplayer Online", 36, ColorGold,
                new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(360, 50));

            var serverInput = CreateInputField("Input_Server", onlineT,
                "Adres IP serwera…", 0, -100, 340, 52);

            var btnHost = CreateButton("Btn_Host", onlineT, "Hostuj grę",  ColorButtonSuccess, -90, -175, 160, 54);
            var btnJoin = CreateButton("Btn_Join", onlineT, "Dołącz",      ColorButtonNormal,   90, -175, 160, 54);
            var btnBackO= CreateButton("Btn_BackO",onlineT, "← Wstecz",   ColorButtonDanger,    0, -255, 160, 42);

            onlinePanel.SetActive(false);

            // ── Wire MainMenuManager fields ───────────────────────────
            var so = new SerializedObject(mmm);
            so.FindProperty("_btnPvP")              .objectReferenceValue = btnPvP;
            so.FindProperty("_btnVsAI")             .objectReferenceValue = btnVsAI;
            so.FindProperty("_btnOnline")           .objectReferenceValue = btnOnline;
            so.FindProperty("_aiOptionsPanel")      .objectReferenceValue = aiPanel;
            so.FindProperty("_aiColorDropdown")     .objectReferenceValue = aiColorDrop;
            so.FindProperty("_aiDifficultyDropdown").objectReferenceValue = aiDiffDrop;
            so.FindProperty("_onlinePanel")         .objectReferenceValue = onlinePanel;
            so.FindProperty("_serverAddressInput")  .objectReferenceValue = serverInput;
            so.FindProperty("_btnHost")             .objectReferenceValue = btnHost;
            so.FindProperty("_btnJoin")             .objectReferenceValue = btnJoin;
            so.FindProperty("_btnBack")             .objectReferenceValue = btnBackAI;  // shared Back wire
            so.FindProperty("_btnQuit")             .objectReferenceValue = btnQuit;
            so.ApplyModifiedProperties();

            // Start AI button – calls StartVsAI() directly
            btnStartAI.onClick.AddListener(mmm.StartVsAI);

            // Back buttons inside sub-panels call ShowMainButtons via a helper
            btnBackO.onClick.AddListener(() => {
                onlinePanel.SetActive(false);
                cardGo.SetActive(true);
            });

            EditorUtility.SetDirty(mmm);

            // ── Save scene ────────────────────────────────────────────
            EditorSceneManager.SaveScene(scene, ScenePath);

            // ── Add both scenes to Build Settings ─────────────────────
            AddSceneToBuildSettings(ScenePath);
            AddSceneToBuildSettings("Assets/Scenes/Game.unity");

            // ── Patch Game scene with GameBootstrap ───────────────────
            bool bootstrapAdded = AddGameBootstrapToGameScene(silent: true);

            string msg = $"Scena MainMenu zapisana:\n{ScenePath}\n\nObie sceny dodane do Build Settings.";
            if (bootstrapAdded)
                msg += "\n\nGameBootstrap dodany do sceny Game.";
            else
                msg += "\n\n⚠ Nie znaleziono sceny Game – uruchom\n'Add GameBootstrap to Game Scene'\npo ręcznym stworzeniu sceny Game.";

            Debug.Log("[MainMenuSceneSetup] MainMenu scene created at " + ScenePath);
            EditorUtility.DisplayDialog("Warcaby", msg, "OK");
        }

        // ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the Game scene, finds the GameObject with GameManager,
        /// adds GameBootstrap to it (if missing), saves the scene and returns
        /// to whatever was open before.
        /// </summary>
        [MenuItem("Tools/Warcaby/Add GameBootstrap to Game Scene")]
        public static void AddGameBootstrapToGameSceneMenu() =>
            AddGameBootstrapToGameScene(silent: false);

        private static bool AddGameBootstrapToGameScene(bool silent)
        {
            const string gameScenePath = "Assets/Scenes/Game.unity";

            if (!System.IO.File.Exists(
                    System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(Application.dataPath),
                        gameScenePath)))
            {
                if (!silent)
                    EditorUtility.DisplayDialog("Warcaby",
                        $"Nie znaleziono pliku:\n{gameScenePath}\n\nStwórz scenę Game i spróbuj ponownie.", "OK");
                return false;
            }

            // Remember current scene so we can return to it
            var currentScene = EditorSceneManager.GetActiveScene();
            string currentPath = currentScene.path;

            // Open Game scene additively so we don't lose the current one in memory
            var gameScene = EditorSceneManager.OpenScene(gameScenePath, OpenSceneMode.Additive);
            EditorSceneManager.SetActiveScene(gameScene);

            bool added = false;

            // Find GO with GameManager
            var gmComponent = Object.FindObjectOfType<GameManager>();
            if (gmComponent == null)
            {
                // GameManager not yet placed – create a host GO for it
                var hostGo = new GameObject("GameManager");
                gmComponent = hostGo.AddComponent<GameManager>();
                Undo.RegisterCreatedObjectUndo(hostGo, "Create GameManager GO");
                Debug.Log("[MainMenuSceneSetup] Created GameManager GO in Game scene.");
            }

            var targetGo = gmComponent.gameObject;

            // Add GameBootstrap if missing
            if (targetGo.GetComponent<GameBootstrap>() == null)
            {
                Undo.AddComponent<GameBootstrap>(targetGo);
                added = true;
                Debug.Log($"[MainMenuSceneSetup] GameBootstrap added to '{targetGo.name}' in Game scene.");
            }
            else
            {
                Debug.Log("[MainMenuSceneSetup] GameBootstrap already present – skipped.");
                added = true; // treat as success
            }

            EditorSceneManager.SaveScene(gameScene);

            // Return to original scene
            if (!string.IsNullOrEmpty(currentPath) && currentPath != gameScenePath)
            {
                EditorSceneManager.SetActiveScene(currentScene);
                EditorSceneManager.CloseScene(gameScene, true);
            }

            if (!silent)
                EditorUtility.DisplayDialog("Warcaby",
                    added
                        ? $"GameBootstrap przypisany do '{targetGo.name}' w scenie Game."
                        : "GameBootstrap już istniał – brak zmian.", "OK");

            return added;
        }

        // ═════════════════════════════════════════════════════════════════
        // Factories
        // ═════════════════════════════════════════════════════════════════

        private static Button CreateButton(string name, Transform parent,
            string label, Color bg, float x, float y, float w, float h)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);

            var img = go.AddComponent<Image>();
            img.color = bg;

            var btn = go.AddComponent<Button>();
            var cs = btn.colors;
            cs.normalColor      = bg;
            cs.highlightedColor = Color.Lerp(bg, Color.white, 0.22f);
            cs.pressedColor     = Color.Lerp(bg, Color.black, 0.22f);
            btn.colors = cs;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;

            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = label;
            tmp.fontSize  = 22;
            tmp.color     = new Color(0.93f, 0.87f, 0.75f);
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold;

            return btn;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent,
            string text, float fontSize, Color color,
            Vector2 anchor, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static TMP_Dropdown CreateDropdown(string name, Transform parent,
            string[] options, float x, float y, float w, float h)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            go.AddComponent<Image>().color = new Color(0.20f, 0.18f, 0.15f);

            var drop = go.AddComponent<TMP_Dropdown>();
            drop.ClearOptions();
            var list = new System.Collections.Generic.List<TMP_Dropdown.OptionData>();
            foreach (var o in options) list.Add(new TMP_Dropdown.OptionData(o));
            drop.AddOptions(list);

            // Caption label
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lblRt = lblGo.AddComponent<RectTransform>();
            lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = Vector2.one;
            lblRt.offsetMin = new Vector2(10, 0); lblRt.offsetMax = new Vector2(-30, 0);
            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            lbl.fontSize  = 18;
            lbl.color     = new Color(0.93f, 0.87f, 0.75f);
            lbl.alignment = TextAlignmentOptions.MidlineLeft;
            drop.captionText = lbl;

            return drop;
        }

        private static TMP_InputField CreateInputField(string name, Transform parent,
            string placeholder, float x, float y, float w, float h)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
            go.AddComponent<Image>().color = new Color(0.18f, 0.16f, 0.13f);

            var field = go.AddComponent<TMP_InputField>();

            var textAreaGo = new GameObject("TextArea");
            textAreaGo.transform.SetParent(go.transform, false);
            var taRt = textAreaGo.AddComponent<RectTransform>();
            taRt.anchorMin = Vector2.zero; taRt.anchorMax = Vector2.one;
            taRt.offsetMin = new Vector2(10, 5); taRt.offsetMax = new Vector2(-10, -5);
            textAreaGo.AddComponent<RectMask2D>();

            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(textAreaGo.transform, false);
            StretchFull(phGo.AddComponent<RectTransform>());
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            phTmp.text = placeholder; phTmp.fontSize = 18;
            phTmp.color = new Color(0.55f, 0.52f, 0.46f);
            field.placeholder = phTmp;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(textAreaGo.transform, false);
            StretchFull(textGo.AddComponent<RectTransform>());
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            textTmp.fontSize = 18; textTmp.color = new Color(0.93f, 0.87f, 0.75f);
            field.textComponent = textTmp;

            return field;
        }

        private static void CreateImage(string name, Transform parent,
            Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin; rt.offsetMax = offsetMax;
            go.AddComponent<Image>().color = color;
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        private static void EnsureScenesFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }

        private static void AddSceneToBuildSettings(string scenePath)
        {
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene>(
                EditorBuildSettings.scenes);

            foreach (var s in scenes)
                if (s.path == scenePath) return; // already added

            scenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
