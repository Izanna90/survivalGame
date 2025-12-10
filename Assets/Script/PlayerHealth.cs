using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 2;
    public int currentHealth;

    [Header("UI")]
    public Slider healthSlider; // assign a Slider on a Canvas (top-left)
    public string autoFindSliderName = "HealthSlider";

    void Awake()
    {
        currentHealth = maxHealth;
        if (healthSlider == null)
        {
            var go = GameObject.Find(autoFindSliderName);
            if (go != null) healthSlider = go.GetComponent<Slider>();
        }
        if (healthSlider != null)
        {
            healthSlider.minValue = 0;
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }
    }

    public void TakeDamage(int amount)
    {
        if (currentHealth <= 0) return;
        currentHealth = Mathf.Max(0, currentHealth - amount);
        if (healthSlider != null) healthSlider.value = currentHealth;

        if (currentHealth <= 0)
        {
            // Optional: disable player controls here
            Time.timeScale = 0f;
            Debug.Log("Player died.");
        }
    }
}
