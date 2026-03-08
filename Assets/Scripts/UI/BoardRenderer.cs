using System.Collections.Generic;
using UnityEngine;
using Warcaby.Core;

namespace Warcaby.UI
{
    /// <summary>
    /// Renders the board and pieces using SpriteRenderer.
    /// Place this on a GameObject with all tile/piece child prefabs referenced.
    /// 
    /// Coordinate mapping:
    ///   Board(row=0,col=0) → top-left in world space.
    ///   Each cell = 1 Unity unit square, centered at (col + 0.5, -(row + 0.5)).
    /// </summary>
    public class BoardRenderer : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject _lightTilePrefab;
        [SerializeField] private GameObject _darkTilePrefab;
        [SerializeField] private GameObject _whitePiecePrefab;
        [SerializeField] private GameObject _blackPiecePrefab;
        [SerializeField] private GameObject _whiteKingPrefab;
        [SerializeField] private GameObject _blackKingPrefab;

        [Header("Highlight Sprites")]
        [SerializeField] private Sprite _selectedHighlight;
        [SerializeField] private Sprite _moveHighlight;
        [SerializeField] private Sprite _captureHighlight;
        [SerializeField] private Color _selectedColor = new Color(1f, 1f, 0f, 0.5f);
        [SerializeField] private Color _moveColor    = new Color(0f, 1f, 0f, 0.4f);

        // Runtime data
        private readonly Dictionary<BoardPosition, GameObject> _pieces = new();
        private readonly List<GameObject> _highlights = new();
        private GameManager _gm;

        // ─── Unity ────────────────────────────────────────────────────────

        private void Start()
        {
            CreateTiles();
            _gm = GameManager.Instance;
            if (_gm == null) { Debug.LogError("[BoardRenderer] GameManager not found!"); return; }
            _gm.OnBoardChanged += Redraw;

            // GameBootstrap.Start() may have already called StartGame() before we subscribed.
            // If the board is already initialized, draw it immediately.
            if (_gm.Board != null)
                Redraw(_gm.Board);
        }

        private void OnDestroy()
        {
            if (_gm != null) _gm.OnBoardChanged -= Redraw;
        }

        // ─── Tile creation ────────────────────────────────────────────────

        private void CreateTiles()
        {
            for (int r = 0; r < Board.Size; r++)
            {
                for (int c = 0; c < Board.Size; c++)
                {
                    bool isDark = (r + c) % 2 == 1;
                    var prefab = isDark ? _darkTilePrefab : _lightTilePrefab;
                    var tile = Instantiate(prefab, CellCenter(r, c), Quaternion.identity, transform);
                    tile.name = $"Tile_{r}_{c}";
                }
            }
        }

        // ─── Board redraw ─────────────────────────────────────────────────

        private void Redraw(Board board)
        {
            ClearPieces();
            ClearHighlights();

            for (int r = 0; r < Board.Size; r++)
            {
                for (int c = 0; c < Board.Size; c++)
                {
                    var pos = new BoardPosition(r, c);
                    var piece = board.GetPiece(pos);
                    if (piece == PieceType.None) continue;

                    var prefab = GetPiecePrefab(piece);
                    if (prefab == null) continue;

                    var go = Instantiate(prefab, CellCenter(r, c), Quaternion.identity, transform);
                    _pieces[pos] = go;
                }
            }

            // Show selection and legal move highlights
            if (_gm.SelectedPosition.HasValue)
            {
                ShowHighlight(_gm.SelectedPosition.Value, _selectedColor);
                foreach (var move in _gm.GetMovesFromPosition(_gm.SelectedPosition.Value))
                    ShowHighlight(move.To, _moveColor);
            }
        }

        // ─── Highlights ───────────────────────────────────────────────────

        private void ShowHighlight(BoardPosition pos, Color color)
        {
            var go = new GameObject($"Highlight_{pos}");
            go.transform.SetParent(transform);
            go.transform.position = CellCenter(pos.Row, pos.Col) + Vector3.back * 0.1f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _lightTilePrefab.GetComponent<SpriteRenderer>().sprite;
            sr.color = color;
            sr.sortingOrder = 1;
            sr.transform.localScale = Vector3.one;

            _highlights.Add(go);
        }

        private void ClearHighlights()
        {
            foreach (var h in _highlights) Destroy(h);
            _highlights.Clear();
        }

        // ─── Helpers ─────────────────────────────────────────────────────

        private void ClearPieces()
        {
            foreach (var kvp in _pieces) Destroy(kvp.Value);
            _pieces.Clear();
        }

        private static Vector3 CellCenter(int row, int col) =>
            new Vector3(col + 0.5f, -(row + 0.5f), 0f);

        private GameObject GetPiecePrefab(PieceType type) => type switch
        {
            PieceType.White      => _whitePiecePrefab,
            PieceType.Black      => _blackPiecePrefab,
            PieceType.WhiteKing  => _whiteKingPrefab,
            PieceType.BlackKing  => _blackKingPrefab,
            _                    => null
        };

        /// <summary>Converts a world-space position to a BoardPosition.</summary>
        public static BoardPosition WorldToBoard(Vector3 world)
        {
            int col = Mathf.FloorToInt(world.x);
            int row = Mathf.FloorToInt(-world.y);
            return new BoardPosition(row, col);
        }
    }
}
