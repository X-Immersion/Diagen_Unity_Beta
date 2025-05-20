// using System.Reflection.PortableExecutable;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System;
using System.IO;
using System.Text;
using TMPro;
using System.Threading;
using System.Linq;
using UnityEngine.UIElements;

// Define custom classes
using ArielCommonTypes;
using ArielVoiceRemote;
using ArielVoiceLocal;
using System.Threading.Tasks;

namespace ArielVoiceGen
{
    // code declarations
    public class ArielVoiceGeneration : EditorWindow
    {
        // Start server


        //Window size
        public static int minWidth = 810;
        public static int minHeight = 320;
        public static int maxWidth = 1000;
        public static int maxHeight = 1000;

        //Tab
        private int selectedMainTab = 0;
        private int selectedOptionTab = 0;
        string[] mainTabs = { "Generation", "Options" };
        string[] optionTabs = { "Glossary options",/* "Create new Glossary"*/};

        Vector2 scrollPos;
        Vector2 scrollPos2 = Vector2.zero;
        Vector2 scrollPos3 = Vector2.zero;

        //Start For Generation
        // public Dictionary<string, string[]> Options = new Dictionary<string, string[]>
        public SpeakerSettings speakerSettings = new SpeakerSettings();
        public string[] speakers = new string[0];
        // public string[] Languages = new string[]
        // {
        //     "Arabic", "Chinese (Mandarin)", "Danish", "Dutch", "English (Australian)", "English (British)", "English (Indian)", "English (New Zealand)", "English (South African)", "English (US)", "English (Welsh)", "French", "French (Canadian)", "German", "Hindi", "Icelandic", "Italian", "Japanese", "Korean", "Norwegian", "Polish", "Portuguese (Brazilian)", "Portuguese (European)", "Romanian", "Russian", "Spanish (European)", "Spanish (Mexican)", "Spanish (US)", "Swedish", "Turkish", "Welsh",
        // };

        public string[] languages = new string[0];

        public string[] Effects = new string[]
        {
            "none","phone", "cave", "gasmask", "smallcave", "nextroom", "badreception"
        };

        public bool ttsReady = true;
        public string buttonText = "Save to wav";

        private ArielTts _tts = new ArielTts(null, 0f, 1f, null, 0, 0, 0, null, 0, null);

        private List<ArielTts> _ttsList = new List<ArielTts>()
        {
            new ArielTts("", 0f, 1f, "", 0, 0, 0, null, 0, null)
        };

        private string lastSpeaker = "";

        public AudioClip son_pilou;

        public string savePath = null;

        private string textError;

        private bool localGeneration = false;

        private int howMuchElement = 1;

        ArielLocal arielLocal = new ArielLocal(); // If using the local server, you will need an instance of it, because of the server reference

        //Start For Option
        public ArielCsv _csv = new ArielCsv(null, null);

        public List<ArielCsv> _csvList = new List<ArielCsv>()
        {
            new ArielCsv("Example", "Ekzample")
        };

        // Edition and creation of glossary
        public string glossary;
        public string glossaryPath = "";
        public string newGlossaryName;

        // Use of glossary
        public string glossaryToUse;
        public string glossaryToUsePath;

        public bool isCreate;

        public string glossarySaveFolder = null;
        public string newGlossaryPath = null;

        // API Key Option
        public string ArielApiKey = "";
        private const string API_KEY_PREF = "ArielApiKey";

        //End For Option


        [MenuItem("Window/ArielVoiceGeneration", false, 1)]
        public static void ShowMyEditor()
        {
            EditorWindow wnd = GetWindow<ArielVoiceGeneration>();
            wnd.titleContent = new GUIContent("ArielVoiceGeneration");
            wnd.minSize = new Vector2(minWidth, minHeight);
            //wnd.maxSize = new Vector2(maxWidth, maxHeight);
        }

        public async Task Awake()
        {
            selectedMainTab = 0;
            selectedOptionTab = 0;
            isCreate = false;
            // Load the API key when the editor opens
            if (string.IsNullOrEmpty(ArielApiKey))
            {
                ArielApiKey = EditorPrefs.GetString(API_KEY_PREF, ""); // Load the saved key, default to an empty string
            }

            if (!string.IsNullOrEmpty(ArielApiKey))
            {
                speakerSettings = await ArielRemote.GetSpeakers(ArielApiKey);
            }
            else
            {
                UnityEngine.Debug.LogError("API Key is not set. Please enter a valid API Key in the editor.");
            }

            // Start local server
            arielLocal.StartServer();

        }

        //Called when click on Add button
        public void AddElement(List<ArielTts> ttsList)
        {
            howMuchElement += 1;
            ttsList.Add(new ArielTts(null, 0f, 1f, null, 0, 9, 0, null, 0, null));  //try with null insted of ""
        }
        //Called when click on Remove button
        public void RemoveElement(List<ArielTts> ttsList, int position)
        {
            if (howMuchElement > 1)
            {
                howMuchElement -= 1;
                ttsList.RemoveAt(position);
            }
        }

        private void RemoveFromCSV(List<ArielCsv> csvList, int position)
        {
            //Remove a specific line from the CSV file (add cross at the end of all the lines who trigger the function)
            csvList.RemoveAt(position);

        }

        private void AddToCSV(List<ArielCsv> csvList)
        {
            //Add a new line to the CSV file
            csvList.Add(new ArielCommonTypes.ArielCsv("", ""));

        }


        //Display every frame
        public void OnGUI()
        {
            selectedMainTab = GUILayout.Toolbar(selectedMainTab, mainTabs, GUILayout.Width(200));

            if (selectedMainTab == 0) // 0 = Generation
            {
                GUILayout.Space(20);
                LayoutItem(_tts, _ttsList);
            }
            if (selectedMainTab == 1) // 1 = Glossary
            {
                selectedOptionTab = GUILayout.Toolbar(selectedOptionTab, optionTabs, GUILayout.Width(600));
                GUILayout.Label("If you want to change the pronunciation of some words, we advise you to use a glossary to save your changes. \n" +
                "Create and modify csv files. Don't forget to import the glossary csv file in the 'generation' window.");

                if (selectedOptionTab == 0)  // 0 = Edit 
                {
                    GUILayout.Space(20);
                    OptionEdit(_csv, _csvList);
                }
                if (selectedOptionTab == 1)  // 1 = Create
                {
                    GUILayout.Space(20);
                    OptionCreate(_csvList);
                }
            }
        }

        public void EditCSV(string filename, List<ArielCsv> csvList)
        {
            if (selectedOptionTab == 0)  // 0 = Edit 
            {
                if (csvList.Count > 0)
                {
                    TextWriter tw = new StreamWriter(filename, false);
                    tw.Write("word, pronunciation");
                    tw.Close();

                    tw = new StreamWriter(filename, true);

                    for (int i = 0; i < csvList.Count; i++)
                    {
                        tw.Write("\n" + csvList[i].Word + "," + csvList[i].Pronunciation);
                    }

                    tw.Close();

                }
            }
            if (selectedOptionTab == 1)  // 1 = Create
            {
                if (newGlossaryName != null && newGlossaryName != "")
                {
                    glossarySaveFolder = EditorUtility.OpenFolderPanel("Select Folder", "", "");

                    newGlossaryPath = glossarySaveFolder + "/" + newGlossaryName + ".csv";
                    glossaryPath = newGlossaryPath; //For Edit tab
                    TextWriter tw = new StreamWriter(newGlossaryPath, false);
                    tw.Write("word, pronunciation");
                    tw.Close();

                    tw = new StreamWriter(newGlossaryPath, true);

                    for (int i = 0; i < csvList.Count; i++)
                    {
                        tw.Write("\n" + csvList[i].Word + "," + csvList[i].Pronunciation);
                    }

                    tw.Close();

                }
            }

        }

        public void ReadCSV(string glossary, List<ArielCsv> csvList)
        {
            UnityEngine.Debug.Log("Read");
            string[] data = glossary.Split(new string[] { ",", "\n" }, StringSplitOptions.None);
            UnityEngine.Debug.Log("Read ok");

            int tableSize = data.Length / 2 - 1;

            //Debug.Log("table size is : " + tableSize);

            for (int i = 0; i < tableSize; i++)
            {
                csvList.Add(new ArielCsv("", ""));
                csvList[i].Word = (data[2 * (i + 1)]);
                csvList[i].Pronunciation = data[2 * (i + 1) + 1];
            }

            UnityEngine.Debug.Log(glossary);
        }

        public void ImportCSVdiagen(string phraseList, List<ArielTts> voiceChannelList)
        {
            UnityEngine.Debug.Log("Read");
            string[] data = phraseList.Split(new string[] { "~", "\n" }, StringSplitOptions.None);
            UnityEngine.Debug.Log("Read ok");

            int tableSize = data.Length / 2 - 8;
            howMuchElement = 1;
            voiceChannelList[0].Phrase = data[16 + 2];
            for (int i = 1; i < tableSize; i++)
            {
                AddElement(voiceChannelList);
                voiceChannelList[i].Phrase = data[16 + 2 * (i + 1)];

            }

        }

        public void ImportCSV(string phraseList, List<ArielTts> voiceChannelList)
        {
            UnityEngine.Debug.Log("Read");
            //string[] data = phraseList.Split(new string[] { "\\", "\n" }, StringSplitOptions.None);
            string[] data = phraseList.Split(new string[] { "\n" }, StringSplitOptions.None);
            UnityEngine.Debug.Log("Read ok");

            int tableSize = data.Length - 1;
            howMuchElement = 1;
            voiceChannelList[0].Phrase = data[0];
            for (int i = 1; i < tableSize; i++)
            {
                AddElement(voiceChannelList);
                voiceChannelList[i].Phrase = data[i];

            }

        }

        public string CompareSentenceToGlossary(string sentence, string glossaryPath)
        {
            glossaryToUse = File.ReadAllText(glossaryPath);

            string[] data = glossaryToUse.Split(new string[] { ",", "\n" }, StringSplitOptions.None);

            int tableSize = data.Length / 2 - 1;

            //Debug.Log("table size is : " + tableSize);

            for (int i = 0; i < tableSize; i++)
            {
                if (data[2 * (i + 1)] != null && data[2 * (i + 1) + 1] != null && data[2 * (i + 1)] != "" && data[2 * (i + 1) + 1] != "")
                    sentence = sentence.Replace(data[2 * (i + 1)], data[2 * (i + 1) + 1]);
                //Debug.Log("Data " + i + " is : " + data[2 * (i + 1)]);
            }

            return sentence;
        }

        // Custom Color for GUI background
        private GUIStyle BackgroundCustomColor(Color color)
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.SetPixel(0, 0, color);
            texture.Apply();
            GUIStyle style = new GUIStyle();
            style.normal.background = texture;
            return style;
        }

        // The OptionCreate function is deprecated
        private void OptionCreate(List<ArielCsv> csvList)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("New Glossary", GUILayout.Width(minWidth / 3)))
            {
                isCreate = true;
                glossaryPath = null;
                csvList.Clear();
                csvList.Add(new ArielCsv("", ""));
            }


            EditorGUIUtility.labelWidth = 130;
            //EditorGUILayout.LabelField(label: "CSV file: ", Path.GetFileNameWithoutExtension(glossaryPath), EditorStyles.wordWrappedLabel);
            newGlossaryName = EditorGUILayout.TextField(new GUIContent("Glossary name: "), newGlossaryName, GUI.skin.textArea, GUILayout.Width(300), GUILayout.Height(18));


            GUILayout.EndHorizontal();

            GUILayout.BeginVertical();

            //CSV content (Word, Pronunciation)
            for (int i = 0; i < csvList.Count; i++)
            {
                GUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 90f;
                csvList[i].Word = EditorGUILayout.TextField(new GUIContent("Word"), csvList[i].Word, GUI.skin.textArea, GUILayout.Width(310), GUILayout.Height(18));
                EditorGUIUtility.labelWidth = 90f;
                csvList[i].Pronunciation = EditorGUILayout.TextField(new GUIContent("Pronunciation"), csvList[i].Pronunciation, GUI.skin.textArea, GUILayout.Width(310), GUILayout.Height(18));
                if (GUILayout.Button("X", GUILayout.Width(30), GUILayout.Height(20)))
                {
                    RemoveFromCSV(csvList, i);
                }
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("+", GUILayout.Width(20)))
            {
                AddToCSV(csvList);
            }

            if (GUILayout.Button("Save", GUILayout.Width(minWidth / 3)))
            {
                EditCSV(glossaryPath, csvList);
            }

            GUILayout.EndVertical();
        }

        // Main Glossary function
        private void OptionEdit(ArielCsv csv, List<ArielCsv> csvList)
        {
            GUILayout.BeginHorizontal();

            EditorGUIUtility.labelWidth = 130;
            EditorGUILayout.LabelField(label: "Current CSV file: ", Path.GetFileNameWithoutExtension(glossaryPath), EditorStyles.wordWrappedLabel);

            GUILayout.EndHorizontal();

            GUILayout.BeginVertical(BackgroundCustomColor(new Color(0.5f, 0.5f, 0.5f, 0.25f)));

            scrollPos2 = EditorGUILayout.BeginScrollView(scrollPos2, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            //CSV content (Word, Pronunciation)
            for (int i = 0; i < csvList.Count; i++)
            {
                GUILayout.BeginHorizontal();
                EditorGUIUtility.labelWidth = 90f;
                csvList[i].Word = EditorGUILayout.TextField(new GUIContent("Word"), csvList[i].Word, GUI.skin.textArea, GUILayout.Width(310), GUILayout.Height(18));
                EditorGUIUtility.labelWidth = 90f;
                csvList[i].Pronunciation = EditorGUILayout.TextField(new GUIContent("Pronunciation"), csvList[i].Pronunciation, GUI.skin.textArea, GUILayout.Width(310), GUILayout.Height(18));
                if (GUILayout.Button("X", GUILayout.Width(30), GUILayout.Height(20)))
                {
                    RemoveFromCSV(csvList, i);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add word", GUILayout.Height(35)))
            {
                AddToCSV(csvList);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10f);

            // Glossary buttons
            GUILayout.BeginHorizontal();

            //Left Column
            GUILayout.BeginVertical();

            if (GUILayout.Button("Save", GUILayout.Width(EditorGUIUtility.currentViewWidth / 2), GUILayout.Height(45)))
            {
                if (Path.GetFileNameWithoutExtension(glossaryPath) != null && Path.GetFileNameWithoutExtension(glossaryPath) != "")
                {
                    EditCSV(glossaryPath, csvList);
                    glossary = Path.GetFileNameWithoutExtension(glossaryPath);
                }
                else
                {
                    var path = EditorUtility.SaveFilePanel("Save glossary as", "", "glossary.csv", "csv");
                    EditCSV(path, csvList);
                    glossary = Path.GetFileNameWithoutExtension(path);
                }
            }

            if (GUILayout.Button("Save as new", GUILayout.Width(EditorGUIUtility.currentViewWidth / 2), GUILayout.Height(45)))
            {
                var path = "";

                if (glossaryPath != "" && glossaryPath != null)
                {
                    path = EditorUtility.SaveFilePanel("Save glossary as", "", Path.GetFileNameWithoutExtension(glossaryPath) + ".csv", "csv");
                    glossaryPath = path;
                }
                else
                {
                    path = EditorUtility.SaveFilePanel("Save glossary as", "", "glossary.csv", "csv");
                    glossaryPath = path;
                }
                EditCSV(path, csvList);
            }
            GUILayout.EndVertical();

            // Right Column
            GUILayout.BeginVertical();
            //Import CSV Button
            if (GUILayout.Button("Import CSV file", GUILayout.Width(EditorGUIUtility.currentViewWidth / 2), GUILayout.Height(45)))
            {
                newGlossaryName = "";
                glossaryPath = EditorUtility.OpenFilePanel("Open CSV File", "", "csv");
                if (glossaryPath != null && glossaryPath != "")
                {
                    csvList.Clear();
                    glossary = File.ReadAllText(glossaryPath);
                    //UnityEngine.Debug.Log(glossary);
                    ReadCSV(glossary, csvList);
                    //isGlossary = true;
                }
                else
                {
                    UnityEngine.Debug.LogError("No selected file");
                }

            }

            if (GUILayout.Button("Create empty Glossary", GUILayout.Width(EditorGUIUtility.currentViewWidth / 2), GUILayout.Height(45)))
            {
                isCreate = true;
                glossaryPath = null;
                csvList.Clear();
                csvList.Add(new ArielCsv("", ""));
            }
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
        }


        //Main Window
        private async void LayoutItem(ArielTts tts, List<ArielTts> ttsList)
        {
            if (tts == null)
            {
                return;
            }
            GUILayout.BeginHorizontal(); // Begin a horizontal group
            GUIContent apiKeyContent = new GUIContent("API Key: ", "Enter your API key here.");
            EditorGUIUtility.labelWidth = 105f;
            ArielApiKey = EditorGUILayout.TextField("API Key: ", ArielApiKey, GUI.skin.textArea, GUILayout.MinWidth(120));

            if (GUILayout.Button("Fetch Speakers", GUILayout.Width(120))) // Add the button beside the text field
            {
                if (!string.IsNullOrEmpty(ArielApiKey) && localGeneration == false)
                {
                    EditorPrefs.SetString(API_KEY_PREF, ArielApiKey); // Save the API key
                    speakerSettings = await ArielRemote.GetSpeakers(ArielApiKey); // Call GetSpeakers with the API key
                }
                else
                {
                    UnityEngine.Debug.LogError("Please enter a valid API Key before fetching speakers. Switching to local version.");

                    // Set local execution to true
                    localGeneration = true;
                    speakerSettings = arielLocal.GetSpeakers();

                }
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(15f);

            scrollPos3 = EditorGUILayout.BeginScrollView(scrollPos3, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true)/*, GUILayout.MinHeight(210f)*/);
            //Text field with all the parameters for voice generation
            for (int i = 0; i < howMuchElement; i++)
            {
                GUILayout.BeginHorizontal(BackgroundCustomColor(new Color(0.5f, 0.5f, 0.5f, 0.25f)));

                // Left Column (Filename + Text area)
                GUILayout.BeginVertical();
                EditorGUIUtility.labelWidth = 105f;
                ttsList[i].Title = EditorGUILayout.TextField("File name", ttsList[i].Title, GUI.skin.textArea, GUILayout.MinWidth(360));
                GUILayout.Space(5f);
                EditorGUIUtility.labelWidth = 105f;
                ttsList[i].Phrase = EditorGUILayout.TextField(new GUIContent("Text to Speech"), ttsList[i].Phrase, GUI.skin.textArea, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true), GUILayout.MinWidth(310), GUILayout.MinHeight(50), GUILayout.MaxHeight(120));//, GUILayout.Width(300), GUILayout.Height(40));
                GUILayout.Label("The sentence should be about 5 to 50 words long");
                GUILayout.EndVertical();

                GUILayout.Space(5f);

                // Settings Column
                GUILayout.BeginVertical();
                EditorGUIUtility.labelWidth = 70f;
                int previousSelectedO = ttsList[i].Selected_O;
                int previousSelectedL = ttsList[i].Selected_L;

                speakers = new string[speakerSettings.speakers.Length];

                for (int j = 0; j < speakerSettings.speakers.Length; j++)
                {
                    speakers[j] = speakerSettings.speakers[j].name;
                }

                ttsList[i].Selected_O = EditorGUILayout.Popup("Voice", ttsList[i].Selected_O, speakers, GUILayout.Width(228));
                if (previousSelectedO != ttsList[i].Selected_O)
                {
                    // Handle the change in selection here
                    UnityEngine.Debug.Log($"Voice selection changed from {speakerSettings.speakers[previousSelectedO]} to {speakerSettings.speakers[ttsList[i].Selected_O]}");
                }

                if (speakerSettings.speakers != null && speakerSettings.speakers.Length > 0)
                {
                    languages = speakerSettings.speakers[ttsList[i].Selected_O].language.ToArray();
                }
                else
                {
                    languages = new string[0];
                }

                ttsList[i].Selected_L = EditorGUILayout.Popup("Language", ttsList[i].Selected_L, languages, GUILayout.Width(228));
                if (previousSelectedL != ttsList[i].Selected_L)
                {
                    // Handle the change in selection here
                    UnityEngine.Debug.Log($"Language selection changed from {speakerSettings.speakers[ttsList[i].Selected_O].language[previousSelectedL]} to {speakerSettings.speakers[ttsList[i].Selected_O].language[ttsList[i].Selected_L]}");
                }
                EditorGUIUtility.labelWidth = 50f;

                // Store the previous value of localGeneration
                bool previousLocalGeneration = localGeneration;

                // Checkbox for local Execution
                localGeneration = GUILayout.Toggle(localGeneration, new GUIContent("Local Generation"), GUILayout.Width(200));

                // Check if the value has changed
                if (localGeneration != previousLocalGeneration)
                {
                    // set ttsList[i].Selected_O and ttsList[i].Selected_L to the default values
                    ttsList[i].Selected_O = 0;
                    ttsList[i].Selected_L = 0;

                    // Get speakers
                    if (localGeneration == false)
                    {
                        speakerSettings = await ArielRemote.GetSpeakers(ArielApiKey);
                        UnityEngine.Debug.Log("Remote Generation");
                    }
                    else
                    {
                        speakerSettings = arielLocal.GetSpeakers();
                        UnityEngine.Debug.Log("Local Generation");
                    }
                }

                GUILayout.Space(10f);
                EditorGUIUtility.labelWidth = 50f;
                ttsList[i].Volume = EditorGUILayout.Slider(new GUIContent("Volume"), ttsList[i].Volume, -32f, 32f, GUILayout.Width(200));
                EditorGUIUtility.labelWidth = 50f;
                ttsList[i].Octave = EditorGUILayout.Slider(new GUIContent("Pitch"), ttsList[i].Octave, -1f, 1f, GUILayout.Width(200));
                EditorGUIUtility.labelWidth = 50f;
                ttsList[i].Speed = EditorGUILayout.Slider(new GUIContent("Speed"), ttsList[i].Speed, 0.5f, 4f, GUILayout.Width(200));
                EditorGUIUtility.labelWidth = 70f;

                ttsList[i].AdvancedOptions = EditorGUILayout.Foldout(ttsList[i].AdvancedOptions, "Advanced Options", true);
                if (ttsList[i].AdvancedOptions)
                {
                    GUILayout.BeginVertical(GUILayout.MaxWidth(200f));
                    EditorGUIUtility.labelWidth = 50f;
                    
                    // Dont show, if local generation is selected: Effects are not available in local execution
                    if (!localGeneration)
                    {
                        ttsList[i].Effect = EditorGUILayout.Popup("Effect", ttsList[i].Effect, Effects, GUILayout.Width(200));
                        EditorGUIUtility.labelWidth = 50f;
                        ttsList[i].isStereo = GUILayout.Toggle(ttsList[i].isStereo, "Stereo Audio");
                        EditorGUIUtility.labelWidth = 50f;
                        ttsList[i].isHSRate = GUILayout.Toggle(ttsList[i].isHSRate, "High Sample Rate");
                        EditorGUIUtility.labelWidth = 50f;
                        ttsList[i].useVoiceImprovement = GUILayout.Toggle(ttsList[i].useVoiceImprovement, "Voice Improvement");
                        GUILayout.Space(10f);
                    }
                    EditorGUIUtility.labelWidth = 50f;
                    ttsList[i].useGlossary = GUILayout.Toggle(ttsList[i].useGlossary, "Use glossary");
                    EditorGUIUtility.labelWidth = 50f;
                    if (GUILayout.Button("Select glossary", GUILayout.Width(100)))
                    {
                        ttsList[i].glossaryToUsePath = EditorUtility.OpenFilePanel("Select Glossary to use", "", "csv");
                    }
                    EditorGUIUtility.labelWidth = 50f;
                    GUILayout.Label("Current glossary : " + Path.GetFileName(ttsList[i].glossaryToUsePath));
                    GUILayout.Space(10f);
                    GUILayout.EndVertical();
                }
                GUILayout.Space(10f);
                GUILayout.EndVertical();

                // Close Button
                GUILayout.BeginVertical(GUILayout.Width(80));
                GUILayout.Space(50f);
                if (GUILayout.Button("Remove\nChannel", GUILayout.Width(65), GUILayout.Height(40)))
                {
                    RemoveElement(ttsList, i);
                }
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();

                GUILayout.Space(10f);
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Add new voice channel", GUILayout.Height(45)))
            {
                AddElement(ttsList);
            }

            GUILayout.EndHorizontal();


            GUILayout.Space(20);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Select output folder", GUILayout.Width(EditorGUIUtility.currentViewWidth / 2)))
            {
                savePath = EditorUtility.OpenFolderPanel("Select Output Folder", "", "");
            }
            //GUILayout.Label(savePath, EditorStyles.wordWrappedLabel);
            EditorGUIUtility.labelWidth = 100f;
            EditorGUILayout.LabelField(label: "Output Path: ", savePath, EditorStyles.wordWrappedLabel);
            GUILayout.EndHorizontal();


            // GUILayout.BeginVertical();
            if (GUILayout.Button("Import CSV sentence list", GUILayout.Width(EditorGUIUtility.currentViewWidth / 2), GUILayout.Height(20)))
            {
                var path = "";
                string documentToImport;
                path = EditorUtility.OpenFilePanel("Dialogue document", "", "csv");

                if (path != null && path != "")
                {
                    documentToImport = File.ReadAllText(path);
                    ImportCSV(documentToImport, ttsList);
                }
            }
            //GUILayout.Space(10);

            GUI.enabled = ttsReady;
            if (GUILayout.Button(buttonText, GUILayout.Width(EditorGUIUtility.currentViewWidth / 2), GUILayout.Height(60)))
            {
                if (savePath != null && savePath != "")
                {
                    for (int i = 0; i < howMuchElement; i++)
                    {
                        if (lastSpeaker != speakerSettings.speakers[ttsList[i].Selected_O].name)
                        {
                            // Restart Server
                            if (localGeneration)
                            {
                                arielLocal.RestartServer();
                                lastSpeaker = speakerSettings.speakers[ttsList[i].Selected_O].name;
                            }
                        }
                        string phraseToGenerate = ttsList[i].Phrase;
                        string effectToUse = Effects[ttsList[i].Effect];
                        if (ttsList[i].useGlossary && ttsList[i].glossaryToUsePath != null && ttsList[i].glossaryToUsePath != "")
                        {
                            phraseToGenerate = CompareSentenceToGlossary(ttsList[i].Phrase, ttsList[i].glossaryToUsePath);
                        }

                        if (ttsList[i].isStereo)
                        {
                            ttsList[i].MonoStereo = "true";
                        }
                        else
                        {
                            ttsList[i].MonoStereo = "false";
                        }

                        if (ttsList[i].isHSRate)
                        {
                            ttsList[i].HighSampleRate = "true";
                        }
                        else
                        {
                            ttsList[i].HighSampleRate = "false";
                        }

                        if (ttsList[i].useVoiceImprovement)
                        {
                            ttsList[i].VoiceImprovement = "true";
                        }
                        else
                        {
                            ttsList[i].VoiceImprovement = "false";
                        }

                        if (ttsList[0].Effect == 0)
                        {
                            effectToUse = " ";
                        }
                        UnityEngine.Debug.Log("Settings defined");
                        UnityEngine.Debug.Log("ttsList: " + ttsList.ToString());
                        UnityEngine.Debug.Log($"API Option 2: {speakerSettings}");
                        UnityEngine.Debug.Log($"Phrase to Generate: {phraseToGenerate}");
                        UnityEngine.Debug.Log($"Title: {ttsList[i].Title}");
                        UnityEngine.Debug.Log($"Octave: {ttsList[i].Octave}");
                        UnityEngine.Debug.Log($"Speed: {ttsList[i].Speed}");
                        UnityEngine.Debug.Log($"Effect to Use: {effectToUse}");
                        UnityEngine.Debug.Log($"Mono/Stereo: {ttsList[i].MonoStereo}");
                        UnityEngine.Debug.Log($"Volume: {ttsList[i].Volume}");
                        UnityEngine.Debug.Log($"High Sample Rate: {ttsList[i].HighSampleRate}");
                        UnityEngine.Debug.Log($"Voice Improvement: {ttsList[i].VoiceImprovement}");
                        UnityEngine.Debug.Log($"Save Path: {savePath}");
                        UnityEngine.Debug.Log($"API Key: {ArielApiKey}");
                        UnityEngine.Debug.Log($"Position: {i}");

                        // arielRemote.TextToAudio(Options[ttsList[i].Selected_L][ttsList[i].Selected_O], phraseToGenerate, ttsList[i].Title, ttsList[i].Octave, ttsList[i].Speed, effectToUse, ttsList[i].MonoStereo, ttsList[i].Volume, ttsList[i].HighSampleRate, ttsList[i].VoiceImprovement, savePath, ArielApiKey, i);
                        buttonText = "Generating Audio File...";
                        ttsReady = false;
                        if (!localGeneration)
                        {
                            UnityEngine.Debug.Log("Generating Audio with remote server");
                            ttsReady = await ArielRemote.TextToAudio(speakerSettings.speakers[ttsList[i].Selected_O].name, phraseToGenerate, ttsList[i].Title, ttsList[i].Octave, ttsList[i].Speed, effectToUse, ttsList[i].MonoStereo, ttsList[i].Volume, ttsList[i].HighSampleRate, ttsList[i].VoiceImprovement, savePath, ArielApiKey, i);
                        }
                        else
                        {
                            UnityEngine.Debug.Log("Generating Audio with local server");
                            ttsReady = await arielLocal.TextToAudio(speakerSettings.speakers[ttsList[i].Selected_O].name, phraseToGenerate, ttsList[i].Title, ttsList[i].Octave, ttsList[i].Speed, effectToUse, ttsList[i].MonoStereo, ttsList[i].Volume, ttsList[i].HighSampleRate, ttsList[i].VoiceImprovement, savePath, ArielApiKey, i);
                        }
                        buttonText = "Save to wav";
                        UnityEngine.Debug.Log("TextToAudio finished");
                        Repaint();
                    }
                }
                else
                {
                    textError = "Error : No Output Folder Selected";
                }
            }

            if (textError != null && textError != "")
            {
                var TextStyle = new GUIStyle();
                TextStyle.normal.textColor = Color.red;
                GUILayout.Label(textError, TextStyle);

                // Start a coroutine to clear the error message after 5 seconds
                EditorApplication.delayCall += async () =>
                {
                    await Task.Delay(5000); // Delay for 5 seconds
                    textError = "";
                    Repaint(); // Repaint the editor window to update the UI
                };
            }

        }

        public void OnDisable()
        {
            // Shutdown the local server
            arielLocal.ShutdownServer();
            UnityEngine.Debug.Log("Local server stopped successfully");
        }
    }

}
