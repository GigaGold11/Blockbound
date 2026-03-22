using UnityEngine;

namespace Blockbound.UI
{
    public class GameSceneBootstrap : MonoBehaviour
    {
        private void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}