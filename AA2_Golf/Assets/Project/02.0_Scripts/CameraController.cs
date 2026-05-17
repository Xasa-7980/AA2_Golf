using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset = new Vector3(0f, 5f, -7f);
    [SerializeField] private float smoothSpeed = 5f;

    [Header("Free Roam")]
    [SerializeField] private float freeMoveSpeed = 10f;
    [SerializeField] private float freeLookSpeed = 2f;
    [SerializeField] private Vector3 freeRoamMinBounds = new Vector3(-50f, 1f, -50f);
    [SerializeField] private Vector3 freeRoamMaxBounds = new Vector3(50f, 30f, 50f);

    private bool isFreeRoam = false;
    private Vector3 savedPosition;
    private Quaternion savedRotation;
    private float yaw = 0f;
    private float pitch = 0f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
            ToggleFreeRoam();

        if (isFreeRoam)
            HandleFreeRoam();
    }

    void LateUpdate()
    {
        if (isFreeRoam || target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.LookAt(target);
    }

    private void ToggleFreeRoam()
    {
        isFreeRoam = !isFreeRoam;

        if (isFreeRoam)
        {
            savedPosition = transform.position;
            savedRotation = transform.rotation;

            // Initialise yaw from current rotation so the camera doesn't snap
            yaw = transform.eulerAngles.y;
            pitch = transform.eulerAngles.x;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = true;
        }
        else
        {
            // Return to follow mode
            if (target != null)
            {
                transform.position = target.position + offset;
            }
            transform.rotation = savedRotation;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    private void HandleFreeRoam()
    {
        // Mouse look
        yaw   += Input.GetAxis("Mouse X") * freeLookSpeed;
        pitch -= Input.GetAxis("Mouse Y") * freeLookSpeed;
        pitch  = Mathf.Clamp(pitch, -80f, 80f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // WASD movement
        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;

        Vector3 newPos = transform.position + move.normalized * freeMoveSpeed * Time.deltaTime;

        // Apply position constraints
        newPos.x = Mathf.Clamp(newPos.x, freeRoamMinBounds.x, freeRoamMaxBounds.x);
        newPos.y = Mathf.Clamp(newPos.y, freeRoamMinBounds.y, freeRoamMaxBounds.y);
        newPos.z = Mathf.Clamp(newPos.z, freeRoamMinBounds.z, freeRoamMaxBounds.z);

        transform.position = newPos;
    }
}