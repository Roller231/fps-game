using UnityEngine;

[DefaultExecutionOrder(1000)]
public class SimpleFreeLook : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 10f;
    [SerializeField] private float fastMultiplier = 2f;
    [SerializeField] private float lookSensitivity = 2f;
    [SerializeField] private float minPitch = -85f;
    [SerializeField] private float maxPitch = 85f;

    private float pitch;
    private float yaw;

    private void OnEnable()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        var euler = transform.rotation.eulerAngles;
        yaw = euler.y;
        pitch = euler.x;
    }

    private void Update()
    {
        float mx = Input.GetAxis("Mouse X") * lookSensitivity;
        float my = Input.GetAxis("Mouse Y") * lookSensitivity;

        yaw += mx;
        pitch = Mathf.Clamp(pitch - my, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        float up = 0f;
        if (Input.GetKey(KeyCode.E)) up += 1f;
        if (Input.GetKey(KeyCode.Q)) up -= 1f;

        Vector3 dir = (transform.right * h + transform.forward * v + transform.up * up).normalized;
        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? fastMultiplier : 1f);
        transform.position += dir * speed * Time.deltaTime;
    }
}
