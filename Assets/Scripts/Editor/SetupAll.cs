#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Warcaby.Editor
{
    /// <summary>
    /// One-click full project setup. Runs every step in the correct order.
    /// Run via: Tools → Warcaby → ★ SETUP EVERYTHING ★
    /// </summary>
    public static class SetupAll
    {
        [MenuItem("Tools/Warcaby/★ SETUP EVERYTHING ★", priority = -100)]
        public static void RunAll()
        {
            bool ok = EditorUtility.DisplayDialog("Warcaby – Setup All",
                "Zostanie wykonana pełna konfiguracja projektu:\n\n" +
                "1. Generowanie prefabów\n" +
                "2. Tworzenie i konfiguracja sceny Game\n" +
                "3. Budowanie UI Canvas + przypisanie referencji\n" +
                "4. Konfiguracja sieci Mirror w scenie Game\n" +
                "5. Tworzenie sceny MainMenu\n" +
                "6. Dodanie obu scen do Build Settings\n\n" +
                "Kontynuować?",
                "Tak, zrób to!", "Anuluj");

            if (!ok) return;

            int step = 0;

            try
            {
                // ── Step 1: Prefabs ───────────────────────────────────
                EditorUtility.DisplayProgressBar("Warcaby Setup", "Krok 1/5 – Generowanie prefabów…", 0.0f);
                PrefabCreator.CreateAllPrefabs();
                step = 1;

                // ── Step 2: Game scene ────────────────────────────────
                EditorUtility.DisplayProgressBar("Warcaby Setup", "Krok 2/5 – Konfiguracja sceny Game…", 0.2f);
                GameSceneSetup.SetupGameScene();
                step = 2;

                // ── Step 3: UI Manager ────────────────────────────────
                EditorUtility.DisplayProgressBar("Warcaby Setup", "Krok 3/5 – Budowanie UI Canvas…", 0.4f);
                UIManagerSetup.SetupUIManager();

                EditorUtility.DisplayProgressBar("Warcaby Setup", "Krok 3/5 – Przypisywanie referencji UI…", 0.5f);
                UIManagerSetup.AssignUIReferences();
                step = 3;

                // ── Step 4: Network ───────────────────────────────────
                EditorUtility.DisplayProgressBar("Warcaby Setup", "Krok 4/5 – Konfiguracja Mirror…", 0.7f);
                NetworkSceneSetup.SetupNetwork();
                step = 4;

                // ── Step 5: MainMenu scene ────────────────────────────
                EditorUtility.DisplayProgressBar("Warcaby Setup", "Krok 5/5 – Tworzenie sceny MainMenu…", 0.85f);
                MainMenuSceneSetup.SetupMainMenuScene();
                step = 5;

                EditorUtility.ClearProgressBar();

                Debug.Log("[SetupAll] Full setup complete!");
                EditorUtility.DisplayDialog("Warcaby",
                    "✓ Konfiguracja zakończona!\n\n" +
                    "✓ Prefaby wygenerowane\n" +
                    "✓ Scena Game skonfigurowana\n" +
                    "✓ UI Canvas + referencje przypisane\n" +
                    "✓ Mirror skonfigurowany\n" +
                    "✓ Scena MainMenu utworzona\n\n" +
                    "Otwórz scenę MainMenu i naciśnij ▶ Play!",
                    "Graj!");
            }
            catch (System.Exception e)
            {
                EditorUtility.ClearProgressBar();
                Debug.LogError($"[SetupAll] Błąd na kroku {step + 1}: {e.Message}\n{e.StackTrace}");
                EditorUtility.DisplayDialog("Warcaby – Błąd",
                    $"Błąd na kroku {step + 1}:\n\n{e.Message}\n\n" +
                    "Sprawdź Console i uruchom pozostałe kroki ręcznie.", "OK");
            }
        }
    }
}
#endif
