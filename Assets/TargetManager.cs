using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetManager : MonoBehaviour
{
    public static TargetManager Instance;

    public List<Package> packages = new List<Package>();
    public List<NewBlockScript> newBlocks = new List<NewBlockScript>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void RegisterPackage(Package package)
    {
        if (!packages.Contains(package))
            packages.Add(package);
    }

    public void RegisterNewBlock(NewBlockScript newBlock)
    {
        if (!newBlocks.Contains(newBlock))
            newBlocks.Add(newBlock);
    }
    void Update()
    {

    }

    public void MoveMatchingCubesToTargets()
    {
        foreach (var newBlock in newBlocks)
        {
            foreach (var package in packages)
            {
                if (newBlock.newBlockColor == (int)package.packageColor)
                {
                    if (newBlock.transform.parent == package.transform)
                        break;
                    StartCoroutine(MoveBlockToPackageRoutine(newBlock.transform, package.transform));
                    break;
                }
            }
        }
    }

    IEnumerator MoveBlockToPackageRoutine(Transform block, Transform packageTarget)
    {
        Vector3 startPos = block.position;
        Vector3 targetLocalPos = new Vector3(0f, 1.8f, 0f);
        Vector3 endPos = packageTarget.TransformPoint(targetLocalPos);
        Vector3 controlPoint = startPos + Vector3.up * 2f;

        float duration = 0.5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            Vector3 bezierPos = Mathf.Pow(1 - t, 2) * startPos
                              + 2 * (1 - t) * t * controlPoint
                              + Mathf.Pow(t, 2) * endPos;

            block.position = bezierPos;

            yield return null;
        }

        block.position = endPos;
        block.SetParent(packageTarget);
        block.localRotation = Quaternion.identity;
        block.localPosition = targetLocalPos;

        NewBlockScript nbs = block.GetComponent<NewBlockScript>();
        if (nbs != null && nbs.originSlot != null)
        {
            nbs.originSlot.Free();
        }

        Animator anim = block.GetChild(0).GetComponent<Animator>();
        if (anim != null)
        {
            anim.enabled = true;
            anim.SetTrigger("Occupy");
        }
    }

}
