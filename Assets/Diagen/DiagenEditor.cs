#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Debug = UnityEngine.Debug;
using System.Threading.Tasks;

using DiagenCommonTypes;
using DiagenLayoutTypes;

namespace Diagen
{
    public class DiagenAPI : EditorWindow
    {
        [SerializeField]
        private Settings settings = new Settings();
        private int selectedMainTab = 0;
        string[] mainTabs = { "Setup", "LLM Generation", "Event Generation", "Topic Detection" };

        // init all gui tabs
        DiagenLayoutLlm diagenLayoutLlm;
        DiagenLayoutEvent diagenLayoutEvent;
        DiagenLayoutTopic diagenLayoutTopic;
        DiagenLayoutOption diagenLayoutOption;

        private string sessionStatesFilePath;

        // Add variables for GIF animation
        private Texture2D[] processingFrames;
        private Texture2D[] questionFrames;
        private Texture2D[] errorFrames;
        private int currentFrame;
        private float frameTime = 0.1f;
        public bool isRunning = false;
        public bool isError = false;
        public bool isQuestion = false;
        private float imageX = 0f; // X position of the image
        private float speed = 500f; // Speed of movement in pixels per second
        private string processingText = "Processing..."; // Text to display while processing
        private string questionText = "I couldn't find anything..."; // Text to display while processing
        private string errorText = "Error. Please check the logs."; // Text to display in case of an error
        [MenuItem("Window/Diagen Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<DiagenAPI>("Diagen Editor");
            window.minSize = new Vector2(300, 200);
            window.maxSize = new Vector2(800, 600);
        }

        private void OnEnable()
        {
            EditorApplication.quitting += OnEditorQuitting;

            // llamaServerPath = Path.Combine(Application.dataPath, "Diagen", "Local", LlamaServerExecutable);
            // modelPath = Path.Combine(Application.dataPath, "Diagen", "Local", "models", "baseXI7.gguf");
            sessionStatesFilePath = Path.Combine(Application.persistentDataPath, "sessionStates.json");
            settings = DiagenLayoutCommon.LoadSettings(settings, sessionStatesFilePath);

            diagenLayoutLlm = ScriptableObject.CreateInstance<DiagenLayoutLlm>();
            diagenLayoutLlm.SetDiagenAPI(this); // Pass reference to self
            
            diagenLayoutEvent = ScriptableObject.CreateInstance<DiagenLayoutEvent>();
            diagenLayoutEvent.SetDiagenAPI(this); // Pass reference to self
            
            diagenLayoutTopic = ScriptableObject.CreateInstance<DiagenLayoutTopic>();
            diagenLayoutTopic.SetDiagenAPI(this);

            diagenLayoutOption = ScriptableObject.CreateInstance<DiagenLayoutOption>();

            settings.llmServerSettings.Init(); // Ensure paths are initialized

            // Load GIF frames
            LoadGifFrames(2, "waiting");
            LoadGifFrames(1, "error");
            LoadGifFrames(1, "question");
            ProcessingState();
            MoveImage();
        }


        private void OnGUI()
        {
            if (settings == null)
            {
                Debug.LogError("Settings is null. Reinitializing.");
                settings = new Settings();
                settings.optionTables = new OptionTables();
                settings.sessionStates = new List<Session>();
            }

            if (mainTabs == null || mainTabs.Length == 0)
            {
                mainTabs = new string[] { "Setup", "LLM Generation", "Event Generation", "Topic Detection" }; // Default fallback
            }

            if (selectedMainTab < 0 || selectedMainTab >= mainTabs.Length)
            {
                Debug.LogWarning($"SelectedMainTab ({selectedMainTab}) is out of bounds. Resetting to 0.");
                selectedMainTab = 0;
            }

            selectedMainTab = GUILayout.Toolbar(selectedMainTab, mainTabs, GUILayout.Width(200), GUILayout.Height(30), GUILayout.ExpandWidth(true));

            var task = new Task<Settings>(() => settings);
            switch (selectedMainTab)
            {

               case 0: // Setup
                    if (settings.optionTables == null)
                    {
                        settings.optionTables = new OptionTables();
                    }
                    if (diagenLayoutOption == null)
                    {
                        diagenLayoutOption = ScriptableObject.CreateInstance<DiagenLayoutOption>();
                    }
                    // Display the session options tab
                    settings = diagenLayoutOption.SessionOptionsTab(settings);
                    break;

               
                case 1: // LLM Generation
                    if (diagenLayoutLlm == null)
                    {
                        diagenLayoutLlm = ScriptableObject.CreateInstance<DiagenLayoutLlm>();
                    }

                    // Display the llm tab
                    task = diagenLayoutLlm.LlmOptionsTab(settings);
                    task.ContinueWith(t => settings = t.Result, TaskScheduler.FromCurrentSynchronizationContext());
                    break;


                case 2: // Event Generation
                    if (diagenLayoutEvent == null)
                    {
                        diagenLayoutEvent = ScriptableObject.CreateInstance<DiagenLayoutEvent>();
                    }

                    // Display the events tab
                    settings = diagenLayoutEvent.EventsTab(settings);
                    break;

                case 3: // Topic Detection
                    if (diagenLayoutTopic == null)
                    {
                        diagenLayoutTopic = ScriptableObject.CreateInstance<DiagenLayoutTopic>();
                    }

                    // Display the topic detection tab
                    task = diagenLayoutTopic.TopicDetectionTab(settings);
                    task.ContinueWith(t => settings = t.Result, TaskScheduler.FromCurrentSynchronizationContext());
                    break;


                default:
                    GUILayout.Label("Invalid tab selected.", EditorStyles.boldLabel);
                    break;
            }

            if (isRunning && processingFrames != null && processingFrames.Length > 0)
            {
                float bottomY = position.height - 130; // Ensure the image stays at the bottom
                Rect imageRect = new Rect(imageX, bottomY, 100, 100); // Adjust width/height as needed
                GUI.DrawTexture(imageRect, processingFrames[currentFrame]);

                // Draw the "Waiting ..." text below the moving element
                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.alignment = TextAnchor.MiddleCenter;
                textStyle.normal.textColor = Color.white; // Change the text color if needed

                // Adjust the position of the text to be below the image
                Rect textRect = new Rect(imageX, bottomY + 105, 100, 20); // Adjust the Y position and height as needed
                GUI.Label(textRect, processingText, textStyle);
            }

            if (isError)
            {
                float bottomY = position.height - 130; // Ensure the image stays at the bottom
                float errorRectWidth = 138;
                float errorRectHeight = 79;
                Rect errorRect = new Rect((position.width - errorRectWidth) / 2, bottomY, errorRectWidth, errorRectHeight); // Center the errorRect
                GUI.DrawTexture(errorRect, errorFrames[0]);

                // Draw the "Error. Please check the logs." text below the moving element
                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.alignment = TextAnchor.MiddleCenter;
                textStyle.normal.textColor = Color.red; // Change the text color if needed

                // Calculate the width of the text dynamically
                Vector2 textSize = textStyle.CalcSize(new GUIContent(errorText));
                float textWidth = textSize.x;
                float textHeight = textSize.y;

                // Adjust the position of the text to be below the image and centered
                Rect textRect = new Rect(errorRect.x + (errorRect.width - textWidth) / 2, bottomY + errorRect.height + 5, textWidth, textHeight); // Adjust the Y position and height as needed
                GUI.Label(textRect, errorText, textStyle);
            }

            if (isQuestion)
            {
                float bottomY = position.height - 130; // Ensure the image stays at the bottom
                float questionRectWidth = 100;
                float questionRectHeight = 85;
                Rect questionRect = new Rect((position.width - questionRectWidth) / 2, bottomY, questionRectWidth, questionRectHeight); // Center the questionRect
                GUI.DrawTexture(questionRect, questionFrames[0]);

                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.alignment = TextAnchor.MiddleCenter;
                textStyle.normal.textColor = Color.white; // Change the text color if needed

                // Calculate the width of the text dynamically
                Vector2 textSize = textStyle.CalcSize(new GUIContent(questionText));
                float textWidth = textSize.x;
                float textHeight = textSize.y;

                // Adjust the position of the text to be below the image and centered
                Rect textRect = new Rect(questionRect.x + (questionRect.width - textWidth) / 2, bottomY + questionRect.height + 5, textWidth, textHeight); // Adjust the Y position and height as needed
                GUI.Label(textRect, questionText, textStyle);
            }



        }


        private void OnDestroy()
        {
            Cleanup();
        }
        
        private void OnDisable()
        {
            isRunning = false;
            Cleanup();
            EditorApplication.quitting -= OnEditorQuitting;
        }

        private void OnEditorQuitting()
        {
            // Ensure cleanup code is executed when the editor quits
            Cleanup();
        }

        private void Cleanup()
        {
            DiagenLayoutCommon.SaveSettings(settings, sessionStatesFilePath);
            //settings.llmServerProcess = DiagenSubsystem.StopLlamaServer(settings.llmServerProcess);
        }
        

        private async void ProcessingState()
        {
            while (isRunning)
            {
                if (processingFrames != null && processingFrames.Length > 0)
                {
                    currentFrame = (currentFrame + 1) % processingFrames.Length;
                    Repaint(); // Force the window to update
                }
                await Task.Delay((int)(frameTime * 1000)); // Wait without blocking the UI thread
            }
        }

        private async void MoveImage()
        {
            while (isRunning)
            {
                imageX += speed * 0.016f; // Move right (assuming ~60FPS)
                if (imageX > position.width) imageX = -100; // Reset when it moves out of bounds
                Repaint();
                await Task.Delay(16); // Roughly 60FPS
            }
        }

        private void LoadGifFrames(int frameCount, string type)
        {
            if (type == "waiting")
            {
                processingFrames = new Texture2D[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    processingFrames[i] = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Diagen/Images/waiting_{i}.png");
                }
            }
            else if (type == "error")
            {
                errorFrames = new Texture2D[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    errorFrames[i] = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Diagen/Images/error_{i}.png");
                }
            }
            else if (type == "question")
            {
                questionFrames = new Texture2D[frameCount];
                for (int i = 0; i < frameCount; i++)
                {
                    questionFrames[i] = AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/Diagen/Images/question_{i}.png");
                }
            }
        }
        
        // Method to set the isRunning state
        public void SetRunning(bool running)
        {
            isRunning = running;
            if (running)
            {
                // Restart animation coroutines if needed
                ProcessingState();
                MoveImage();
            }
        }
        
        // Method to set the error state
        public void SetError(bool error)
        {
            isError = error;
        }

        // Method to set the question state
        public void SetQuestion(bool question)
        {
            isQuestion = question;
        }
    }
}
#endif