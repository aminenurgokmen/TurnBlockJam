using UnityEngine;

public class Slot : MonoBehaviour
{
    public bool isOccupied = false;
    public Block currentBlock;

    public void Occupy()
    {
        isOccupied = true;
    }

    public void Free()
    {
        isOccupied = false;
    }
}
