using UnityEngine;

public class UPB3DCamera : MonoBehaviour {
    public Camera cam;
    public Transform target;

    public float zoomSpeed = 10f;
    public float minZoom = 20f;
    public float maxZoom = 100f;

    public float rotateSpeed = 2f;
    private Vector3 _lastMousePos;
    private float _angleX = 0f;
    private float _angleY = 30f;
    private float _distance = 30f;

    void Start() {
        if (target == null) {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }
        UpdateCameraPosition();
    }

    void Update() {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll != 0) {
            _distance -= scroll * zoomSpeed;
            _distance = Mathf.Clamp(_distance, minZoom, maxZoom);
            UpdateCameraPosition();
        }

        if (Input.GetMouseButtonDown(0)) {
            _lastMousePos = Input.mousePosition;
        } else if (Input.GetMouseButton(0)) {
            Vector3 delta = Input.mousePosition - _lastMousePos;
            _angleX += delta.x * rotateSpeed * 0.1f;
            _angleY -= delta.y * rotateSpeed * 0.1f;
            _angleY = Mathf.Clamp(_angleY, 5f, 85f);
            UpdateCameraPosition();
            _lastMousePos = Input.mousePosition;
        }
    }

    void UpdateCameraPosition() {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(_angleY, _angleX, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -_distance);
        transform.position = target.position + offset;
        transform.LookAt(target);
    }
}
