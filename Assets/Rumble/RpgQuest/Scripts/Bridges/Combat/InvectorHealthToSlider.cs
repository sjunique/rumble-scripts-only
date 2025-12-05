// Assets/Rumble/RpgQuest/UI/InvectorHealthToSlider.cs
using UnityEngine;
using UnityEngine.UI;
using Invector.vCharacterController;
using Invector;
[RequireComponent(typeof(vHealthController))]
public class InvectorHealthToSlider : MonoBehaviour
{
    public Slider healthSlider;
    private vHealthController health;

    void Awake()
    {
        health = GetComponent<vHealthController>();
        if (healthSlider)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = health.MaxHealth;
            healthSlider.value    = health.currentHealth;
        }
    }

    void LateUpdate()
    {
        if (healthSlider) healthSlider.value = health.currentHealth;
    }
}
