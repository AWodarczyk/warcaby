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
        // Pitch: two shades of grass green
        private static readonly Color ColLightTile = new Color(0.30f, 0.62f, 0.25f); // jasna trawa
        private static readonly Color ColDarkTile  = new Color(0.22f, 0.48f, 0.18f); // ciemna trawa

        // Team colours
        private static readonly Color ColWhiteBall  = new Color(0.96f, 0.96f, 0.94f); // biała piłka
        private static readonly Color ColWhitePatch = new Color(0.12f, 0.12f, 0.12f); // czarne łaty
        private static readonly Color ColBlackBall  = new Color(0.90f, 0.15f, 0.15f); // czerwona drużyna
        private static readonly Color ColBlackPatch = new Color(0.10f, 0.10f, 0.10f); // ciemne łaty
        private static readonly Color ColKingRing   = new Color(1.00f, 0.82f, 0.05f); // złota obwódka damy
        private static readonly Color ColKingStar   = new Color(1.00f, 0.95f, 0.40f); // złota gwiazdka

        private static readonly Color ColSelected   = new Color(1.00f, 1.00f, 0.00f, 0.60f);
        private static readonly Color ColMoveDot    = new Color(0.00f, 0.95f, 0.30f, 0.55f);

        // ─── Runtime ──────────────────────────────────────────────────────
        private Sprite _squareSprite;
        private Sprite _circleSprite;
        private Sprite _whiteBallSprite;
        private Sprite _blackBallSprite;
        private Sprite _whiteKingSprite;
        private Sprite _blackKingSprite;
        private readonly Dictionary<BoardPosition, GameObject> _pieces    = new();
        private readonly List<GameObject>                      _highlights = new();
        private GameManager _gm;

        // ═════════════════════════════════════════════════════════════════
        // Unity lifecycle
        // ═════════════════════════════════════════════════════════════════

        private void Start()
        {
            // Generate procedural sprites once at startup
            _squareSprite   = MakeSquareSprite();
            _circleSprite   = MakeCircleSprite();
            _whiteBallSprite = MakeSoccerBallSprite(ColWhiteBall, ColWhitePatch);
            _blackBallSprite = MakeSoccerBallSprite(ColBlackBall, ColBlackPatch);
            _whiteKingSprite = MakeSoccerBallSprite(ColWhiteBall, ColKingStar, isKing: true);
            _blackKingSprite = MakeSoccerBallSprite(ColBlackBall, ColKingStar, isKing: true);

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

                    // Procedural pitch tile – alternating row stripes on playable (dark) squares
                    var tile = new GameObject($"Tile_{r}_{c}");
                    tile.transform.SetParent(transform);
                    tile.transform.position = Cell(r, c);
                    var sr = tile.AddComponent<SpriteRenderer>();
                    sr.sprite       = dark ? MakePitchTileSprite(r) : _squareSprite;
                    sr.color        = dark ? ColDarkTile : ColLightTile;
                    sr.sortingOrder = 0;
                }
            }
        }

        /// <summary>
        /// Generates a tile sprite with subtle vertical "mowing stripes" for the dark
        /// (playable) squares, alternating between two shades per row – classic stadium look.
        /// </summary>
        private static Sprite MakePitchTileSprite(int row)
        {
            const int S   = 64;
            const int stripeW = 8; // pixels per stripe
            bool invertStripe = (row % 2 == 0);

            var tex = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px = new Color[S * S];
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                int stripe = x / stripeW;
                bool lighter = ((stripe % 2 == 0) ^ invertStripe);
                // Slight brightness difference between stripes
                float v = lighter ? 1.06f : 0.94f;
                px[y * S + x] = new Color(v, v, v, 1f);
            }
            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
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

            Sprite ballSprite = isKing
                ? (isWhite ? _whiteKingSprite : _blackKingSprite)
                : (isWhite ? _whiteBallSprite  : _blackBallSprite);

            var root = new GameObject();
            root.transform.position = pos;

            // Ball body
            var sr     = root.AddComponent<SpriteRenderer>();
            sr.sprite  = ballSprite;
            sr.color   = Color.white;   // tint applied via sprite texture
            sr.sortingOrder = 2;

            // Gold ring outline for kings
            if (isKing)
            {
                var ring = new GameObject("Ring");
                ring.transform.SetParent(root.transform, false);
                ring.transform.localScale = new Vector3(1.14f, 1.14f, 1f);
                var ringSR     = ring.AddComponent<SpriteRenderer>();
                ringSR.sprite  = _circleSprite;
                ringSR.color   = ColKingRing;
                ringSR.sortingOrder = 1;
            }

            // Thin dark shadow ring for normal pieces (visual depth)
            var shadow = new GameObject("Shadow");
            shadow.transform.SetParent(root.transform, false);
            shadow.transform.localScale    = new Vector3(1.06f, 1.06f, 1f);
            shadow.transform.localPosition = new Vector3(0.03f, -0.03f, 0.1f);
            var shadowSR   = shadow.AddComponent<SpriteRenderer>();
            shadowSR.sprite     = _circleSprite;
            shadowSR.color      = new Color(0f, 0f, 0f, 0.35f);
            shadowSR.sortingOrder = 0;

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

        /// <summary>White filled square, 1 Unity unit = 1 tile (PPU = S).</summary>
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

        /// <summary>White anti-aliased circle filled.</summary>
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

        /// <summary>
        /// Soccer-ball sprite: coloured circle with 6 darker pentagon patches.
        /// Kings get a 5-point star in the centre instead of the central patch.
        /// </summary>
        private static Sprite MakeSoccerBallSprite(Color ballColor, Color patchColor,
            bool isKing = false)
        {
            const int S  = 128;
            float r      = S / 2f;
            var tex      = new Texture2D(S, S, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var px       = new Color[S * S];

            // 1) Base circle
            for (int y = 0; y < S; y++)
            for (int x = 0; x < S; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                // Smooth edge
                float alpha = Mathf.Clamp01((r - 1f) - dist + 1.5f);
                // Slight rim shading for 3D feel
                float shade = Mathf.Clamp01(1f - (dist / r) * 0.25f);
                px[y * S + x] = new Color(
                    ballColor.r * shade,
                    ballColor.g * shade,
                    ballColor.b * shade,
                    alpha);
            }

            // 2) Central patch (or star for kings)
            if (isKing)
            {
                PaintStar(px, S, r, r, r * 0.28f, r * 0.13f, patchColor);
            }
            else
            {
                PaintPatch(px, S, r, r, r * 0.18f, patchColor, ballColor);
            }

            // 3) Ring of 5 patches offset from centre
            float ringR = r * 0.46f;
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad - Mathf.PI / 2f;
                float cx = r + ringR * Mathf.Cos(angle);
                float cy = r + ringR * Mathf.Sin(angle);
                PaintPatch(px, S, cx, cy, r * 0.15f, patchColor, ballColor);
            }

            tex.SetPixels(px);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, S, S), new Vector2(0.5f, 0.5f), S);
        }

        /// <summary>Paints a soft circular patch onto a pixel array.</summary>
        private static void PaintPatch(Color[] px, int S, float cx, float cy,
            float patchR, Color patch, Color ball)
        {
            float ballR  = S / 2f;
            float ballCx = S / 2f, ballCy = S / 2f;

            int x0 = Mathf.Max(0, (int)(cx - patchR - 2));
            int x1 = Mathf.Min(S - 1, (int)(cx + patchR + 2));
            int y0 = Mathf.Max(0, (int)(cy - patchR - 2));
            int y1 = Mathf.Min(S - 1, (int)(cy + patchR + 2));

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float onBall = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                                new Vector2(ballCx, ballCy));
                if (onBall >= ballR) continue;

                float dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f),
                                               new Vector2(cx, cy));
                float alpha = Mathf.Clamp01(patchR - dist + 0.5f);
                if (alpha <= 0f) continue;

                int idx = y * S + x;
                px[idx] = Color.Lerp(px[idx], new Color(patch.r, patch.g, patch.b, px[idx].a), alpha);
            }
        }

        /// <summary>Paints a 5-point star for king pieces.</summary>
        private static void PaintStar(Color[] px, int S, float cx, float cy,
            float outerR, float innerR, Color starColor)
        {
            float ballR = S / 2f;

            int x0 = Mathf.Max(0, (int)(cx - outerR - 2));
            int x1 = Mathf.Min(S - 1, (int)(cx + outerR + 2));
            int y0 = Mathf.Max(0, (int)(cy - outerR - 2));
            int y1 = Mathf.Min(S - 1, (int)(cy + outerR + 2));

            for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px2 = x + 0.5f - cx, py2 = y + 0.5f - cy;
                float onBall = Mathf.Sqrt((x + 0.5f - S / 2f) * (x + 0.5f - S / 2f) +
                                          (y + 0.5f - S / 2f) * (y + 0.5f - S / 2f));
                if (onBall >= ballR) continue;

                float angle = Mathf.Atan2(py2, px2);
                float dist  = Mathf.Sqrt(px2 * px2 + py2 * py2);

                // Polar equation of a 5-point star
                float starAngle = angle / (2f * Mathf.PI) * 10f;
                float frac      = starAngle - Mathf.Floor(starAngle);
                float starR     = Mathf.Lerp(outerR, innerR, Mathf.Abs(frac - 0.5f) * 2f);

                float alpha = Mathf.Clamp01(starR + 1f - dist);
                if (alpha <= 0f) continue;

                int idx = y * S + x;
                px[idx] = Color.Lerp(px[idx], new Color(starColor.r, starColor.g, starColor.b, px[idx].a), alpha);
            }
        }
    }
}
