using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBlockScript : MonoBehaviour
{
    public int newBlockColor;
    public Slot originSlot;

    void Start()
    {
        TargetManager.Instance.RegisterNewBlock(this);

        Material currentMat = transform.GetChild(0).GetComponent<MeshRenderer>().sharedMaterial;

        for (int i = 0; i < GameScript.Instance.materials.Length; i++)
        {
            if (GameScript.Instance.materials[i] == currentMat)
            {
                newBlockColor = i;
                break;
            }
        }

    }
}
