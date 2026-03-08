using UnityEngine;

namespace Warcaby
{
    /// <summary>
    /// Placed in the Game scene. Reads GameSettings (set by MainMenuManager)
    /// and starts the game with the correct mode and options.
    /// Attach to the same GameObject as GameManager, or any persistent GO.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        private void Start()
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Debug.LogError("[GameBootstrap] GameManager.Instance is null!");
                return;
            }

            gm.StartGame(GameSettings.Mode, GameSettings.HumanColor);
        }
    }
}
