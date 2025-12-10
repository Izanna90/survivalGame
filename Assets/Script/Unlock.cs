using UnityEngine;

public class Unlock : MonoBehaviour
{
    [Header("Interaction")]
    public GameObject promptPrefab; // UI prefab that says "Press E to unlock door"
    public Vector3 promptOffset = new Vector3(0, 2f, 0);

    [Header("Door/House")]
    public Animator doorAnimator; // optional: play open animation
    public string openTriggerName = "Open"; // Animator trigger name

    private GameObject promptInstance;
    private bool playerInRange;
    private bool isUnlocked;

    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        ShowPrompt();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        HidePrompt();
    }

    void Update()
    {
        if (!playerInRange || isUnlocked) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            if (KeyState.HasKey)
            {
                UnlockAndStopGame();
            }
            else
            {
                // Optionally show "Chest contains the key"
            }
        }

        if (promptInstance)
        {
            promptInstance.transform.position = transform.position + promptOffset;
        }
    }

    void UnlockAndStopGame()
    {
        isUnlocked = true;
        HidePrompt();

        if (doorAnimator && !string.IsNullOrEmpty(openTriggerName))
        {
            doorAnimator.SetTrigger(openTriggerName);
        }

        // Stop the game
        Time.timeScale = 0f;
        // Optionally show a UI indicating success
    }

    void ShowPrompt()
    {
        if (promptInstance || promptPrefab == null) return;
        promptInstance = Instantiate(promptPrefab, transform.position + promptOffset, Quaternion.identity);
    }

    void HidePrompt()
    {
        if (promptInstance)
        {
            Destroy(promptInstance);
            promptInstance = null;
        }
    }
}
