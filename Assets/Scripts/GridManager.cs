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

    public bool isMatchProcessing = false;
    private bool isMoving = false;

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
        UpdateColorGridFromAll();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.O))
        {
            DebugTopTwoRowsSlidingBlocks();
        }
        {

        }

        if (!isMatchProcessing)
            ApplyGravityToEmptyBlocks();
    }





    public void UpdateColorGridFromAll()
    {
        colorGrid.Clear();
        foreach (var block in allBlocks)
        {
            var positions = block.GetOccupiedGridPositions();
            for (int i = 0; i < positions.Length; i++)
            {
                colorGrid[positions[i]] = block.blockData[i].color;
            }
        }
        // CheckAndSpawnAtTopIfEmpty(GameScript.Instance.blockPrefab);
    }

    public void UpdateColorGrid(Block block)
    {
        foreach (var pos in block.GetOccupiedGridPositions())
        {
            if (colorGrid.ContainsKey(pos))
                colorGrid.Remove(pos);
        }

        var positions = block.GetOccupiedGridPositions();
        for (int i = 0; i < positions.Length; i++)
        {
            colorGrid[positions[i]] = block.blockData[i].color;
        }
    }

    public void CheckForMatches()
    {
        List<Block> singleColorBlocks = new List<Block>();
        foreach (var block in allBlocks)
        {
            if (IsSingleColor(block))
                singleColorBlocks.Add(block);
        }

        foreach (var block in singleColorBlocks)
        {
            Material matchMaterial = block.blockData[0].part.GetComponent<MeshRenderer>().material;

            Vector2Int collectPos = block.GetOccupiedGridPositions()[0];
            GameScript.Instance.Collected(matchMaterial, new Vector3(collectPos.x * cellSize, 0, collectPos.y * cellSize));

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

                    if (!(ca == cb && cb == cc && cc == cd))
                        continue;

                    List<Vector2Int> matchPositions = new List<Vector2Int> { a, b, c, d };
                    HashSet<Block> involvedBlocks = new HashSet<Block>();

                    foreach (var block in allBlocks)
                    {
                        var occupiedPositions = block.GetOccupiedGridPositions();
                        foreach (var pos in occupiedPositions)
                        {
                            if (matchPositions.Contains(pos))
                            {
                                involvedBlocks.Add(block);
                                break;
                            }
                        }
                    }

                    if (involvedBlocks.Count != 2)
                        continue; // ❌ 2 bloktan farklıysa match sayma

                    Debug.Log($"✅ MATCH FOUND between 2 blocks at {a}, {b}, {c}, {d}");

                    // Devam: matchMaterial bul, parçaları sil vs.
                    Material matchMaterial = null;
                    foreach (var block in involvedBlocks)
                    {
                        var positions = block.GetOccupiedGridPositions();
                        for (int i = 0; i < positions.Length; i++)
                        {
                            if (matchPositions.Contains(positions[i]))
                            {
                                matchMaterial = block.blockData[i].part.GetComponent<MeshRenderer>().sharedMaterial;
                                break;
                            }
                        }
                        if (matchMaterial != null)
                            break;
                    }

                    GameScript.Instance.Collected(matchMaterial, new Vector3((a.x * cellSize) + 1, 0, (a.y * cellSize) + 1));

                    foreach (var block in involvedBlocks)
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

                    UpdateColorGridFromAll();

                    // Sadece 2 blok olduğundan garanti
                    Block[] blocks = new Block[2];
                    involvedBlocks.CopyTo(blocks);
                    isMatchProcessing = true;
                    StartCoroutine(MoveAndTransfer(blocks[0], blocks[1], 0.3f));
                }

            }
        }

        if (!isMatchProcessing)
            ApplyGravityToEmptyBlocks();

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
            if (blockB == null) yield break;
            blockB.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (blockB == null) yield break;

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

        UpdateColorGridFromAll();

        isMatchProcessing = false; // Eşleşme bitti, yer çekimi tekrar aktif

        ApplyGravityToEmptyBlocks();

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

    public void DebugTopTwoRowsSlidingBlocks()
    {
        // Sadece y çiftleri: 01, 23, 45, ...
        for (int rowY1 = 0; rowY1 < height - 1; rowY1 += 2)
        {
            int rowY2 = rowY1 + 1;

            for (int x = 0; x < width - 1; x += 2)
            {
                Vector2Int a = new Vector2Int(x, rowY2);     // üst sol
                Vector2Int b = new Vector2Int(x + 1, rowY2); // üst sağ
                Vector2Int c = new Vector2Int(x, rowY1);     // alt sol
                Vector2Int d = new Vector2Int(x + 1, rowY1); // alt sağ

                bool hasA = colorGrid.ContainsKey(a);
                bool hasB = colorGrid.ContainsKey(b);
                bool hasC = colorGrid.ContainsKey(c);
                bool hasD = colorGrid.ContainsKey(d);

                string state = (hasA && hasB && hasC && hasD) ? "Dolu" :
                               (!hasA && !hasB && !hasC && !hasD) ? "Boş" : "Karışık";

                Debug.Log($"{a.x}{a.y} {b.x}{b.y} {c.x}{c.y} {d.x}{d.y} => {state}");

                if (state == "Boş")
                {
                    // Hedef konum: bu 2x2 bloğun merkezi
                    Vector3 spawnTarget = new Vector3(x + 1f, 0, rowY1 + 1f) * cellSize;

                    // Spawn pozisyonu: ekranın üstünde yukarıda bir yer (örnek: grid'in üstünden 2 birim yukarı)
                    float spawnHeight = (height + 2);
                    Vector3 spawnStart = new Vector3(x + 1f, 0, spawnHeight) * cellSize;

                    GameObject newBlockObj = Instantiate(GameScript.Instance.blockPrefab, spawnStart, Quaternion.identity);
                    Block newBlock = newBlockObj.GetComponent<Block>();

                    if (newBlock != null)
                    {
                        RegisterBlock(newBlock);
                        Vector2Int gridPos = new Vector2Int(x + 1, rowY1 + 1);
                        Vector3 expectedWorld = new Vector3(gridPos.x, 0, gridPos.y) * cellSize;

                        Debug.Log($"Yeni blok yerleştirildi → GridPos: {gridPos}, CellSize: {cellSize}, WorldPos: {expectedWorld}");

                        StartCoroutine(MoveBlockToPosition(newBlockObj.transform, spawnTarget, 0.5f));

                        return; // Sadece bir blok spawn et
                    }
                    else
                    {
                        Debug.LogError("blockPrefab üzerinde Block component bulunamadı!");
                    }
                }
            }
        }

        Debug.Log("Hiçbir boş 2x2 alan bulunamadı.");
    }


   private IEnumerator MoveBlockToPosition(Transform blockTransform, Vector3 targetPos, float duration)
{
    Vector3 startPos = blockTransform.position;
    float elapsed = 0f;

    while (Vector3.Distance(blockTransform.position, targetPos) > 0.001f)
    {
        // Zamanla artış
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);

        // Pozisyonu güncelle
        blockTransform.position = Vector3.Lerp(startPos, targetPos, t);

        yield return null;
    }

    // Son pozisyonu kesin olarak hedefe ayarla
    blockTransform.position = targetPos;
    Debug.Log("Block final position set to: " + targetPos);
}



    //Kayma durumları cart curt

    public List<(Vector2Int[], string)> GetBlockStates()
    {
        List<(Vector2Int[], string)> result = new List<(Vector2Int[], string)>();

        for (int x = 0; x < width - 1; x += 2)
        {
            for (int y = 0; y < height - 1; y += 2)
            {
                Vector2Int a = new Vector2Int(x, y);
                Vector2Int b = new Vector2Int(x, y + 1);
                Vector2Int c = new Vector2Int(x + 1, y + 1);
                Vector2Int d = new Vector2Int(x + 1, y);

                bool hasA = colorGrid.ContainsKey(a);
                bool hasB = colorGrid.ContainsKey(b);
                bool hasC = colorGrid.ContainsKey(c);
                bool hasD = colorGrid.ContainsKey(d);

                string state = (hasA && hasB && hasC && hasD) ? "Dolu" :
                               (!hasA && !hasB && !hasC && !hasD) ? "Boş" : "Karışık";

                result.Add((new Vector2Int[] { a, b, c, d }, state));
            }
        }
        return result;
    }

    public void ApplyGravityToEmptyBlocks()
    {
        var states = GetBlockStates();

        // Önce dikey kaydırma
        foreach (var block in states)
        {
            if (block.Item2 == "Boş")
            {
                Vector2Int[] coords = block.Item1;
                int minY = coords[0].y;
                int x1 = coords[0].x;
                int x2 = coords[2].x;

                // Yalnızca dikey boşluk
                for (int y = minY + 2; y < height; y += 2)
                {
                    Vector2Int checkA = new Vector2Int(x1, y);
                    Vector2Int checkB = new Vector2Int(x1, y + 1);
                    Vector2Int checkC = new Vector2Int(x2, y + 1);
                    Vector2Int checkD = new Vector2Int(x2, y);

                    if (colorGrid.ContainsKey(checkA) || colorGrid.ContainsKey(checkB) ||
                        colorGrid.ContainsKey(checkC) || colorGrid.ContainsKey(checkD))
                    {
                        StartCoroutine(MoveBlockDownSmooth(
                            new Vector2Int[] { checkA, checkB, checkC, checkD },
                            cellSize * 2, 0.2f));
                    }
                }
            }
        }

        // Sonra sadece en alt satırda yatay kaydırma
        foreach (var block in states)
        {
            if (block.Item2 == "Boş")
            {
                Vector2Int[] coords = block.Item1;
                int minY = coords[0].y;
                int minX = coords[0].x;

                // Sadece en alt satır
                if (minY == 0)
                {
                    Vector2Int rightA = new Vector2Int(minX + 2, 0);
                    Vector2Int rightB = new Vector2Int(minX + 2, 1);
                    Vector2Int rightC = new Vector2Int(minX + 3, 1);
                    Vector2Int rightD = new Vector2Int(minX + 3, 0);

                    if (colorGrid.ContainsKey(rightA) || colorGrid.ContainsKey(rightB) ||
                        colorGrid.ContainsKey(rightC) || colorGrid.ContainsKey(rightD))
                    {
                        StartCoroutine(MoveBlockSidewaysSmooth(
                            new Vector2Int[] { rightA, rightB, rightC, rightD },
                            -cellSize * 2, 0.2f));
                    }
                }
            }
        }

        UpdateColorGridFromAll();
        // DebugBlockStates();
    }


    private IEnumerator MoveBlockDownSmooth(Vector2Int[] positions, float moveDistance, float duration)
    {
        if (isMoving || isMatchProcessing) yield break;

        isMoving = true;

        List<Block> blocksToMove = new List<Block>();

        foreach (var block in allBlocks)
        {
            var occupied = block.GetOccupiedGridPositions();
            bool matches = false;
            foreach (var pos in occupied)
            {
                if (System.Array.Exists(positions, p => p == pos))
                {
                    matches = true;
                    break;
                }
            }
            if (matches)
                blocksToMove.Add(block);
        }

        float elapsed = 0f;
        Vector3 moveVector = new Vector3(0, 0, -moveDistance);

        Dictionary<Block, Vector3> startPositions = new Dictionary<Block, Vector3>();
        foreach (var b in blocksToMove)
        {
            startPositions[b] = b.transform.position;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            foreach (var b in blocksToMove)
            {
                if (b == null) continue;
                b.transform.position = Vector3.Lerp(startPositions[b], startPositions[b] + moveVector, t);
            }

            yield return null;
        }

        foreach (var b in blocksToMove)
        {
            if (b == null) continue;
            b.transform.position = startPositions[b] + moveVector;
        }

        UpdateColorGridFromAll();
        isMoving = false;

        CheckForMatches();
    }


    private IEnumerator MoveBlockSidewaysSmooth(Vector2Int[] positions, float moveDistance, float duration)
    {
        if (isMoving || isMatchProcessing) yield break;

        isMoving = true;

        List<Block> blocksToMove = new List<Block>();

        foreach (var block in allBlocks)
        {
            var occupied = block.GetOccupiedGridPositions();
            bool matches = false;
            foreach (var pos in occupied)
            {
                if (System.Array.Exists(positions, p => p == pos))
                {
                    matches = true;
                    break;
                }
            }
            if (matches)
                blocksToMove.Add(block);
        }

        float elapsed = 0f;
        Vector3 moveVector = new Vector3(moveDistance, 0, 0);

        Dictionary<Block, Vector3> startPositions = new Dictionary<Block, Vector3>();
        foreach (var b in blocksToMove)
        {
            startPositions[b] = b.transform.position;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            foreach (var b in blocksToMove)
            {
                if (b == null) continue;
                b.transform.position = Vector3.Lerp(startPositions[b], startPositions[b] + moveVector, t);
            }

            yield return null;
        }

        foreach (var b in blocksToMove)
        {
            if (b == null) continue;
            b.transform.position = startPositions[b] + moveVector;
        }

        UpdateColorGridFromAll();
        isMoving = false;

        CheckForMatches(); // Kayma sonrası yeni match kontrolü
    }

}