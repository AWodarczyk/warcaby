#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Mirror;

namespace Warcaby.Editor
{
    /// <summary>
    /// Configures Online Multiplayer in the Game scene:
    ///   1. Adds CheckersNetworkManager (replaces default NetworkManager)
    ///   2. Assigns NetworkGameManager prefab
    ///   3. Assigns NetworkPlayer prefab
    ///
    /// Requires prefabs to already exist in Assets/Prefabs/
    /// (run Tools → Warcaby → Create All Prefabs first).
    ///
    /// Run via: Tools → Warcaby → Setup Network (Game Scene)
    /// </summary>
    public static class NetworkSceneSetup
    {
        private const string GameScenePath            = "Assets/Scenes/Game.unity";
        private const string PrefabNetworkPlayer      = "Assets/Prefabs/NetworkPlayer.prefab";
        private const string PrefabNetworkGameManager = "Assets/Prefabs/NetworkGameManager.prefab";

        [MenuItem("Tools/Warcaby/Setup Network (Game Scene)")]
        public static void SetupNetwork()
        {
            // ── Validate prefabs exist ────────────────────────────────
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabNetworkPlayer);
            var ngmPrefab    = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabNetworkGameManager);

            if (playerPrefab == null || ngmPrefab == null)
            {
                EditorUtility.DisplayDialog("Warcaby",
                    "Nie znaleziono prefabów sieciowych.\n\n" +
                    "Uruchom najpierw:\n  Tools → Warcaby → Create All Prefabs",
                    "OK");
                return;
            }

            // ── Open / switch to Game scene ───────────────────────────
            bool gameSceneExists = System.IO.File.Exists(
                System.IO.Path.Combine(
                    System.IO.Path.GetDirectoryName(Application.dataPath),
                    GameScenePath));

            var activeScene = EditorSceneManager.GetActiveScene();
            bool openedAdditively = false;

            UnityEngine.SceneManagement.Scene gameScene;

            if (gameSceneExists)
            {
                if (activeScene.path == GameScenePath)
                {
                    gameScene = activeScene;
                }
                else
                {
                    gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Additive);
                    EditorSceneManager.SetActiveScene(gameScene);
                    openedAdditively = true;
                }
            }
            else
            {
                // Create a new Game scene if it doesn't exist yet
                EnsureScenesFolderExists();
                gameScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Additive);
                EditorSceneManager.SetActiveScene(gameScene);
                openedAdditively = true;
            }

            // ── Remove any existing vanilla NetworkManager ────────────
            var oldNM = Object.FindObjectOfType<NetworkManager>();
            if (oldNM != null && oldNM.GetType() == typeof(NetworkManager))
            {
                Undo.DestroyObjectImmediate(oldNM.gameObject);
                Debug.Log("[NetworkSceneSetup] Removed default NetworkManager.");
            }

            // ── Add CheckersNetworkManager ────────────────────────────
            var existingCNM = Object.FindObjectOfType<Network.CheckersNetworkManager>();
            GameObject nmGo;
            Network.CheckersNetworkManager cnm;

            if (existingCNM != null)
            {
                nmGo = existingCNM.gameObject;
                cnm  = existingCNM;
                Debug.Log("[NetworkSceneSetup] CheckersNetworkManager already present.");
            }
            else
            {
                nmGo = new GameObject("NetworkManager");
                cnm  = Undo.AddComponent<Network.CheckersNetworkManager>(nmGo);
                Undo.RegisterCreatedObjectUndo(nmGo, "Add CheckersNetworkManager");
                Debug.Log("[NetworkSceneSetup] Created CheckersNetworkManager GO.");
            }

            // ── Transport (required by Mirror) ────────────────────────
            // KcpTransport is Mirror's default UDP transport (bundled with Mirror package)
            Transport transport = nmGo.GetComponent<Mirror.KcpTransport>();
            if (transport == null)
                transport = Undo.AddComponent<Mirror.KcpTransport>(nmGo);

            // Assign transport to NetworkManager via SerializedObject
            var nmSo = new SerializedObject(cnm);
            var transportProp = nmSo.FindProperty("transport");
            if (transportProp != null)
            {
                transportProp.objectReferenceValue = transport;
                nmSo.ApplyModifiedProperties();
                Debug.Log("[NetworkSceneSetup] KcpTransport assigned.");
            }
            else
            {
                Debug.LogWarning("[NetworkSceneSetup] Could not find 'transport' property – assign manually.");
            }

            // ── Assign prefabs via SerializedObject ───────────────────
            var so = new SerializedObject(cnm);

            // playerPrefab is a built-in Mirror NetworkManager field
            so.FindProperty("playerPrefab")
              .objectReferenceValue = playerPrefab;

            // _networkGameManagerPrefab is our custom field
            var ngmProp = so.FindProperty("_networkGameManagerPrefab");
            if (ngmProp != null)
                ngmProp.objectReferenceValue = ngmPrefab;
            else
                Debug.LogWarning("[NetworkSceneSetup] Field '_networkGameManagerPrefab' not found on CheckersNetworkManager.");

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(cnm);

            // ── Register NetworkPlayer prefab with Mirror's spawnable list ──
            RegisterSpawnablePrefab(cnm, playerPrefab);
            RegisterSpawnablePrefab(cnm, ngmPrefab);

            // ── Save scene ────────────────────────────────────────────
            if (gameSceneExists)
                EditorSceneManager.SaveScene(gameScene);
            else
                EditorSceneManager.SaveScene(gameScene, GameScenePath);

            // ── Return to original scene ──────────────────────────────
            if (openedAdditively && activeScene.IsValid() && activeScene.path != GameScenePath)
            {
                EditorSceneManager.SetActiveScene(activeScene);
                EditorSceneManager.CloseScene(gameScene, true);
            }

            Debug.Log("[NetworkSceneSetup] Network setup complete.");
            EditorUtility.DisplayDialog("Warcaby",
                "Konfiguracja sieciowa zakończona:\n\n" +
                "✓ CheckersNetworkManager dodany do sceny Game\n" +
                "✓ KcpTransport dodany i przypisany\n" +
                "✓ NetworkPlayer prefab przypisany\n" +
                "✓ NetworkGameManager prefab przypisany\n" +
                "✓ Prefaby zarejestrowane jako Spawnable",
                "OK");
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        /// <summary>
        /// Adds a prefab to Mirror's NetworkManager.spawnPrefabs list if not already there.
        /// </summary>
        private static void RegisterSpawnablePrefab(NetworkManager nm, GameObject prefab)
        {
            if (prefab == null) return;
            var so = new SerializedObject(nm);
            var spawnList = so.FindProperty("spawnPrefabs");
            if (spawnList == null) return;

            // Check duplicates
            for (int i = 0; i < spawnList.arraySize; i++)
            {
                if (spawnList.GetArrayElementAtIndex(i).objectReferenceValue == prefab)
                    return; // already registered
            }

            spawnList.arraySize++;
            spawnList.GetArrayElementAtIndex(spawnList.arraySize - 1).objectReferenceValue = prefab;
            so.ApplyModifiedProperties();
            Debug.Log($"[NetworkSceneSetup] Registered spawnable prefab: {prefab.name}");
        }

        private static void EnsureScenesFolderExists()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");
        }
    }
}
#endif
