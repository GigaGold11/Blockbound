using Blockbound.Core;
using UnityEngine;

namespace Blockbound.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        public void PlayGame()
        {
            GameManager.Instance.LoadGame();
        }

        public void QuitGame()
        {
            GameManager.Instance.QuitGame();
        }
    }
}