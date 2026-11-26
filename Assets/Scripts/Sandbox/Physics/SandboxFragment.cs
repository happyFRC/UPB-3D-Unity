using UnityEngine;

/// <summary>碰撞生成的碎片；只负责把物理层的位置同步到渲染 Transform。</summary>
public class SandboxFragment : MonoBehaviour {
    public SandboxPhysicsBody body;
    public bool isMist;
    public Material fragmentMaterial;

    public double logicalX;
    public double logicalY;
    public double logicalZ;

    public void SetLogicalPosition(double x, double y, double z) {
        logicalX = x;
        logicalY = y;
        logicalZ = z;
        ApplyRenderPosition();
    }

    public void ApplyRenderPosition() {
        transform.position = SandboxFloatingOrigin.ToRender(logicalX, logicalY, logicalZ);
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
