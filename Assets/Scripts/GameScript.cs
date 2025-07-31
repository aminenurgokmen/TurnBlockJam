using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameScript : MonoBehaviour
{
    public static GameScript Instance;
    public Material[] materials;
    public GameObject blockPrefab;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Material AssignMaterial(ColorType color)
    {
       return materials[(int)color];
    }
    
   
}
