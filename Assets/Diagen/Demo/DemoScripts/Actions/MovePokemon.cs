using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MovePokemon : MonoBehaviour
{
    [SerializeField] private Transform targetPosition; // The position to move towards
    [SerializeField] private float moveSpeed = 3f; // Speed of movement
    [SerializeField] private float bounceHeight = 0.2f; // Height of the bounce effect
    [SerializeField] private float bounceSpeed = 5f; // Speed of bouncing

    private RectTransform uiImage;
    private Transform spriteTransform;
    private bool isUIElement;
    public Camera mainCamera;
    public Camera battleCamera; // Assign in Unity

    void Start()
    {
        // Detect if the object is a UI element or a world object
        uiImage = GetComponent<RectTransform>();
        spriteTransform = GetComponent<Transform>();

        if (uiImage != null && GetComponent<Image>() != null)
        {
            isUIElement = true;
        }
        else if (spriteTransform != null && GetComponent<SpriteRenderer>() != null)
        {
            isUIElement = false;
        }
    }

    public void Run()
    {
        Debug.Log("🏃 Run method executed!");
        // Start moving towards the target once you get the trigger "folowing"
        StartCoroutine(MoveTowardsTarget());
    }

    private IEnumerator MoveTowardsTarget()
    {
        Vector3 startPos = isUIElement ? uiImage.anchoredPosition : spriteTransform.position;

        while (Vector3.Distance(startPos, targetPosition.position) > 0.05f)
        {
            // Move smoothly towards the target
            Vector3 newPosition = Vector3.Lerp(startPos, targetPosition.position, moveSpeed * Time.deltaTime);

            // Add a bounce effect
            newPosition.y += Mathf.Sin(Time.time * bounceSpeed) * bounceHeight;

            if (isUIElement)
            {
                uiImage.anchoredPosition = newPosition;
            }
            else
            {
                spriteTransform.position = newPosition;
            }

            startPos = newPosition;
            yield return null;
        }

        // Final position fix
        if (isUIElement)
        {
            uiImage.anchoredPosition = targetPosition.position;
        }
        else
        {
            spriteTransform.position = targetPosition.position;
        }

        // 🔹 Switch back to the main camera after movement completes
        SwitchBackToMainCamera();
    }


    public void SwitchBackToMainCamera()
    {
        if (battleCamera != null && mainCamera != null)
        {
            Debug.Log("🎥 Switching back to Main Camera on Display 1");

            // Enable main camera and deactivate battle camera
            battleCamera.gameObject.SetActive(false);

            mainCamera.gameObject.SetActive(true);

            // Ensure the main camera is the active one
            mainCamera.enabled = true;
        }
        else
        {
            Debug.LogError("⚠️ Cameras not assigned correctly!");
        }
    }

}
