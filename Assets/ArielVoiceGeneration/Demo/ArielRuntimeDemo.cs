using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI; 
using Debug = UnityEngine.Debug;
using UnityEngine.Playables;

using ArielCommonTypes;
using ArielVoiceRemote;
using ArielVoiceLocal;

// For this demo, make sure you have an event system in the scene
// Add an audio source to the Event System object
// Add an ArielRuntimeDemo script to the Event System object

public class ArielDemoUI : MonoBehaviour
{
    private GameObject inputBox;
    private InputField inputField;
    private Canvas canvas;
    private AudioSource audioSource;
    public string voice = "";
    public string apiKey = "";
    private bool isPlaying = false;
    private bool connectedToInternet = true;
    public static bool localExecution = false;
    ArielLocal arielLocal = new ArielLocal(); // If using the local server, you will need an instance of it, because of the server reference

    void Start()
    {
        CreateInputBox();

        // Check internet connection
        connectedToInternet = Application.internetReachability != NetworkReachability.NotReachable;

        // if localExecution is true, start local server
        if (localExecution)
        {
            arielLocal.StartServer();
        }
        else if (!connectedToInternet && !localExecution)
        {
            Debug.LogError("No internet connection and remote version selected. Quitting game");

            // End game if no internet connection
            Application.Quit();
        }

        EnableTextInput();

        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;  // Prevent auto-play
    }

    void Update()
    {
        // Check if the Esc key is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (localExecution)
            {
                arielLocal.ShutdownServer();
            }
            // Exit the game
            Application.Quit();
        }
    }

    private async void SubmitText(string input)
    {        
        // Disable text input while processing
        DisableTextInput();

        if (!localExecution && connectedToInternet)
        {   
            // Generate TTS on Remote Server
            AudioClip audioClip = await ArielRemote.TextToAudioRuntime(voice, input, apiKey: apiKey, logs: true);

            // If audioClip is null, it means there was an error
            if (audioClip == null)
            {
                Debug.LogError("Error generating audio. Please check the logs for more information.");
            }
            else 
            {
                // Attach to AudioSource
                audioSource.clip = audioClip;

                // Play audio
                audioSource.Play();
            }
            // Start coroutine to check when audio has stopped
            StartCoroutine(CheckIfAudioStopped());
        }
        else if (localExecution)
        {   
            // Generate TTS on Local Server
            AudioClip audioClip = await arielLocal.TextToAudioRuntime(voice, input, logs: true);

            // If audioClip is null, it means there was an error
            if (audioClip == null)
            {
                Debug.LogError("Error generating audio. Please check the logs for more information.");
            }
            else {
                // Attach to AudioSource
                audioSource.clip = audioClip;

                // Play audio
                audioSource.Play();
            }            
            
            // Start coroutine to check when audio has stopped
            StartCoroutine(CheckIfAudioStopped());
        }
        else {
            Debug.LogError("No Internet available for remote execution. Quitting game.");
            Application.Quit();
        }
    }

    private System.Collections.IEnumerator CheckIfAudioStopped()
    {
        isPlaying = true;
        while (audioSource.isPlaying)
        {
            yield return null;
        }
        isPlaying = false;
        EnableTextInput();
    }

    void CreateInputBox()
    {
        GameObject canvasObject = new GameObject("InputCanvas");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.AddComponent<GraphicRaycaster>();

        inputBox = new GameObject("InputBox");
        inputBox.transform.SetParent(canvas.transform, false);

        RectTransform rectTransform = inputBox.AddComponent<RectTransform>();
        rectTransform.sizeDelta = new Vector2(400, 50);
        rectTransform.anchorMin = new Vector2(0.5f, 0.3f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.3f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;

        Image inputBackground = inputBox.AddComponent<Image>();
        inputBackground.color = Color.white;

        inputField = inputBox.AddComponent<InputField>();
        inputField.textComponent = CreateTextComponent(inputBox.transform);
        inputField.placeholder = CreatePlaceholderComponent(inputBox.transform);
        inputField.textComponent.supportRichText = false;
        inputField.onEndEdit.AddListener(SubmitText);
        inputField.ActivateInputField();
        inputField.Select();
        inputField.text = "";
        inputField.interactable = true;
    }

    private Text CreateTextComponent(Transform parent)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);
        Text text = textObject.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;

        RectTransform rectTransform = text.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(10, 5);
        rectTransform.offsetMax = new Vector2(-10, -5);

        return text;
    }

    private Text CreatePlaceholderComponent(Transform parent)
    {
        GameObject placeholderObject = new GameObject("Placeholder");
        placeholderObject.transform.SetParent(parent, false);
        Text placeholder = placeholderObject.AddComponent<Text>();
        placeholder.text = "Enter text...";
        placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholder.color = new Color(0.5f, 0.5f, 0.5f);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.supportRichText = false;

        RectTransform rectTransform = placeholder.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(10, 5);
        rectTransform.offsetMax = new Vector2(-10, -5);

        return placeholder;
    }

    private void DisableTextInput()
    {
        if (inputField != null)
        {
            inputField.interactable = false;
        }
    }

    private void EnableTextInput()
    {
        if (inputField != null && !isPlaying)
        {
            inputField.interactable = true;

            // Set text input to empty
            inputField.text = "";
        }
    }

    // On Game End
    void OnApplicationQuit()
    {
        if (localExecution)
        {
            arielLocal.ShutdownServer();
        }
    }
}
