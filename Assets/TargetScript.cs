using System.Collections;
using System.Collections.Generic;
using Microsoft.Unity.VisualStudio.Editor;
using UnityEngine;

public class TargetScript : MonoBehaviour
{
    public int targetColor;
    public int targetCount;
    public int currentCount = 0;
    public TMPro.TextMeshProUGUI countText;

    void Start()
    {

        for (int i = 0; i < GameScript.Instance.materials.Length; i++)
        {
            if (i == targetColor)
            {
                targetColor = i;
                break;
            }
        }
    }
    public void Collect()
    {
        currentCount++;
        if (currentCount >= targetCount)
        {
            // Logic for when the target is fully collected
            Debug.Log("Target collected!");
        }
        UpdateCountText();
    }

    public void Setup(int colorIndex, int count)
    {
        //color -1 ise rengarenk
        targetColor = colorIndex;
        targetCount = count;
        currentCount = 0;
        UpdateCountText();
        // update ui
    }
    void UpdateCountText()
    {
        countText.text = (targetCount-currentCount) + "";
        // Logic to update the UI text showing the current count
        // This could be a Text component or similar
    }
    public bool IsCompleted()
    {
        return currentCount >= targetCount;
    }
}
