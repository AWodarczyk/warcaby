using Mirror;
using UnityEngine;
using Warcaby.Core;

namespace Warcaby.Network
{
    /// <summary>
    /// Represents a remote player's identity. Spawned by Mirror for each connection.
    /// </summary>
    public class NetworkPlayer : NetworkBehaviour
    {
        [SyncVar]
        public PlayerColor Color;

        [SyncVar]
        public string PlayerName = "Gracz";

        public bool IsMyTurn => GameManager.Instance != null &&
                                GameManager.Instance.CurrentPlayer == Color;

        public void AssignColor(PlayerColor color) => Color = color;

        public override void OnStartLocalPlayer()
        {
            CmdSetName($"Gracz_{Color}");
        }

        [Command]
        private void CmdSetName(string name) => PlayerName = name;

        /// <summary>Called by InputHandler – sends move to server.</summary>
        [Command]
        public void CmdSendMove(int fromRow, int fromCol, int toRow, int toCol)
        {
            var networkGM = FindObjectOfType<NetworkGameManager>();
            networkGM?.ServerReceiveMove(this, fromRow, fromCol, toRow, toCol);
        }
    }
}
