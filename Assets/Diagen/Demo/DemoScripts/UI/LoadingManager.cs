using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LoadingManager : MonoBehaviour
{
    public GameObject loadingScreen; // Assign a UI Panel in the Inspector
    public float loadingTime = 10f; // 10 seconds delay

    void Start()
    {
        StartCoroutine(ShowLoadingScreen());
    }

    IEnumerator ShowLoadingScreen()
    {
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true); // Show loading screen
        }

        // Disable player controls (if needed)
        if (FindObjectOfType<PlayerController>() != null)
        {
            FindObjectOfType<PlayerController>().enabled = false;
        }

        yield return new WaitForSeconds(loadingTime);

        if (loadingScreen != null)
        {
            loadingScreen.SetActive(false); // Hide loading screen
        }

        // Enable player controls again
        if (FindObjectOfType<PlayerController>() != null)
        {
            FindObjectOfType<PlayerController>().enabled = true;
        }
    }
}
