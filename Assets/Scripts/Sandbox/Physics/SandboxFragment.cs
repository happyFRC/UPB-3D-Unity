using UnityEngine;

/// <summary>碰撞生成的碎片；只负责把物理层的位置同步到渲染 Transform。</summary>
public class SandboxFragment : MonoBehaviour {
    public SandboxPhysicsBody body;
    public bool isMist;
    public Material fragmentMaterial;

    public double logicalX;
    public double logicalY;
    public double logicalZ;

    /// <summary>真实基准单位下的直径（未乘世界缩放）。</summary>
    public double visualDiameter = 1.0;
    private double _appliedWorldScale = -1.0;

    public void SetLogicalPosition(double x, double y, double z) {
        logicalX = x;
        logicalY = y;
        logicalZ = z;
        ApplyRenderPosition();
    }

    public void ApplyRenderPosition() {
        transform.position = SandboxFloatingOrigin.ToRender(logicalX, logicalY, logicalZ);
        _appliedWorldScale = SandboxWorldScale.Scale;
        float diameter = (float)(visualDiameter * _appliedWorldScale);
        if (diameter < 1e-20f) diameter = 1e-20f;
        transform.localScale = new Vector3(diameter, diameter, diameter);
    }

    private void LateUpdate() {
        if (System.Math.Abs(_appliedWorldScale - SandboxWorldScale.Scale) > 0.0) {
            ApplyRenderPosition();
        }
    }

    public void ApplyThermalState(double temperatureK) {
        if (!isMist || fragmentMaterial == null) return;
        var (r, g, b) = Spectrum.GetBlackbodyColor(temperatureK);
        fragmentMaterial.SetColor("_Color", new Color(r, g, b, 1f));
    }

    private void OnDestroy() {
        if (fragmentMaterial != null) Destroy(fragmentMaterial);
    }
}
