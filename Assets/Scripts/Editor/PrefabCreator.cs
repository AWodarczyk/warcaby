#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;
using Warcaby.Network;

namespace Warcaby.Editor
{
    /// <summary>
    /// Generates all game prefabs into Assets/Prefabs/.
    /// Run via: menu Tools → Warcaby → Create All Prefabs
    /// </summary>
    public static class PrefabCreator
    {
        private const string PrefabsPath = "Assets/Prefabs";

        // ─── Colors ───────────────────────────────────────────────────────
        private static readonly Color ColorLightTile   = new Color(0.93f, 0.84f, 0.69f); // jasny krem
        private static readonly Color ColorDarkTile    = new Color(0.56f, 0.35f, 0.18f); // ciemny brąz
        private static readonly Color ColorWhitePiece  = new Color(0.95f, 0.95f, 0.92f); // kość słoniowa
        private static readonly Color ColorBlackPiece  = new Color(0.15f, 0.12f, 0.10f); // prawie czarny
        private static readonly Color ColorKingAccent  = new Color(1.00f, 0.85f, 0.10f); // złoty (dla damy)
        private static readonly Color ColorHighlight   = new Color(1.00f, 1.00f, 0.00f, 0.55f);

        [MenuItem("Tools/Warcaby/Create All Prefabs")]
        public static void CreateAllPrefabs()
        {
            EnsureDirectory(PrefabsPath);

            CreateTilePrefab("LightTile",  ColorLightTile);
            CreateTilePrefab("DarkTile",   ColorDarkTile);

            CreatePiecePrefab("WhitePiece", ColorWhitePiece, isKing: false);
            CreatePiecePrefab("BlackPiece", ColorBlackPiece, isKing: false);
            CreatePiecePrefab("WhiteKing",  ColorWhitePiece, isKing: true, crownColor: ColorKingAccent);
            CreatePiecePrefab("BlackKing",  ColorBlackPiece, isKing: true, crownColor: ColorKingAccent);

            CreateNetworkPlayerPrefab();
            CreateNetworkGameManagerPrefab();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[PrefabCreator] All prefabs created in " + PrefabsPath);
            EditorUtility.DisplayDialog("Warcaby", "Prefaby zostały wygenerowane w Assets/Prefabs/", "OK");
        }

        // ─── Tile Prefab ──────────────────────────────────────────────────

        private static void CreateTilePrefab(string name, Color color)
        {
            var go = new GameObject(name);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite  = CreateSquareSprite(64);
            sr.color   = color;
            sr.sortingOrder = 0;

            // Box collider for click detection
            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one;

            SavePrefab(go, name);
            Object.DestroyImmediate(go);
        }

        // ─── Piece Prefab ─────────────────────────────────────────────────

        private static void CreatePiecePrefab(string name, Color color,
            bool isKing, Color crownColor = default)
        {
            var go = new GameObject(name);

            // Body (circle)
            var body = go;
            var sr = body.AddComponent<SpriteRenderer>();
            sr.sprite = CreateCircleSprite(64);
            sr.color  = color;
            sr.sortingOrder = 2;

            // Outline ring
            var outline = new GameObject("Outline");
            outline.transform.SetParent(go.transform);
            outline.transform.localPosition = Vector3.zero;
            outline.transform.localScale = new Vector3(1.05f, 1.05f, 1f);
            var outlineSr = outline.AddComponent<SpriteRenderer>();
            outlineSr.sprite = CreateCircleSprite(64);
            outlineSr.color  = new Color(0f, 0f, 0f, 0.5f);
            outlineSr.sortingOrder = 1;

            // Crown marker for kings
            if (isKing)
            {
                var crown = new GameObject("Crown");
                crown.transform.SetParent(go.transform);
                crown.transform.localPosition = new Vector3(0, 0, -0.1f);
                crown.transform.localScale = new Vector3(0.4f, 0.4f, 1f);
                var crownSr = crown.AddComponent<SpriteRenderer>();
                crownSr.sprite = CreateStarSprite(64);
                crownSr.color  = crownColor;
                crownSr.sortingOrder = 3;
            }

            // Collider
            go.AddComponent<CircleCollider2D>().radius = 0.45f;

            SavePrefab(go, name);
            Object.DestroyImmediate(go);
        }

        // ─── Network Prefabs ──────────────────────────────────────────────

        private static void CreateNetworkPlayerPrefab()
        {
            var go = new GameObject("NetworkPlayer");
            go.AddComponent<Mirror.NetworkIdentity>();
            go.AddComponent<NetworkPlayer>();
            SavePrefab(go, "NetworkPlayer");
            Object.DestroyImmediate(go);
        }

        private static void CreateNetworkGameManagerPrefab()
        {
            var go = new GameObject("NetworkGameManager");
            go.AddComponent<Mirror.NetworkIdentity>();
            go.AddComponent<NetworkGameManager>();
            SavePrefab(go, "NetworkGameManager");
            Object.DestroyImmediate(go);
        }

        // ─── Sprite generators ─────────────────────────────────────────────

        /// <summary>Creates a white filled square sprite.</summary>
        private static Sprite CreateSquareSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>Creates a white filled circle sprite with anti-aliased edge.</summary>
        private static Sprite CreateCircleSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            float r = size / 2f;
            var pixels = new Color[size * size];

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                                  new Vector2(r, r));
                    float alpha = Mathf.Clamp01(r - dist);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
        }

        /// <summary>Creates a simple 5-point star sprite for king crowns.</summary>
        private static Sprite CreateStarSprite(int size)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            var pixels = new Color[size * size];
            float cx = size / 2f, cy = size / 2f;
            float outerR = size * 0.45f, innerR = size * 0.20f;

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float px = x + 0.5f - cx, py = y + 0.5f - cy;
                    float angle = Mathf.Atan2(py, px);
                    float dist  = Mathf.Sqrt(px * px + py * py);

                    // Interpolate between inner and outer radius at 5-pointed star
                    float normalizedAngle = angle / (2f * Mathf.PI) * 10f;
                    float fraction = normalizedAngle - Mathf.Floor(normalizedAngle);
                    float starRadius = Mathf.Lerp(outerR, innerR,
                        Mathf.Abs(fraction - 0.5f) * 2f);

                    float alpha = dist <= starRadius ? 1f :
                                  Mathf.Clamp01(starRadius + 1.5f - dist);
                    pixels[y * size + x] = new Color(1, 1, 1, alpha);
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
        }

        // ─── Helpers ──────────────────────────────────────────────────────

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path);
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }

        private static void SavePrefab(GameObject go, string name)
        {
            string path = $"{PrefabsPath}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Debug.Log($"[PrefabCreator] Created: {path}");
        }
    }
}
#endif
