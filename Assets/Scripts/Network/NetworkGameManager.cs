using Mirror;
using UnityEngine;
using Warcaby.Core;

namespace Warcaby.Network
{
    /// <summary>
    /// Server-authoritative game manager for online multiplayer.
    /// Spawned on the server when both players are connected.
    /// </summary>
    public class NetworkGameManager : NetworkBehaviour
    {
        // Board state is only kept on server; clients receive RPC updates
        private Board _serverBoard;
        private PlayerColor _currentPlayer = PlayerColor.White;
        private Core.GameResult _result = Core.GameResult.InProgress;

        public override void OnStartServer()
        {
            _serverBoard = Board.CreateInitial();
            _currentPlayer = PlayerColor.White;
            RpcSyncBoard(SerializeBoard(_serverBoard), _currentPlayer);
        }

        // ─── Server-side move validation ──────────────────────────────────

        public void ServerReceiveMove(NetworkPlayer sender,
            int fromRow, int fromCol, int toRow, int toCol)
        {
            if (!isServer) return;
            if (sender.Color != _currentPlayer) return;  // not their turn
            if (_result != Core.GameResult.InProgress) return;

            var from = new BoardPosition(fromRow, fromCol);
            var to = new BoardPosition(toRow, toCol);

            var legalMoves = GameRules.GetLegalMoves(_serverBoard, _currentPlayer);
            Move matched = null;
            foreach (var m in legalMoves)
                if (m.From == from && m.To == to) { matched = m; break; }

            if (matched == null) return; // illegal move: ignore

            _serverBoard.ApplyMove(matched);
            _result = GameRules.GetResult(_serverBoard, _currentPlayer.Opponent());
            _currentPlayer = _currentPlayer.Opponent();

            RpcSyncBoard(SerializeBoard(_serverBoard), _currentPlayer);

            if (_result != Core.GameResult.InProgress)
                RpcGameOver(_result);
        }

        // ─── Client RPCs ──────────────────────────────────────────────────

        [ClientRpc]
        private void RpcSyncBoard(byte[] boardData, PlayerColor currentPlayer)
        {
            var board = DeserializeBoard(boardData);
            if (GameManager.Instance != null)
            {
                // Update local GameManager state (read-only; moves go through Cmd)
                GameManager.Instance.Board.ApplyMove(null); // just trigger refresh via external method
                // In a real integration, GameManager would expose a method like SetBoardFromNetwork
            }
        }

        [ClientRpc]
        private void RpcGameOver(Core.GameResult result)
        {
            GameManager.Instance?.OnGameOver(result); // expose via public event
        }

        // ─── Serialization ────────────────────────────────────────────────

        private static byte[] SerializeBoard(Board board)
        {
            var bytes = new byte[Board.Size * Board.Size];
            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                    bytes[r * Board.Size + c] = (byte)board.GetPiece(r, c);
            return bytes;
        }

        private static Board DeserializeBoard(byte[] bytes)
        {
            var board = new Board();
            for (int r = 0; r < Board.Size; r++)
                for (int c = 0; c < Board.Size; c++)
                    board.SetPiece(r, c, (PieceType)bytes[r * Board.Size + c]);
            return board;
        }
    }
}
