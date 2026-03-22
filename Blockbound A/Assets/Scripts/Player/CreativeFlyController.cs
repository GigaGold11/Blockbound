using UnityEngine;

namespace Blockbound.Player
{
    public class CreativeFlyController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraTransform;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float sprintMultiplier = 2.5f;
        [SerializeField] private float verticalSpeed = 10f;
        [SerializeField] private float lookSensitivity = 2f;

        private float pitch;

        private void Start()
        {
            LockCursor(true);

            if (cameraTransform == null)
                cameraTransform = Camera.main != null ? Camera.main.transform : null;
        }

        private void Update()
        {
            HandleMouseLook();
            HandleMovement();

            if (Input.GetKeyDown(KeyCode.Escape))
                LockCursor(false);

            if (Input.GetMouseButtonDown(0))
                LockCursor(true);
        }

        private void HandleMouseLook()
        {
            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            float mouseX = Input.GetAxisRaw("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxisRaw("Mouse Y") * lookSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -89f, 89f);

            if (cameraTransform != null)
            {
                cameraTransform.localEulerAngles = new Vector3(pitch, 0f, 0f);
            }
        }

        private void HandleMovement()
        {
            float speed = moveSpeed;
            if (Input.GetKey(KeyCode.LeftShift))
                speed *= sprintMultiplier;

            Vector3 move = Vector3.zero;

            move += transform.forward * Input.GetAxisRaw("Vertical");
            move += transform.right * Input.GetAxisRaw("Horizontal");

            if (Input.GetKey(KeyCode.Space))
                move += Vector3.up * (verticalSpeed / moveSpeed);

            if (Input.GetKey(KeyCode.LeftControl))
                move += Vector3.down * (verticalSpeed / moveSpeed);

            if (move.sqrMagnitude > 1f)
                move.Normalize();

            transform.position += move * speed * Time.deltaTime;
        }

        private void LockCursor(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}