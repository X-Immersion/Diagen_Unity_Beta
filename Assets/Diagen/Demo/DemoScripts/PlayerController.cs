using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    private bool isMoving;
    private Vector2 input;
    private Animator animator;
    public Vector3 cameraOffset = new Vector3(0f, 0f, -10f);
    public Camera mainCamera;
    public Camera battleCamera; // Assign in Unity

    public LayerMask solidObjects;
    public LayerMask grassLayer;

    private bool encoutered = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        mainCamera = Camera.main;

        if (battleCamera == null)
        {
            Debug.LogError("Battle Camera is not assigned!");
        }
    }

    private void Update()
    {
        if (!isMoving)
        {
            HandleInput();
        }
    }

    private void LateUpdate()
    {
        FollowPlayer();
    }

    private void HandleInput()
    {
        input.x = Input.GetAxisRaw("Horizontal");
        input.y = Input.GetAxisRaw("Vertical");

        if (input != Vector2.zero)
        {
            input = input.normalized;

            animator.SetFloat("moveX", input.x);
            animator.SetFloat("moveY", input.y);
            animator.SetBool("isMoving", true);

            Vector3 targetPos = transform.position + new Vector3(input.x, input.y, 0);
            if (IsWalkable(targetPos))
            {
                StartCoroutine(Move(targetPos));
            }
        }
        else
        {
            animator.SetBool("isMoving", false);
        }
    }

    private IEnumerator Move(Vector3 targetPos)
    {
        isMoving = true;

        while (Vector3.Distance(transform.position, targetPos) > 0.01f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPos;
        isMoving = false;
        if (encoutered == false)
        {
            CheckForEncounters();
        }
    }

    private void FollowPlayer()
    {
        if (mainCamera != null)
        {
            mainCamera.transform.position = transform.position + cameraOffset;
        }
    }

    private bool IsWalkable(Vector3 targetPos)
    {
        return Physics2D.OverlapCircle(targetPos, 0.1f, solidObjects) == null;
    }

    private void CheckForEncounters()
    {
        if (Physics2D.OverlapCircle(transform.position, 0.2f, grassLayer) != null)
        {
            if (Random.Range(1, 101) <= -1)
            {
                Debug.Log("Encountered a wild animal");
                SwitchToBattleCamera();
                encoutered = true;
            }
        }
    }

    private void SwitchToBattleCamera()
    {
        if (battleCamera != null && mainCamera != null)
        {
            Debug.Log("🎥 Switching to Battle Camera on Display 2");

            // Disable main camera and activate battle camera
            mainCamera.gameObject.SetActive(false);
            battleCamera.gameObject.SetActive(true);

            // Ensure the battle camera is the active one
            battleCamera.enabled = true;
        }
        else
        {
            Debug.LogError("⚠️ Cameras not assigned correctly!");
        }
    }

}
