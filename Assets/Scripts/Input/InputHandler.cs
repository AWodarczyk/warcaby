using UnityEngine;
using Warcaby.Core;
using Warcaby.UI;

namespace Warcaby.Input
{
    /// <summary>
    /// Reads mouse input, converts screen coordinates to board positions,
    /// and forwards clicks to GameManager.
    /// Attach to the same GameObject as BoardRenderer (or a Camera child).
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class InputHandler : MonoBehaviour
    {
        private Camera _cam;
        private Network.NetworkPlayer _localNetworkPlayer;

        private void Awake() => _cam = GetComponent<Camera>();

        private void Start()
        {
            // Cache the local network player if in online mode
            _localNetworkPlayer = FindLocalNetworkPlayer();
        }

        private void Update()
        {
            if (!UnityEngine.Input.GetMouseButtonDown(0)) return;
            if (GameManager.Instance == null) return;
            if (GameManager.Instance.Result != GameResult.InProgress) return;

            var worldPos = _cam.ScreenToWorldPoint(UnityEngine.Input.mousePosition);
            worldPos.z = 0;
            var boardPos = BoardRenderer.WorldToBoard(worldPos);

            if (!Board.IsInBounds(boardPos)) return;

            HandleBoardClick(boardPos);
        }

        private void HandleBoardClick(BoardPosition pos)
        {
            var gm = GameManager.Instance;

            if (gm.Mode == GameMode.OnlineMultiplayer)
            {
                // In online mode, send move command via network if it's our turn
                if (_localNetworkPlayer == null || !_localNetworkPlayer.IsMyTurn) return;

                var selected = gm.SelectedPosition;
                if (!selected.HasValue)
                {
                    gm.OnSquareClicked(pos); // select piece locally for highlights
                    return;
                }

                // Send move to server
                _localNetworkPlayer.CmdSendMove(selected.Value.Row, selected.Value.Col,
                                                pos.Row, pos.Col);
                gm.OnSquareClicked(pos); // clears selection locally
            }
            else
            {
                gm.OnSquareClicked(pos);
            }
        }

        private Network.NetworkPlayer FindLocalNetworkPlayer()
        {
            foreach (var player in FindObjectsOfType<Network.NetworkPlayer>())
                if (player.isLocalPlayer) return player;
            return null;
        }
    }
}
