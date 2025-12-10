using UnityEngine;

public static class KeyState
{
    public static bool HasKey;
}

public class KeyScript : MonoBehaviour
{
    [Header("Chest Interaction")]
    public GameObject promptPrefab; // "Press E to open chest"
    public Vector3 promptOffset = new Vector3(0, 1.6f, 0);
    public Animator chestAnimator; // optional
    public string openTriggerName = "Open";

    private GameObject promptInstance;
    private bool playerInRange;
    private bool chestOpened;

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
        if (!playerInRange || chestOpened) return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            chestOpened = true;
            KeyState.HasKey = true; // grant invisible key
            HidePrompt();

            if (chestAnimator && !string.IsNullOrEmpty(openTriggerName))
                chestAnimator.SetTrigger(openTriggerName);
        }

        if (promptInstance)
            promptInstance.transform.position = transform.position + promptOffset;
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
