using UnityEngine;
using System.Collections.Generic;

public class TargetScript : MonoBehaviour
{
    public List<Transform> holder;
    public int colorID; // ColorType ile aynı index olmalı

    public Transform GetNextHolder()
    {
        if (holder.Count > 0)
            return holder[0]; // şimdilik ilk holder
        return null;
    }
}
