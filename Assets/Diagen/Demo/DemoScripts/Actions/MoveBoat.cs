using UnityEngine;
using System.Collections; // Required for IEnumerator
using UnityEngine;

public class MoveBoat : MonoBehaviour
{
    public float startSpeed = 10f;
    public float minSpeed = 2f;
    public float shorePositionX = 7.16f;
    public float startPositionX = 100f;
    private float currentSpeed;

    private bool isMoving = false;

    void Start()
    {
        transform.position = new Vector3(startPositionX, transform.position.y, transform.position.z);
        currentSpeed = startSpeed;
    }

    void Update()
    {
        if (isMoving && transform.position.x > shorePositionX)
        {
            float distance = transform.position.x - shorePositionX;
            float t = distance / (startPositionX - shorePositionX);
            currentSpeed = Mathf.Lerp(startSpeed, minSpeed, 1 - t);
            transform.Translate(Vector3.left * currentSpeed * Time.deltaTime);
        }
        else if (isMoving)
        {
            transform.position = new Vector3(shorePositionX, transform.position.y, transform.position.z);
            isMoving = false;
            StartCoroutine(EndGameAfterDelay(3f));
        }
    }

    public void StartMoving()
    {
        isMoving = true;
        currentSpeed = startSpeed;
        Debug.Log("isMoving? "+ isMoving);

    }

    // Make sure this method is **public**
    public void Run()
    {
        Debug.Log("Running MoveBoat Script...");
        Debug.Log("Position "+ transform.position.x);
        StartMoving();
    }

    private IEnumerator EndGameAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        GameOver();
    }

    private void GameOver()
    {
        Debug.Log("Game Over! The boat has arrived. Closing game...");
        Application.Quit();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
