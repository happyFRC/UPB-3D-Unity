using UnityEngine;

public class UPB3DCamera : MonoBehaviour {
    public Camera cam;
    public Transform target;

    public float zoomSpeed = 0.5f;
    public float minZoom = 0.002f;
    public float maxZoom = 100000f;

    public float rotateSpeed = 2f;
    private Vector3 _lastMousePos;
    private float _angleX = 0f;
    private float _angleY = 30f;
    private float _distance = 30f;
    private float _logDistance;

    void Start() {
        if (target == null) {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }
        _logDistance = Mathf.Log(_distance);
        UpdateCameraPosition();
    }

    void Update() {
        minZoom = target.transform.localScale.x + 2;
        _distance = Mathf.Clamp(_distance, minZoom, maxZoom);
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f) {
            _logDistance -= scroll * zoomSpeed;
            float newDistance = Mathf.Exp(_logDistance);
            if (newDistance < minZoom) {
                newDistance = minZoom;
                _logDistance = Mathf.Log(minZoom);
            } else if (newDistance > maxZoom) {
                newDistance = maxZoom;
                _logDistance = Mathf.Log(maxZoom);
            }
            _distance = newDistance;
        }

        if (Input.GetMouseButtonDown(0)) {
            _lastMousePos = Input.mousePosition;
        } else if (Input.GetMouseButton(0)) {
            Vector3 delta = Input.mousePosition - _lastMousePos;
            _angleX += delta.x * rotateSpeed * 0.1f;
            _angleY -= delta.y * rotateSpeed * 0.1f;
            _angleY = Mathf.Clamp(_angleY, 5f, 85f);
            _lastMousePos = Input.mousePosition;
        }
        UpdateCameraPosition();
    }

    void UpdateCameraPosition() {
        if (target == null) return;

        Quaternion rotation = Quaternion.Euler(_angleY, _angleX, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -_distance);
        transform.position = target.position + offset;
        transform.LookAt(target);
    }
}