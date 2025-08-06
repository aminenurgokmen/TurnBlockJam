using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using Unity.VisualScripting;
using UnityEngine;

public class GameScript : MonoBehaviour
{
    public float moveSpeed = 5f;
    public static GameScript Instance;
    public Material[] materials;

    public List<Target> targets;
    public List<TargetScript> targetScripts;
    private void Awake()
    {
        Instance = this;
    }
    void Start()
    {
            SetupTargets();
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Break();
        }
        // Handle game logic here
    }
    public void SetupTargets()
    {
        foreach (var item in CanvasManager.Instance.targetScripts)
        {
            item.gameObject.SetActive(false);
        }

        for (int i = 0; i < targets.Count; i++)
        {

            CanvasManager.Instance.targetScripts[i].gameObject.SetActive(true);
            CanvasManager.Instance.targetScripts[i].Setup(targets[i].color, targets[i].count);
            targetScripts.Add(CanvasManager.Instance.targetScripts[i]);

        }

    }

    public Material AssignMaterial(ColorType color)
    {
        return materials[(int)color];
    }

    public void Collected(Material mat, Vector3 pos)
    {
        int colorIdx = System.Array.IndexOf(materials, mat);
        Debug.Log($"Collected: {mat.name} at {pos} + Color Index: {colorIdx}");
        TargetScript targetScript = targetScripts.Find(x => x.targetColor == colorIdx);

        if (targetScript && targetScript.IsCompleted())
        {
            return;
        }
        if (targets.Count == 1 && targets[0].color == -1 &&  !targetScripts[0].IsCompleted())
        {
            targetScript = targetScripts[0];

        }
     
        if (targetScript != null)
        {
            CollectedUIScript collectedUI = Instantiate(CanvasManager.Instance.collectedUIPrefab ,GetComponent<Camera>().WorldToScreenPoint(pos), Quaternion.identity, CanvasManager.Instance.gamePanel.transform);
            collectedUI.targetScript = targetScript;
          //  collectedUI.transform.position = transform.position;
        }

    }
    [System.Serializable]
    public class Target
    {
        public int color;
        public int count;
    }

}
