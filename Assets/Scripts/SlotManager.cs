using UnityEngine;
using System.Collections.Generic;

public class SlotManager : MonoBehaviour
{
    public static SlotManager Instance;
    public List<Slot> slots = new List<Slot>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    public Slot GetNextEmptySlot()
    {
        foreach (var slot in slots)
        {
            if (!slot.isOccupied)
            {
                return slot;
            }
        }
        return null; // boş yoksa null
    }
}
