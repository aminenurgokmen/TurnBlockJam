using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectedUIScript : MonoBehaviour
{
    public TargetScript targetScript;
    Vector3 startPos, targetPos, midPos;
    float counter;
    bool completed;
    // Start is called before the first frame update
    void Start()
    {
        startPos =transform.position;
        midPos = Vector3.Lerp(startPos, targetScript.transform.position, 0.5f) + Vector3.right * Screen.width * 0.1f; 
        targetPos = targetScript.transform.position;
        counter = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        counter += Time.deltaTime;
        transform.position = Vector3.Lerp(Vector3.Lerp(startPos, midPos, counter), Vector3.Lerp(midPos, targetPos, counter), counter);
        if(counter >= 1f && !completed)
        {
            completed = true;
            Destroy(gameObject);
            targetScript.Collect();
        }
        
    }
}
