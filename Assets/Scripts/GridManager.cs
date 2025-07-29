using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    public int width = 5;
    public int height = 5;
    public float cellSize = 1f;

    private Dictionary<Vector2Int, ColorType> colorGrid = new Dictionary<Vector2Int, ColorType>();
    public List<Block> allBlocks = new List<Block>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterBlock(Block block)
    {
        if (!allBlocks.Contains(block))
            allBlocks.Add(block);
    }

    public void UpdateColorGrid(Block block)
    {
        // Önce bu block'un kapladığı yerleri temizle
        foreach (var kvp in new Dictionary<Vector2Int, ColorType>(colorGrid))
        {
            Vector2Int pos = kvp.Key;
            Vector2Int[] occupied = block.GetOccupiedGridPositions();
            foreach (var occ in occupied)
            {
                if (pos == occ)
                    colorGrid.Remove(pos);
            }
        }

        // Yeniden ekle
        var positions = block.GetOccupiedGridPositions();
        for (int i = 0; i < positions.Length; i++)
        {
            colorGrid[positions[i]] = block.blockData[i].color;
        }
    }

    public void CheckForMatches()
    {
        // Önce tek renk blokları bul ve match yap
        List<Block> singleColorBlocks = new List<Block>();
        foreach (var block in allBlocks)
        {
            if (IsSingleColor(block))
                singleColorBlocks.Add(block);
        }

        // Tek renk blokları yok et
        foreach (var block in singleColorBlocks)
        {
            Debug.Log($"🎯 Single Color Auto-Match: {block.blockData[0].color}");
            foreach (var partData in block.blockData)
            {
                Vector2Int pos = block.GetOccupiedGridPositions()[0];
                if (colorGrid.ContainsKey(pos))
                    colorGrid.Remove(pos);
                Destroy(partData.part);
            }
            allBlocks.Remove(block);
            Destroy(block.gameObject);
        }
        for (int x = 0; x < width - 1; x++)
        {
            for (int y = 0; y < height - 1; y++)
            {
                Vector2Int a = new Vector2Int(x, y);
                Vector2Int b = new Vector2Int(x + 1, y);
                Vector2Int c = new Vector2Int(x, y + 1);
                Vector2Int d = new Vector2Int(x + 1, y + 1);

                if (colorGrid.ContainsKey(a) && colorGrid.ContainsKey(b) &&
                    colorGrid.ContainsKey(c) && colorGrid.ContainsKey(d))
                {
                    ColorType ca = colorGrid[a];
                    ColorType cb = colorGrid[b];
                    ColorType cc = colorGrid[c];
                    ColorType cd = colorGrid[d];

                    // Renkler aynı değilse zaten match yok
                    if (!(ca == cb && cb == cc && cc == cd))
                        continue;

                    // --- Bütünlük kontrolü ---
                    List<Vector2Int> matchPositions = new List<Vector2Int> { a, b, c, d };
                    List<Block> matchedBlocks = new List<Block>();

                    foreach (var block in allBlocks)
                    {
                        var occupiedPositions = block.GetOccupiedGridPositions();
                        int overlapCount = 0;

                        foreach (var pos in occupiedPositions)
                        {
                            if (matchPositions.Contains(pos))
                                overlapCount++;
                        }

                        if (overlapCount > 0)
                            matchedBlocks.Add(block);
                    }

                    // Eğer 4 hücre en az 2 farklı bloktan gelmiyorsa match iptal
                    if (matchedBlocks.Count < 2)
                        continue;

                    Debug.Log($"✅ MATCH FOUND: {ca} at positions {a}, {b}, {c}, {d}");

                    // --- Silme işlemleri ---
                    foreach (var block in matchedBlocks)
                    {
                        for (int i = block.blockData.Count - 1; i >= 0; i--)
                        {
                            Vector2Int pos = block.GetOccupiedGridPositions()[i];
                            if (matchPositions.Contains(pos))
                            {
                                if (colorGrid.ContainsKey(pos))
                                    colorGrid.Remove(pos);

                                GameObject part = block.blockData[i].part;
                                block.blockData.RemoveAt(i);
                                Destroy(part);
                            }
                        }
                    }

                    // Grid güncelle
                    colorGrid.Clear();
                    foreach (var block in allBlocks)
                    {
                        var positions = block.GetOccupiedGridPositions();
                        for (int i = 0; i < positions.Length; i++)
                        {
                            colorGrid[positions[i]] = block.blockData[i].color;
                        }
                    }

                    // 2 blok varsa birleştir
                    if (matchedBlocks.Count == 2)
                    {
                        Block blockA = matchedBlocks[0];
                        Block blockB = matchedBlocks[1];
                        StartCoroutine(MoveAndTransfer(blockA, blockB, 0.3f));
                    }
                }
            }
        }
    }


    private IEnumerator MoveAndTransfer(Block blockA, Block blockB, float duration)
    {
        blockA.SetColliderActive(false);
        blockB.SetColliderActive(false);

        Vector3 startPos = blockB.transform.position;
        Vector3 targetPos = blockA.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            blockB.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        blockB.transform.position = targetPos;

        List<BlockData> partsToMove = new List<BlockData>(blockB.blockData);
        foreach (var partData in partsToMove)
        {
            partData.part.transform.SetParent(blockA.transform);
            blockA.blockData.Add(partData);
        }

        blockB.blockData.Clear();
        allBlocks.Remove(blockB);
        Destroy(blockB.gameObject);

        for (int i = 0; i < blockA.blockData.Count; i++)
        {
            blockA.blockData[i].part.name = $"{i + 1}";
        }
        blockA.SetColliderActive(true);

        if (IsSingleColor(blockA))
        {
            CheckForMatches();
        }
    }


    private bool IsSingleColor(Block block)
    {
        if (block.blockData.Count == 0)
            return false;

        ColorType firstColor = block.blockData[0].color;
        foreach (var data in block.blockData)
        {
            if (data.color != firstColor)
                return false;
        }
        return true;
    }
}
