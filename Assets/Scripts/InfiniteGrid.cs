using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class InfiniteGrid : MonoBehaviour {
    public float gridSize = 1f;
    public int gridExtent = 2000;
    public Color gridColor = new(0.2f, 0.5f, 1f, 0.4f);

    private Camera _mainCamera;
    private Mesh _mesh;
    private MeshRenderer _renderer;
    private bool _isVisible = true;

    void Start() {
        _mainCamera = Camera.main;
        _renderer = GetComponent<MeshRenderer>();
        transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

        if (_renderer.sharedMaterial == null) {
            _renderer.material = new Material(Shader.Find("Sprites/Default"));
        }

        CreateGridMesh();
    }

    void LateUpdate() {
        if (_mainCamera == null) return;

        if (Input.GetKeyDown(KeyCode.G)) {
            if (_isVisible) {
                // FloatingText.Show("网格已关闭");
            } else {
                // FloatingText.Show("网格已开启");
            }
            // 绷不住了没有字体
            _isVisible = !_isVisible;
            _renderer.enabled = _isVisible;
        }

        if (!_isVisible) return;

        float distance = Vector3.Distance(_mainCamera.transform.position, transform.position);
        float scale = distance * 0.1f;
        scale = Mathf.Max(scale, 1f);
        transform.localScale = new Vector3(scale, 1f, scale);
    }

    void CreateGridMesh() {
        _mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = _mesh;

        int totalLines = (gridExtent * 2 + 1) * 2;
        int vertexCount = totalLines * 2;

        Vector3[] vertices = new Vector3[vertexCount];
        Color[] colors = new Color[vertexCount];
        int[] indices = new int[vertexCount];

        int index = 0;

        for (int z = -gridExtent; z <= gridExtent; z++) {
            vertices[index] = new Vector3(-gridExtent * gridSize, 0, z * gridSize);
            vertices[index + 1] = new Vector3(gridExtent * gridSize, 0, z * gridSize);

            colors[index] = gridColor;
            colors[index + 1] = gridColor;

            indices[index] = index;
            indices[index + 1] = index + 1;

            index += 2;
        }

        for (int x = -gridExtent; x <= gridExtent; x++) {
            vertices[index] = new Vector3(x * gridSize, 0, -gridExtent * gridSize);
            vertices[index + 1] = new Vector3(x * gridSize, 0, gridExtent * gridSize);

            colors[index] = gridColor;
            colors[index + 1] = gridColor;

            indices[index] = index;
            indices[index + 1] = index + 1;

            index += 2;
        }

        _mesh.vertices = vertices;
        _mesh.colors = colors;
        _mesh.SetIndices(indices, MeshTopology.Lines, 0);
    }
}