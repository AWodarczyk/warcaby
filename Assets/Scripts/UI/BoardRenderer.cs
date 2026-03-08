using System.Collections.Generic;
using UnityEngine;
using Warcaby.Core;

namespace Warcaby.UI
{
    /// <summary>
    /// Renders the checkers board and pieces using SpriteRenderers.
    /// All visuals are created procedurally at runtime – no prefab assets required.
    /// Prefab fields are kept as optional overrides.
    ///
    /// Coordinate mapping:
    ///   Board(row=0,col=0) → top-left in world space.
    ///   Each cell = 1 Unity unit, centred at (col+0.5, -(row+0.5)).
    /// </summary>
    public class BoardRenderer : MonoBehaviour
    {
        [Header("Prefab overrides (leave empty to use procedural visuals)")]
        [SerializeField] private GameObject _lightTilePrefab;
        [SerializeField] private GameObject _darkTilePrefab;
        [SerializeField] private GameObject _whitePiecePrefab;
        [SerializeField] private GameObject _blackPiecePrefab;
        [SerializeField] private GameObject _whiteKingPrefab;
        [SerializeField] private GameObject _blackKingPrefab;

        // ─── Colours ──────────────────────────────────────────────────────
        private static readonly Color ColLightTile = new Color(0.93f, 0.84f, 0.69f);
        private static readonly Color ColDarkTile  = new Color(0.56f, 0.35f, 0.18f);
        private static readonly Color ColWhite     = new Color(0.95f, 0.95f, 0.92f);
        private static readonly Color ColBlack     = new Color(0.15f, 0.12f, 0.10f);
        private static readonly Color ColKingGold  = new Color(1.00f, 0.85f, 0.10f);
        private static readonly Color ColSelected  = new Color(1.00f, 1.00f, 0.00f, 0.55f);
        private static readonly Color ColMoveDot   = new Color(0.00f, 0.90f, 0.20f, 0.55f);

        // ─── Runtime ──────────────────────────────────────────────────────
        private Sprite _squareSprite;
        private Sprite _circleSprite;
        private readonly Dictionary<BoardPosition, GameObject> _pieces    = new();
        private readonly List<GameObject>                      _highlights = new();
        private GameManager _gm;

        // ═════════════════════════════════════════════════════════════════
        // Unity lifecycle
        // ═════════════════════════════════════════════════════════════════

        private void Start()
        {
            // Generate procedural sprites once at startup
            _squareSprite = MakeSquareSprite();
            _circleSprite = MakeCircleSprite();

            CreateTiles();

            _gm = GameManager.Instance;
            if (_gm == null) { Debug.LogError("[BoardRenderer] GameManager not found!"); return; }
            _gm.OnBoardChanged += Redraw;

            // GameBootstrap.Start() may have fired before us – redraw immediately if board exists.
            if (_gm.Board != null)
                Redraw(_gm.Board);
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnBoardChanged -= Redraw;
        }

        // ═════════════════════════════════════════════════════════════════
        // Board & piece rendering
        // ═════════════════════════════════════════════════════════════════

        private void CreateTiles()
        {
            for (int r = 0; r < Board.Size; r++)
            {
                for (int c = 0; c < Board.Size; c++)
                {
                    bool dark = (r + c) % 2 == 1;

                    // Try prefab first (might be null / have missing sprite)
                    var prefab = dark ? _darkTilePrefab : _lightTilePrefab;
                    if (PrefabHasSprite(prefab))
                    {
                        Instantiate(prefab, Cell(r, c), Quaternion.identity, transform)
                            .name = $"Tile_{r}_{c}";
                        continue;
                    }

                    // Procedural fallback – always works
                    var tile = new GameObject($"Tile_{r}_{c}");
                    tile.transform.SetParent(transform);
                    tile.transform.position = Cell(r, c);
                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite       = _squareSprite;
                    sr.color        = dark ? ColDarkTile : ColLightTile;
                    sr.sortingOrder = 0;
                }
            }
        }

        private void Redraw(Board board)
        {
            ClearPieces();
            ClearHighlights();

            for (int r = 0; r < Board.Size; r++)
            {
                for (int c = 0; c < Board.Size; c++)
                {
                    var pos   = new BoardPosition(r, c);
                    var piece = board.GetPiece(pos);
                    if (piece == PieceType.None) continue;

                    var go = TryInstantiatePrefab(piece, Cell(r, c))
                          ?? MakeProceduralPiece(piece, Cell(r, c));
                    go.name = $"Piece_{r}_{c}";
                    go.transform.SetParent(transform);
                    _pieces[pos] = go;
                }
            }

            if (_gm.SelectedPosition.HasValue)
            {
                ShowHighlight(_gm.SelectedPosition.Value, ColSelected);
                foreach (var move in _gm.GetMovesFromPosition(_gm.SelectedPosition.Value))
                    ShowHighlight(move.To, ColMoveDot);
            }
        }

        // ─── Prefab helpers ───────────────────────────────────────────────

        private static bool PrefabHasSprite(GameObject prefab)
        {
            if (prefab == null) return false;
            var sr = prefab.GetComponent<SpriteRenderer>();
            return sr != null && sr.sprite != null;
        }

        private GameObject TryInstantiatePrefab(PieceType type, Vector3 pos)
        {
            var prefab = type switch
            {
                PieceType.White     => _whitePiecePrefab,
                PieceType.Black     => _blackPiecePrefab,
                PieceType.WhiteKing => _whiteKingPrefab,
                PieceType.BlackKing => _blackKingPrefab,
                _                   => null
            };
            return PrefabHasSprite(prefab) ? Instantiate(prefab, pos, Quaternion.identity) : null;
        }

        // ─── Procedural piece ─────────────────────────────────────────────

        private GameObject MakeProceduralPiece(PieceType type, Vector3 pos)
        {
            bool isWhite = type == PieceType.White     || type == PieceType.WhiteKing;
            bool isKing  = type == PieceType.WhiteKing || type == PieceType.BlackKing;

            var root = new GameObject();
            root.transform.position = pos;

            // Body
            var bodySR     = root.AddComponent<SpriteRenderer>();
            bodySR.sprite  = _circleSprite;
            bodySR.color   = isWhite ? ColWhite : ColBlack;
            bodySR.sortingOrder = 2;

            // Dark outline ring
            var outline = new GameObject("Outline");
            outline.transform.SetParent(root.transform, false);
            outline.transform.localScale = new Vector3(1.08f, 1.08f, 1f);
            var outSR    = outline.AddComponent<SpriteRenderer>();
            outSR.sprite = _circleSprite;
            outSR.color  = new Color(0f, 0f, 0f, 0.45f);
            outSR.sortingOrder = 1;

            // Golden centre dot for kings
            if (isKing)
            {
                var crown = new GameObject("Crown");
                crown.transform.SetParent(root.transform, false);
                crown.transform.localScale    = new Vector3(0.38f, 0.38f, 1f);
                crown.transform.localPosition = new Vector3(0f, 0f, -0.1f);
                var crSR   = crown.AddComponent<SpriteRenderer>();
                crSR.sprite     = _circleSprite;
                crSR.color      = ColKingGold;
                crSR.sortingOrder = 3;
            }

            return root;
        }

        // ─── Highlights ───────────────────────────────────────────────────

        private void ShowHighlight(BoardPosition pos, Color color)
        {
            var go = new GameObject($"Highlight_{pos}");
            go.transform.SetParent(transform);
            go.transform.position = Cell(pos.Row, pos.Col) + new Vector3(0f, 0f, -0.05f);
            var sr       = go.AddComponent<SpriteRenderer>();
            sr.sprite    = _squareSprite;
            sr.color     = color;
            sr.sortingOrder = 1;
            _highlights.Add(go);
        }

        private void ClearHighlights()
        {
            foreach (var h in _highlights) Destroy(h);
            _highlights.Clear();
        }

        private void ClearPieces()
        {
            foreach (var kvp in _pieces) Destroy(kvp.Value);
            _pieces.Clear();
        }

        // ═════════════════════════════════════════════════════════════════
        // Coordinate helpers
        // ═════════════════════════════════════════════════════════════════

        /// <summary>Board (row, col) → world-space centre. row 0 = top, col 0 = left.</summary>
        private static Vector3 Cell(int row, int col) =>
            new Vector3(col + 0.5f, -(row + 0.5f), 0f);

        /// <summary>World-space position → BoardPosition (used by InputHandler).</summary>
        public static BoardPosition WorldToBoard(Vector3 world)
        {
            int col = Mathf.FloorToInt(world.x);
            int row = Mathf.FloorToInt(-world.y);
            return new BoardPosition(row, col);
        }

        // ═════════════════════════════════════════════════════════════════
        // Procedural sprite generators (runtime, no asset import needed)
        // ═════════════════════════════════════════════════════════════════

        private static Sprite MakeSquareSprite()
        {
            const int S = 64;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[S * S];
            for (int i = 0; i < px.Length; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        }

        private static Sprite MakeCircleSprite()
        {
            const int S = 64;
            float r = S / 2f;
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                px[y * S + x] = new Color(1, 1, 1, Mathf.Clamp01(r - dist));
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        }
    }
}
