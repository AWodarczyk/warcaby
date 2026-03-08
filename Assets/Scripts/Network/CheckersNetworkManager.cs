// Requires Mirror package: https://mirror-networking.gitbook.io/docs/
// Install via Package Manager: com.mirror-networking.mirror
// Or from OpenUPM: openupm add com.mirror-networking.mirror

using Mirror;
using UnityEngine;
using Warcaby.Core;

namespace Warcaby.Network
{
    /// <summary>
    /// Extends Mirror's NetworkManager to handle checkers lobby logic.
    /// Replace the default NetworkManager component with this.
    /// </summary>
    public class CheckersNetworkManager : NetworkManager
    {
        public static CheckersNetworkManager NetInstance =>
            singleton as CheckersNetworkManager;

        [Header("Checkers")]
        [SerializeField] private GameObject _networkGameManagerPrefab;

        public override void OnServerAddPlayer(NetworkConnectionToClient conn)
        {
            base.OnServerAddPlayer(conn);

            // Assign color: first player = White, second = Black
            var player = conn.identity.GetComponent<NetworkPlayer>();
            if (player != null)
            {
                int playerIndex = numPlayers - 1;
                player.AssignColor(playerIndex == 0 ? PlayerColor.White : PlayerColor.Black);
            }

            // Start game when 2 players connected
            if (numPlayers == 2)
            {
                var gm = Instantiate(_networkGameManagerPrefab);
                NetworkServer.Spawn(gm);
            }
        }

        public override void OnServerDisconnect(NetworkConnectionToClient conn)
        {
            base.OnServerDisconnect(conn);
            // Delegate RPC to NetworkGameManager (a proper NetworkBehaviour)
            var ngm = FindObjectOfType<NetworkGameManager>();
            ngm?.NotifyOpponentDisconnected();
        }
    }
}
