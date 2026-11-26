using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class UILineRenderer : Graphic {
    public List<Vector2> points = new();
    public float lineThickness = 2f;
    public bool useGradient = false;
    public List<Color> pointColors = new();

    protected override void OnPopulateMesh(VertexHelper vh) {
        vh.Clear();

        if (points.Count < 2)
            return;

        for (int i = 0; i < points.Count - 1; i++) {
            Vector2 p1 = points[i];
            Vector2 p2 = points[i + 1];

            Color c1 = useGradient && i < pointColors.Count ? pointColors[i] : color;
            Color c2 = useGradient && i + 1 < pointColors.Count ? pointColors[i + 1] : color;

            DrawLine(vh, p1, p2, c1, c2);
        }
    }

    void DrawLine(VertexHelper vh, Vector2 p1, Vector2 p2, Color c1, Color c2) {
        Vector2 dir = (p2 - p1).normalized;
        Vector2 perp = 0.5f * lineThickness * new Vector2(-dir.y, dir.x);

        UIVertex[] verts = new UIVertex[4];
        verts[0].position = p1 + perp;
        verts[1].position = p1 - perp;
        verts[2].position = p2 - perp;
        verts[3].position = p2 + perp;

        verts[0].color = c1;
        verts[1].color = c1;
        verts[2].color = c2;
        verts[3].color = c2;

        vh.AddUIVertexQuad(verts);
    }
}