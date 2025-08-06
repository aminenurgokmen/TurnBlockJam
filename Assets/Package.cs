using UnityEngine;

public class Package : MonoBehaviour
{
    public int packageColor;
    bool inStage = true;

    void Start()
    {
        if (inStage)
        {
            TargetManager.Instance.RegisterPackage(this);
        }
        Material currentMat = GetComponent<MeshRenderer>().sharedMaterials[1];

        for (int i = 0; i < GameScript.Instance.materials.Length; i++)
        {
            if (GameScript.Instance.materials[i] == currentMat)
            {
                packageColor = i;
                break;
            }
        }
    }

}
