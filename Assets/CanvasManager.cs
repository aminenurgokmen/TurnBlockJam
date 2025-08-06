using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CanvasManager : MonoBehaviour
{
    // Start is called before the first frame update
    public static CanvasManager Instance;
    public GameObject gamePanel;

    public CollectedUIScript collectedUIPrefab;
    public List<TargetScript> targetScripts;
    void Awake()
    {
        Instance = this;
    }
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
