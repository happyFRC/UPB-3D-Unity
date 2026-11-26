using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 沙盘/演化场景共用的轨道相机。
///
/// 沙盘模式：
/// - 锁定某个天体时，最近距离不小于该天体直径；不会钻进天体内部。
/// - WASD 进入自由视角：焦点沿摄像机水平前/后/左/右移动。
/// - 自由视角最近可到焦点上方 1km。
/// - WASD 速度 = 0.2 * 到上一个选中天体的距离（没有选中则用原点到摄像机的距离）。
/// </summary>
public class UPB3DCamera : MonoBehaviour {
    public Camera cam;
    public Transform target;

    [Header("缩放控制")]
    public float zoomSpeed = 0.5f;

    [Tooltip("最大缩放距离（会被 HardMaxZoomUnits 夹住）")]
    public float maxZoom = 1e10f;

    /// <summary>
    /// 相机离焦点的最大距离硬上限（Unity 单位）：100 亿。
    /// 所有场景统一；即使别的脚本（如 StarLightPoint）把 maxZoom 调大也不会超过它。
    /// </summary>
    public const float HardMaxZoomUnits = 1e10f;

    /// <summary>实际生效的最远距离：min(场景里配的 maxZoom, 统一硬上限)。</summary>
    private float EffectiveMaxZoom => Mathf.Min(maxZoom, HardMaxZoomUnits);

    /// <summary>把距离夹在 [最近, 最远] 之间，最远永远不超过 100 亿单位。</summary>
    private float ClampZoom(float distance) {
        float max = EffectiveMaxZoom;
        float min = Mathf.Min(minZoom, max);
        return Mathf.Clamp(distance, min, max);
    }

    [Tooltip("非沙盘模式：最小距离 = 目标大小 * 此倍数 + 额外加值")]
    public float targetMultiplier = 1.0f;

    [Tooltip("非沙盘模式：最小距离的额外加值")]
    public float minZoomOffset = 2f;

    private float minZoom = 0.002f;

    private bool _sandboxMode;
    private float _sandboxFreeMinUnits;
    private bool _following;
    private Vector3 _focusPoint;
    private Transform _movementReference;

    [Header("旋转控制")]
    public float rotateSpeed = 2f;

    private Vector3 _lastMousePos;
    private float _angleX = 0f;
    private float _angleY = 30f;

    /// <summary>俯仰上限：约 89 度（俯视 / 仰视各一边），共约 180 度，留 1 度避免 LookAt 极点奇异。</summary>
    private const float PitchLimit = 89f;
    private float _distance = 30f;
    private float _logDistance;

    public float distance => _distance;

    /// <summary>当前相机是否正在跟随指定目标。</summary>
    public bool IsFollowing(Transform candidate) =>
        _following && candidate != null && target == candidate;

    /// <summary>直接设置相机到焦点的距离（沙盘演示用）。</summary>
    public void SetDistance(float distance) {
        _distance = ClampZoom(Mathf.Max(distance, minZoom));
        _logDistance = Mathf.Log(Mathf.Max(_distance, 1e-6f));
        UpdateCameraPosition();
    }

    /// <summary>外部（例如物理步进/浮动原点重建）之后立即同步摄像机位置。</summary>
    public void SyncNow() {
        if (!_sandboxMode && target == null) return;
        if (_following && target != null) _focusPoint = target.position;
        UpdateCameraPosition();
    }

    /// <summary>把相机聚焦到某个目标，并设置距离（沙盘放置/跟随用）。</summary>
    public void Focus(Transform newTarget, float newDistance) {
        if (newTarget == null) return;
        target = newTarget;
        _focusPoint = newTarget.position;
        _following = true;

        float targetSize = Mathf.Abs(newTarget.localScale.x);
        minZoom = _sandboxMode
            ? Mathf.Max(targetSize, 1e-7f)
            : Mathf.Max(targetSize * targetMultiplier + minZoomOffset, 0.001f);

        _distance = ClampZoom(newDistance);
        _logDistance = Mathf.Log(_distance);
        UpdateCameraPosition();
    }

    /// <summary>沙盘专用：开启自由移动，并设置自由视角最近距离。</summary>
    public void EnableSandboxZoom(float minFreeDistanceKm = 1f) {
        _sandboxMode = true;
        _sandboxFreeMinUnits = (float)CelestialScale.KmToUnits(minFreeDistanceKm);
        if (target != null) {
            _focusPoint = target.position;
            _following = true;
        }
    }

    /// <summary>上一次选中的天体；没有选中传 null，WASD 速度改用原点距离。</summary>
    public void SetMovementReference(Transform reference) {
        _movementReference = reference;
    }

    private float ComputeMinZoom(float targetSize) {
        if (_sandboxMode) return Mathf.Max(targetSize, 1e-7f);
        return Mathf.Max(targetSize * targetMultiplier + minZoomOffset, 0.001f);
    }

    public float PlaneHeight(Plane plane) {
        return Mathf.Abs(plane.GetDistanceToPoint(transform.position));
    }

    /// <summary>沙盘放置：相机到平面的高度不足时才后退到指定高度；已经更高就不动。</summary>
    public void EnsurePlaneHeight(Plane plane, float minHeight, Transform focusTarget) {
        if (focusTarget == null || minHeight <= 0f) return;
        if (PlaneHeight(plane) >= minHeight) return;

        // 仰视时 sin 为负，取绝对值；接近水平时给个下限避免距离爆炸。
        float sin = Mathf.Abs(Mathf.Sin(Mathf.Clamp(_angleY, -PitchLimit, PitchLimit) * Mathf.Deg2Rad));
        float desired = minHeight / Mathf.Max(sin, 0.05f);
        Focus(focusTarget, desired);
    }

    private float _lastTouchDist = 0f;
    private Vector2 _lastTouchPos;
    private bool _wasTouching = false;

    private void Start() {
        if (cam == null) cam = GetComponent<Camera>();

        if (target == null) {
            GameObject go = GameObject.FindGameObjectWithTag("Player");
            if (go != null) target = go.transform;
        }

        _focusPoint = target != null ? target.position : Vector3.zero;
        _following = target != null;

        float initScale = target != null ? target.localScale.x : 1f;
        _distance = Mathf.Clamp(initScale * 3.0f, 0.1f, EffectiveMaxZoom);
        _logDistance = Mathf.Log(_distance);

        UpdateCameraPosition();
    }

    private void LateUpdate() {
        if (!_sandboxMode && target == null) return;

        float targetSize = target != null ? Mathf.Abs(target.transform.localScale.x) : 0f;

        if (_sandboxMode) {
            if (_following && target != null) {
                _focusPoint = target.position;
                minZoom = Mathf.Max(targetSize, 1e-7f);
            } else {
                minZoom = _sandboxFreeMinUnits;
            }

            HandleSandboxFreeMovement();

            // WASD 可能刚从“锁定天体”切换到自由视角，重新取一次下限。
            if (_following && target != null) {
                _focusPoint = target.position;
                minZoom = Mathf.Max(targetSize, 1e-7f);
            } else {
                minZoom = _sandboxFreeMinUnits;
            }
        } else {
            minZoom = ComputeMinZoom(targetSize);
        }

        if (minZoom < 1e-7f) minZoom = 1e-7f;
        _distance = ClampZoom(_distance);

        if (Application.isMobilePlatform) {
            HandleTouch();
        } else {
            HandleMouse();
        }

        UpdateCameraPosition();
    }

    private void HandleSandboxFreeMovement() {
        if (!_sandboxMode || IsTypingInInput()) return;

        float horizontal = 0f;
        float vertical = 0f;
        if (Input.GetKey(KeyCode.D)) horizontal += 1f;
        if (Input.GetKey(KeyCode.A)) horizontal -= 1f;
        if (Input.GetKey(KeyCode.W)) vertical += 1f;
        if (Input.GetKey(KeyCode.S)) vertical -= 1f;
        if (Mathf.Approximately(horizontal, 0f) && Mathf.Approximately(vertical, 0f)) {
            return;
        }

        if (_following) DetachToFreeFocus();

        // W/S 沿摄像机真正面对的方向移动，不再压到水平面。
        Vector3 forward = transform.forward;
        if (forward.sqrMagnitude < 1e-8f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 right = transform.right;
        if (right.sqrMagnitude < 1e-8f) right = Vector3.right;
        right.Normalize();

        Vector3 direction = forward * vertical + right * horizontal;
        if (direction.sqrMagnitude < 1e-8f) return;
        direction.Normalize();

        float referenceDistance = _movementReference != null
            ? Vector3.Distance(transform.position, _movementReference.position)
            : transform.position.magnitude;

        float speed = 0.2f * Mathf.Max(0f, referenceDistance);
        _focusPoint += direction * speed * Time.unscaledDeltaTime;
    }

    /// <summary>从锁定天体切到自由视角时，把焦点放到朝向相机的那一侧表面。</summary>
    private void DetachToFreeFocus() {
        if (target != null) {
            float radius = Mathf.Abs(target.localScale.x) * 0.5f;
            Vector3 away = transform.position - target.position;
            if (away.sqrMagnitude < 1e-10f) away = transform.forward;
            away.Normalize();

            _focusPoint = target.position + away * radius;
            _distance = Mathf.Max(_sandboxFreeMinUnits, _distance - radius);
            _logDistance = Mathf.Log(_distance);
        }

        _following = false;
    }

    private static bool IsTypingInInput() {
        if (EventSystem.current == null) return false;
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return false;
        return selected.GetComponentInParent<TMP_InputField>() != null;
    }

    private void HandleMouse() {
        // 指针在 UI（包括参数面板）上时，滚轮只用于 UI，不缩放世界。
        bool overUi = EventSystem.current != null &&
                      EventSystem.current.IsPointerOverGameObject();
        if (!overUi) {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f) {
                _logDistance -= scroll * zoomSpeed;
                ClampDistance();
            }
        }

        if (Input.GetMouseButtonDown(0)) {
            _lastMousePos = Input.mousePosition;
        } else if (Input.GetMouseButton(0)) {
            Vector3 delta = Input.mousePosition - _lastMousePos;
            _angleX += delta.x * rotateSpeed * 0.1f;
            _angleY -= delta.y * rotateSpeed * 0.1f;
            _angleY = Mathf.Clamp(_angleY, -PitchLimit, PitchLimit);
            _lastMousePos = Input.mousePosition;
        }
    }

    private void HandleTouch() {
        if (Input.touchCount == 2) {
            Touch t0 = Input.GetTouch(0);
            Touch t1 = Input.GetTouch(1);
            float currDist = Vector2.Distance(t0.position, t1.position);

            if (!_wasTouching) {
                _lastTouchDist = currDist;
                _wasTouching = true;
            } else {
                float delta = currDist - _lastTouchDist;
                if (Mathf.Abs(delta) > 0.01f) {
                    _logDistance -= delta * zoomSpeed * 0.01f;
                    ClampDistance();
                }
                _lastTouchDist = currDist;
            }
        } else if (Input.touchCount == 1) {
            Touch t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began) {
                _lastTouchPos = t.position;
            } else if (t.phase == TouchPhase.Moved) {
                Vector2 delta = t.position - _lastTouchPos;
                _angleX += delta.x * rotateSpeed * 0.1f;
                _angleY -= delta.y * rotateSpeed * 0.1f;
                _angleY = Mathf.Clamp(_angleY, -PitchLimit, PitchLimit);
                _lastTouchPos = t.position;
            }
        } else {
            _wasTouching = false;
        }
    }

    private void ClampDistance() {
        float newDistance = Mathf.Exp(_logDistance);
        float max = EffectiveMaxZoom;
        if (newDistance < minZoom) {
            newDistance = Mathf.Min(minZoom, max);
            _logDistance = Mathf.Log(newDistance);
        } else if (newDistance > max) {
            newDistance = max;
            _logDistance = Mathf.Log(max);
        }
        _distance = newDistance;
    }

    private void UpdateCameraPosition() {
        if (!_sandboxMode && target == null) return;

        Vector3 focus = _sandboxMode
            ? _focusPoint
            : (target != null ? target.position : _focusPoint);

        Quaternion rotation = Quaternion.Euler(_angleY, _angleX, 0f);
        Vector3 offset = rotation * new Vector3(0f, 0f, -_distance);
        transform.position = focus + offset;
        transform.LookAt(focus);

        if (cam != null) {
            // 远裁剪面随距离扩展
            float requiredFarPlane = _distance + 10000f;
            if (cam.farClipPlane < requiredFarPlane) cam.farClipPlane = requiredFarPlane;

            // 沙盘自由视角可以到 1km 级别，近裁剪面需要降到米级。
            float desiredNear = _sandboxMode
                ? Mathf.Clamp(_distance * 0.00001f, 1e-7f, 1000f)
                : Mathf.Clamp(_distance * 0.001f, 0.01f, 1000f);
            cam.nearClipPlane = desiredNear;
        }
    }
}
