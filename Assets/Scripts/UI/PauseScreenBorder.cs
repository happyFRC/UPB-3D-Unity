using UnityEngine;

public class PauseScreenBorder : MonoBehaviour {
    [Header("线框颜色")]
    public Color borderColor = new Color(1f, 0.2f, 0.2f, 0.8f); // 红色半透明

    [Header("线框粗细 (像素)")]
    public int borderThickness = 4;

    [Header("边缘留白 (像素)")]
    public int borderOffset = 2;

    [Header("呼吸闪烁速度")]
    public float pulseSpeed = 2f;

    private bool _isVisible = false;

    public void SetVisible(bool visible) {
        _isVisible = visible;
    }

    void OnGUI() {
        if (!_isVisible) return;

        // 呼吸效果：让透明度在 0.4 到 1.0 之间波动
        float alpha = Mathf.Lerp(0.4f, 1f, (Mathf.Sin(Time.unscaledTime * pulseSpeed) + 1f) / 2f);
        Color c = borderColor;
        c.a *= alpha;

        int w = Screen.width;
        int h = Screen.height;
        int t = borderThickness;

        Texture2D tex = Texture2D.whiteTexture;

        // 设置颜色并绘制四条边
        GUI.color = c;

        // 上
        GUI.DrawTexture(new Rect(0, 0, w, t), tex);
        // 下
        GUI.DrawTexture(new Rect(0, h - t, w, t), tex);
        // 左
        GUI.DrawTexture(new Rect(0, 0, t, h), tex);
        // 右
        GUI.DrawTexture(new Rect(w - t, 0, t, h), tex);

        GUI.color = Color.white; // 恢复
    }
}