// Assets/Rumble/RpgQuest/Bridges/Combat/ScubaImmunityDriver.cs
using UnityEngine;

[DisallowMultipleComponent]
public class ScubaImmunityDriver : MonoBehaviour
{
    [Header("State (set by binder)")]
    public bool ownsScuba;      // has the upgrade
    public bool scubaEquipped;  // currently equipped

    [Header("Hook-ups (auto if empty)")]
    public ScubaUnderwaterImmunity scubaBridge;  // the script you just tested
    public GameObject scubaVisual;               // optional: tank/gear mesh to toggle

    void Awake()
    {
        if (!scubaBridge) scubaBridge = GetComponent<ScubaUnderwaterImmunity>();
    }

    /// <summary>Call this whenever ownership/equipped changes.</summary>
    public void Refresh()
    {
        bool active = ownsScuba && scubaEquipped;

        if (scubaBridge)
            scubaBridge.SetScuba(active);   // ← enables the actual immunity logic

        if (scubaVisual)
            scubaVisual.SetActive(active);
    }
}
