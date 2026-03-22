using System.Collections.Generic;
using UnityEngine;

namespace Blockbound.Player
{
    public class CreativeHotbar : MonoBehaviour
    {
        [SerializeField] private List<ushort> slots = new List<ushort> { 1, 2, 3 };
        [SerializeField] private int selectedIndex = 0;

        public ushort SelectedBlockId
        {
            get
            {
                if (slots.Count == 0) return 1;
                selectedIndex = Mathf.Clamp(selectedIndex, 0, slots.Count - 1);
                return slots[selectedIndex];
            }
        }

        private void Update()
        {
            HandleNumberKeys();
            HandleScrollWheel();
        }

        private void HandleNumberKeys()
        {
            if (Input.GetKeyDown(KeyCode.Alpha1)) selectedIndex = 0;
            if (Input.GetKeyDown(KeyCode.Alpha2) && slots.Count > 1) selectedIndex = 1;
            if (Input.GetKeyDown(KeyCode.Alpha3) && slots.Count > 2) selectedIndex = 2;
            if (Input.GetKeyDown(KeyCode.Alpha4) && slots.Count > 3) selectedIndex = 3;
            if (Input.GetKeyDown(KeyCode.Alpha5) && slots.Count > 4) selectedIndex = 4;
            if (Input.GetKeyDown(KeyCode.Alpha6) && slots.Count > 5) selectedIndex = 5;
            if (Input.GetKeyDown(KeyCode.Alpha7) && slots.Count > 6) selectedIndex = 6;
            if (Input.GetKeyDown(KeyCode.Alpha8) && slots.Count > 7) selectedIndex = 7;
            if (Input.GetKeyDown(KeyCode.Alpha9) && slots.Count > 8) selectedIndex = 8;
        }

        private void HandleScrollWheel()
        {
            float scroll = Input.mouseScrollDelta.y;

            if (scroll > 0f)
            {
                selectedIndex--;
                if (selectedIndex < 0) selectedIndex = slots.Count - 1;
            }
            else if (scroll < 0f)
            {
                selectedIndex++;
                if (selectedIndex >= slots.Count) selectedIndex = 0;
            }
        }
    }
}