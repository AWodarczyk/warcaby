#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace Warcaby.Editor
{
    /// <summary>
    /// Creates and fully configures the Game scene:
    ///   - GameManager GO (+ GameBootstrap, + GameManager component)
    ///   - Camera GO (+ InputHandler)
    ///   - BoardRenderer GO
    ///   - UIManager GO (delegates to UIManagerSetup)
    ///
    /// Run via: Tools → Warcaby → Setup Game Scene
    /// </summary>
    public static class GameSceneSetup
    {
        public const string GameScenePath = "Assets/Scenes/Game.unity";

        [MenuItem("Tools/Warcaby/Setup Game Scene")]
        public static void SetupGameScene()
        {
            EnsureScenesFolderExists();

            // Open or create the scene
            UnityEngine.SceneManagement.Scene gameScene;
            bool isAlreadyActive = EditorSceneManager.GetActiveScene().path == GameScenePath;

            if (System.IO.File.Exists(
                    System.IO.Path.Combine(
                        System.IO.Path.GetDirectoryName(Application.dataPath),
                        GameScenePath)))
            {
                if (!isAlreadyActive)
                    gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
                else
                    gameScene = EditorSceneManager.GetActiveScene();
            }
            else
            {
                gameScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            EditorSceneManager.SetActiveScene(gameScene);

            // ── Camera ────────────────────────────────────────────────
            var camGo = GameObject.Find("Main Camera") ?? new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.GetOrAddComp<Camera>();
            cam.orthographic     = true;
            cam.orthographicSize = 5f;
            cam.backgroundColor  = new Color(0.08f, 0.07f, 0.06f);
            cam.clearFlags       = CameraClearFlags.SolidColor;
            // Centre camera on 8x8 board (board spans 0..8 in X, -8..0 in Y → centre 4,-4)
            camGo.transform.position = new Vector3(4f, -4f, -10f);
            camGo.GetOrAddComp<Input.InputHandler>();

            // ── GameManager ───────────────────────────────────────────
            var gmGo = FindOrCreate("GameManager");
            gmGo.GetOrAddComp<GameManager>();
            gmGo.GetOrAddComp<GameBootstrap>();

            // ── BoardRenderer ─────────────────────────────────────────
            var boardGo = FindOrCreate("BoardRenderer");
            boardGo.GetOrAddComp<UI.BoardRenderer>();

            // ── UIManager ─────────────────────────────────────────────
            // UIManagerSetup will create Canvas_UI as a child of UIManager
            var uiGo = FindOrCreate("UIManager");
            uiGo.GetOrAddComp<UI.UIManager>();

            // ── EventSystem ───────────────────────────────────────────
            if (Object.FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            // ── Save ──────────────────────────────────────────────────
            EditorSceneManager.SaveScene(gameScene, GameScenePath);
            AssetDatabase.Refresh();

            Debug.Log("[GameSceneSetup] Game scene saved to " + GameScenePath);
            EditorUtility.DisplayDialog("Warcaby",
                "Scena Game skonfigurowana:\n\n" +
                "✓ Camera + InputHandler\n" +
                "✓ GameManager + GameBootstrap\n" +
                "✓ BoardRenderer\n" +
                "✓ UIManager\n" +
                "✓ EventSystem\n\n" +
                "Zapisano: " + GameScenePath, "OK");
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        private static GameObject FindOrCreate(string name)
        {
            return GameObject.Find(name) ?? new GameObject(name);
        }

        private static void EnsureScenesFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }

    internal static class GameObjectExtHelper
    {
        /// <summary>
        /// Returns existing component or adds a new one using plain AddComponent.
        /// Undo.AddComponent returns null on freshly created, unregistered GameObjects.
        /// </summary>
        public static T GetOrAddComp<T>(this GameObject go) where T : Component
        {
            return go.GetComponent<T>() ?? go.AddComponent<T>();
        }
    }
}
#endif
