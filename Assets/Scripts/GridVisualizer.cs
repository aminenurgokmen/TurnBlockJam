using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class GridVisualizer : MonoBehaviour
{
     public int width = 5;
    public int height = 5;
    public float cellSize = 1f;
    Color gridColor = Color.yellow;
    public Color labelColor = Color.black;
    public Vector3 labelOffset = new Vector3(0.5f, 0.1f, 0.5f);

    private void OnDrawGizmos()
    {
        if (GridManager.Instance != null)
        {
            width = GridManager.Instance.width;
            height = GridManager.Instance.height;
        }

        Gizmos.color = gridColor;

        for (int x = 0; x <= width; x++)
        {
            Vector3 from = transform.position + new Vector3(x * cellSize, 0f, 0f);
            Vector3 to = transform.position + new Vector3(x * cellSize, 0f, height * cellSize);
            Gizmos.DrawLine(from, to);
        }

        for (int y = 0; y <= height; y++)
        {
            Vector3 from = transform.position + new Vector3(0f, 0f, y * cellSize);
            Vector3 to = transform.position + new Vector3(width * cellSize, 0f, y * cellSize);
            Gizmos.DrawLine(from, to);
        }
        GUIStyle style = new GUIStyle();
        style.normal.textColor = labelColor;
        style.fontSize = 12;
        style.alignment = TextAnchor.MiddleCenter;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 worldPos = transform.position + new Vector3(x * cellSize, 0f, y * cellSize) + labelOffset;
                Handles.Label(worldPos, $"({x},{y})", style);
            }
        }
    }

}
