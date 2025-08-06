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
        if (Input.GetKeyDown(KeyCode.P))
        {
            StartCoroutine(SwapFirstTwoAreas());
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
            Debug.Log($"🎯 Single Color Auto-Match: {block.blockData[0].color}");
            Material matchMaterial = block.blockData[0].part.GetComponent<MeshRenderer>().material;
            Slot emptySlot = SlotManager.Instance.GetNextEmptySlot();
            if (emptySlot != null)
            {
                emptySlot.Occupy();
                SpawnBlock(GameScript.Instance.blockPrefab, block.transform.position, matchMaterial, emptySlot.transform);
            }

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
                    List<Block> matchedBlocks = new List<Block>();

                    foreach (var block in allBlocks)
                    {
                        var occupiedPositions = block.GetOccupiedGridPositions();
                        foreach (var pos in occupiedPositions)
                        {
                            if (matchPositions.Contains(pos))
                            {
                                if (!matchedBlocks.Contains(block))
                                    matchedBlocks.Add(block);
                                break;
                            }
                        }
                    }

                    if (matchedBlocks.Count < 2)
                        continue;

                    Debug.Log($"✅ MATCH FOUND: {ca} at positions {a}, {b}, {c}, {d}");

                    Material matchMaterial = null;
                    foreach (var block in matchedBlocks)
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

                    Slot emptySlot = SlotManager.Instance.GetNextEmptySlot();
                    if (emptySlot != null && matchMaterial != null)
                    {
                        emptySlot.Occupy();
                        SpawnBlock(GameScript.Instance.blockPrefab, new Vector3(a.x * cellSize, 0, a.y * cellSize), matchMaterial, emptySlot.transform);
                    }


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

                    UpdateColorGridFromAll();

                    if (matchedBlocks.Count == 2)
                    {
                        Block blockA = matchedBlocks[0];
                        Block blockB = matchedBlocks[1];
                        isMatchProcessing = true;  // Eşleşme başladı, yer çekimi kapalı
                        StartCoroutine(MoveAndTransfer(blockA, blockB, 0.3f));
                    }
                }
            }
        }

        if (!isMatchProcessing)
            ApplyGravityToEmptyBlocks();

        TargetManager.Instance.MoveMatchingCubesToTargets();
    }

    public void SpawnBlock(GameObject blockPrefab, Vector3 position, Material mat, Transform slotTarget)
    {
        var newBlockObj = Instantiate(blockPrefab, position, Quaternion.identity);
        newBlockObj.transform.GetChild(0).GetComponent<MeshRenderer>().material = mat;

        NewBlockScript nbs = newBlockObj.GetComponent<NewBlockScript>();
        if (nbs != null)
        {
            TargetManager.Instance.RegisterNewBlock(nbs);

            // Slot bilgisini kaydet
            Slot slot = slotTarget.GetComponent<Slot>();
            if (slot != null)
                nbs.originSlot = slot;
        }

        StartCoroutine(MoveBlockToSlot(newBlockObj.transform, new Vector3(30, 0, 0), slotTarget.position + new Vector3(0, 1, 0)));
    }


    private IEnumerator MoveBlockToSlot(Transform block, Vector3 targetRot, Vector3 targetPos, float duration = 0.5f, float arcHeight = 5f)
    {
        Vector3 startPos = block.position;
        Vector3 controlPoint1 = startPos + Vector3.up * arcHeight;
        Vector3 controlPoint2 = targetPos + Vector3.up * arcHeight;
        Vector3 scale = new Vector3(.35f, .35f, .35f);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            Vector3 m1 = Vector3.Lerp(startPos, controlPoint1, t);
            Vector3 m2 = Vector3.Lerp(controlPoint1, controlPoint2, t);
            Vector3 m3 = Vector3.Lerp(controlPoint2, targetPos, t);

            Vector3 m4 = Vector3.Lerp(m1, m2, t);
            Vector3 m5 = Vector3.Lerp(m2, m3, t);

            block.position = Vector3.Lerp(m4, m5, t);
            block.localScale = Vector3.Lerp(Vector3.one * .5f, scale, t);

            elapsed += Time.deltaTime;
            yield return null;
        }

        block.position = targetPos + new Vector3(0, 0, 0.5f);
        block.rotation = Quaternion.Euler(targetRot);
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

    public void DebugBlockStates()
    {
        Debug.Log("===== BLOK DURUMLARI =====");
        var states = GetBlockStates();
        foreach (var block in states)
        {
            var coords = block.Item1;
            var state = block.Item2;
            Debug.Log($"{coords[0].x},{coords[0].y} {coords[1].x},{coords[1].y} {coords[2].x},{coords[2].y} {coords[3].x},{coords[3].y} {state}");
        }
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
        DebugBlockStates();
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

    /// <Block Swap Animation>
    /// ///////////////////////////////////////////
    /// </summary>
    /// <returns></returns>
    private IEnumerator SwapFirstTwoAreas()
    {
        Vector2Int[] area1 = new Vector2Int[]
        {
            new Vector2Int(0,0), new Vector2Int(1,0),
            new Vector2Int(0,1), new Vector2Int(1,1)
        };

        Vector2Int[] area2 = new Vector2Int[]
        {
            new Vector2Int(2,0), new Vector2Int(3,0),
            new Vector2Int(2,1), new Vector2Int(3,1)
        };

        yield return SwapBlocksAnimated(area1, area2, 0.4f);
    }

    private IEnumerator SwapBlocksAnimated(Vector2Int[] area1, Vector2Int[] area2, float duration)
    {
        Block block1 = GetBlockAtPositions(area1);
        Block block2 = GetBlockAtPositions(area2);

        if (block1 == null || block2 == null)
        {
            Debug.LogWarning("⚠ Swap yapılamadı, alanlardan biri boş!");
            yield break;
        }

        Vector3 startPos1 = block1.transform.position;
        Vector3 startPos2 = block2.transform.position;

        // Üstten giden (block1)
        Vector3 control1_1 = startPos1 + Vector3.up * 2f;
        Vector3 control1_2 = startPos2 + Vector3.up * 2f;

        // Alttan giden (block2)
        Vector3 control2_1 = startPos2 + Vector3.down * 0.5f;
        Vector3 control2_2 = startPos1 + Vector3.down * 0.5f;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            block1.transform.position = CalculateBezierPoint(t, startPos1, control1_1, control1_2, startPos2);
            block2.transform.position = CalculateBezierPoint(t, startPos2, control2_1, control2_2, startPos1);

            yield return null;
        }

        block1.transform.position = startPos2;
        block2.transform.position = startPos1;

        UpdateColorGridFromAll();

        Debug.Log("🔄 İlk iki alan swap edildi!");

        yield return new WaitForSeconds(0.05f);
        CheckForMatches();
    }

    private Vector3 CalculateBezierPoint(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float u = 1 - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;

        Vector3 p = uuu * p0;
        p += 3 * uu * t * p1;
        p += 3 * u * tt * p2;
        p += ttt * p3;

        return p;
    }

    private Block GetBlockAtPositions(Vector2Int[] positions)
    {
        foreach (var block in allBlocks)
        {
            var occupied = block.GetOccupiedGridPositions();
            bool matches = true;
            foreach (var pos in positions)
            {
                if (!System.Array.Exists(occupied, p => p == pos))
                {
                    matches = false;
                    break;
                }
            }
            if (matches)
                return block;
        }
        return null;
    }

}
