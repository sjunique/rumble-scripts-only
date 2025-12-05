using UnityEngine;
using System.Collections.Generic;
using Invector.vItemManager;

public class QuickEquipHotkeys : MonoBehaviour
{
    public vItemType meleeType   = vItemType.MeleeWeapon;   // 1 = melee (press Alpha1)
    public vItemType shooterType = vItemType.ShooterWeapon; // 2 = shooter (press Alpha2)

    private vItemManager im;

    void Awake() => im = GetComponent<vItemManager>();

    void Update()
    {
        if (!im || im.inventory == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipFirstOfType(meleeType);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipFirstOfType(shooterType);
    }

    // ----------------------------------------

    void EquipFirstOfType(vItemType type)
    {
        var item = FirstItemOfType(type);
        if (!item)
        {
            Debug.Log($"[QuickEquip] No {type} in inventory");
            return;
        }

        // Find an EquipArea + Slot that accepts this item type
        if (!TryFindAreaAndSlot(type, out var area, out int slotIndex))
        {
            Debug.LogWarning($"[QuickEquip] No EquipArea/slot accepts items of type {type}. Check your Inventory > Equip Areas.");
            return;
        }

        // Add to that slot and auto-equip it (public API on vEquipArea)
        area.AddItemToEquipSlot(slotIndex, item, autoEquip: true);
        Debug.Log($"[QuickEquip] Equipped '{item.name}' to area '{area.name}' slot #{slotIndex}");
    }

    vItem FirstItemOfType(vItemType type)
    {
        // Some builds expose im.items, some rely on im.inventory.items
        IEnumerable<vItem> Items()
        {
            if (im.items != null) foreach (var it in im.items) if (it) yield return it;
            if (im.inventory && im.inventory.items != null)
                foreach (var it in im.inventory.items) if (it) yield return it;
        }
        foreach (var it in Items())
            if (it.type == type)
                return it;
        return null;
    }

    bool TryFindAreaAndSlot(vItemType type, out vEquipArea area, out int slotIndex)
    {
        area = null; slotIndex = -1;
        var areas = im.inventory?.equipAreas;
        if (areas == null) return false;

        // vEquipArea exposes 'equipSlots', and each vEquipSlot has 'itemType' (a list of allowed types)
        foreach (var a in areas)
        {
            if (a == null || a.equipSlots == null) continue;
            for (int i = 0; i < a.equipSlots.Count; i++)
            {
                var slot = a.equipSlots[i];
                if (slot != null && slot.isValid && slot.itemType != null && slot.itemType.Contains(type))
                {
                    area = a; slotIndex = i;
                    return true;
                }
            }
        }
        return false;
    }
}
