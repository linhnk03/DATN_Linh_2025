using UnityEngine;
public class CameraController : MonoBehaviour
{
    public float zoomSpeed = 5f; // Tốc độ phóng to/thu nhỏ
    public float rotationSpeed = 100f; // Tốc độ xoay
    public float moveSpeed = 5f; // Tốc độ di chuyển
    public float minZoom = 10f; // Zoom tối thiểu
    public float maxZoom = 100f; // Zoom tối đa

    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }

    void Update()
    {
        // Xử lý phóng to/thu nhỏ
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        if (scrollInput != 0)
        {
            float newZoom = cam.fieldOfView - scrollInput * zoomSpeed;
            cam.fieldOfView = Mathf.Clamp(newZoom, minZoom, maxZoom);
        }

        // Xử lý xoay camera
        if (Input.GetMouseButton(0)) // Nếu giữ nút chuột trái
        {
            float rotationX = Input.GetAxis("Mouse X") * rotationSpeed * Time.deltaTime;
            float rotationY = Input.GetAxis("Mouse Y") * rotationSpeed * Time.deltaTime;

            cam.transform.Rotate(-rotationY, rotationX, 0);
        }

        // Xử lý di chuyển camera
        if (Input.GetMouseButton(1)) // Nếu giữ nút chuột phải
        {
            float moveX = Input.GetAxis("Mouse X") * moveSpeed * Time.deltaTime;
            float moveY = Input.GetAxis("Mouse Y") * moveSpeed * Time.deltaTime;

            cam.transform.position += new Vector3(moveX, moveY, 0);
        }
    }
}