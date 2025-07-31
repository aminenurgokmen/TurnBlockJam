using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class Block : MonoBehaviour
{
    public List<BlockData> blockData = new List<BlockData>();
    private bool isRotating = false;
    private Collider blockCollider;

    void Start()
    {
        blockCollider = GetComponent<Collider>();

        GridManager.Instance.RegisterBlock(this);
        foreach (var item in blockData)
        {
            if (item.part != null)
            {
                var mat = GameScript.Instance.AssignMaterial(item.color);
                if (mat != null)
                    item.part.GetComponent<MeshRenderer>().material = mat;
            }
        }
    }

    public void SetColliderActive(bool active)
    {
        if (blockCollider != null)
            blockCollider.enabled = active;
    }

    void Update()
    {
        GridManager.Instance.UpdateColorGrid(this);
    }

    private void OnMouseDown()
    {
        if (!isRotating)
            StartCoroutine(RotateBlock());
    }

    private IEnumerator RotateBlock()
    {
        isRotating = true;

        float elapsed = 0f;
        Quaternion startRot = transform.rotation;
        Quaternion endRot = Quaternion.Euler(0, transform.eulerAngles.y + 90, 0);

        while (elapsed < 0.2f)
        {
            transform.rotation = Quaternion.Slerp(startRot, endRot, elapsed / 0.2f);
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.rotation = endRot;
        isRotating = false;

        GridManager.Instance.CheckForMatches();
    }

    public Vector2Int[] GetOccupiedGridPositions()
    {
        List<Vector2Int> occupiedPositions = new List<Vector2Int>();
        float cellSize = GridManager.Instance.cellSize;

        for (int i = 0; i < blockData.Count; i++)
        {
            var part = blockData[i].part;
            Vector3 worldPos = part.transform.position;

            int x = Mathf.FloorToInt(worldPos.x / cellSize);
            int z = Mathf.FloorToInt(worldPos.z / cellSize);

            occupiedPositions.Add(new Vector2Int(x, z));
        }
        return occupiedPositions.ToArray();
    }
}



[Serializable]
public class BlockData
{
    public ColorType color;
    public GameObject part;

    public BlockData(ColorType color, GameObject part)
    {
        this.color = color;
        this.part = part;
    }
}

public enum ColorType
{
    Red,
    Green,
    Blue,
    Yellow,
    Purple,
    Orange
}
