using UnityEngine;
using UnityEngine.UI;

public class TrustLevelUI : MonoBehaviour
{
    [SerializeField] private Image trustBar; // Assign in the Inspector

    public float trustLevel = 0.5f; // Always starts at 0.5

    private readonly Color lowTrustColor = Color.red;    // Trust = 0
    private readonly Color midTrustColor = Color.yellow; // Trust = 0.5
    private readonly Color highTrustColor = Color.green; // Trust = 1

    private readonly Vector3 minScale = new Vector3(0f, 1f, 1f); // Scale at trust 0
    private readonly Vector3 maxScale = new Vector3(1f, 1f, 1f); // Scale at trust 1

    private void Start()
    {
        trustLevel = 0.5f; // Force correct initial value
        UpdateTrustUI();
    }

    public void ChangeTrustLevel(float changeAmount)
    {
        // Clamp trust level between 0 and 1
        trustLevel = Mathf.Clamp(trustLevel + changeAmount, 0f, 3f);
        UpdateTrustUI();
    }

    private void UpdateTrustUI()
    {
        if (trustBar == null) return;

        // Interpolate color between red (0), yellow (0.5), and green (1)
        trustBar.color = trustLevel < 0.5f
            ? Color.Lerp(lowTrustColor, midTrustColor, trustLevel * 2)
            : Color.Lerp(midTrustColor, highTrustColor, (trustLevel - 0.5f) * 2);

        // Interpolate scale
        trustBar.transform.localScale = Vector3.Lerp(minScale, maxScale, trustLevel);
    }
}
