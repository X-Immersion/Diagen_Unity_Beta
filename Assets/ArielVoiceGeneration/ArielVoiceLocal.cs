#if UNITY_EDITOR
    using UnityEditor;
#endif

using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.UIElements;

// Define custom classes
using ArielCommonTypes;
using ArielVoiceLocal;
using Unity.VisualScripting;

namespace ArielVoiceLocal
{
    /// <summary>
    /// Manages the local Ariel server and handles text-to-speech conversion.
    /// </summary>
    public class ArielLocal
    {
        public string url = "http://localhost:";
        public string processName = "piper_server";
        public int port = 8003;
        private Process process;
        /// <summary>
        /// Starts the local Ariel server.
        /// </summary>
        /// <returns>True if the server started successfully; otherwise, false.</returns>
        public bool StartServer(bool logs = false)
        {
            ServerState serverState = CheckServiceRunningOnPort(port);

            // if serverState portInUse == true and arielServerProcessRunning == true --> force restart server
            if (serverState.portInUse && serverState.arielServerProcessRunning)
            {
                KillProcessOnPort(port);
                if (logs) UnityEngine.Debug.LogWarning($"Old Ariel Server process on port {port} was killed.");
            }
            // if serverState portInUse == true and arielServerProcessRunning == false --> increase port number until free port can be found
            else if (serverState.portInUse && !serverState.arielServerProcessRunning)
            {
                int retries = 0;
                port++;
                while (CheckServiceRunningOnPort(port).portInUse || retries > 10) // added retry limit to avoid infinite loop
                {
                    port++;
                    retries++; // increment retries
                }
                if (logs) UnityEngine.Debug.LogWarning($"Port {port} is in use. Using port {port} instead.");
            }
            else if (!serverState.portInUse && !serverState.arielServerProcessRunning)
            {
                if (logs) UnityEngine.Debug.Log($"Port {port} is free.");
            }

            string pluginFolderPath = Path.GetFullPath("Assets/ArielVoiceGeneration/Local/piper/piper_server.exe");

            ProcessStartInfo start = new ProcessStartInfo
            {
                FileName = pluginFolderPath,
                Arguments = $"--port {port}",
                WorkingDirectory = Path.GetFullPath("Assets/ArielVoiceGeneration/Local"),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };


            process = new Process
            {
                StartInfo = start
            };

            // If successful startup, return true
            try
            {
                if (process.Start())
                {
                    if (logs) UnityEngine.Debug.Log("Server started successfully");
                    return true;
                }
            }
            catch (Exception ex)
            {
                if (logs) UnityEngine.Debug.LogError($"Server could not be started: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Shuts down the local Ariel server.
        /// </summary>
        /// <returns>True if the server was stopped successfully; otherwise, false.</returns>
        public bool ShutdownServer(bool logs = false)
        {
            if (process == null)
            {
                if (logs) UnityEngine.Debug.LogError("Server is not running");
                return false;
            }
            else
            {
                process.Kill();
                process.WaitForExit();
                if (logs) UnityEngine.Debug.Log("Server stopped successfully");
                return true;
            }
        }

        /// <summary>
        /// Restarts the local Ariel server.
        /// </summary>
        /// <returns>True if the server was restarted successfully; otherwise, false.</returns>
        public bool RestartServer()
        {
            ShutdownServer();
            return StartServer();
        }

        /// <summary>
        /// Forces the restart of the local Ariel server.
        /// </summary>
        /// <returns>True if the server was restarted successfully; otherwise, false.</returns>
        public bool ForceRestartServer()
        {
            if (KillProcessOnPort(port))
            {
                return StartServer();
            }
            return false;
        }

        /// <summary>
        /// Checks if the local Ariel server is running.
        /// </summary>
        /// <returns>True if the server is running; otherwise, false.</returns>
        public bool IsServerRunning()
        {
            if (process == null)
            {
                return false;
            }
            return !process.HasExited;
        }

        /// <summary>
        /// Attempts to kill the process that is using the specified port.
        /// </summary>
        /// <param name="killPort">The port number to check for an active process.</param>
        /// <returns>True if the process was successfully killed; otherwise, false.</returns>
        /// <exception cref="Exception">Logs an error message if an exception occurs during the process.</exception>
        private bool KillProcessOnPort(int killPort, bool logs = false)
        {
            try
            {
                // Find the process ID (PID) using netstat
                Process netstatProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c netstat -ano | findstr :{killPort}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                netstatProcess.Start();
                string output = netstatProcess.StandardOutput.ReadToEnd();
                netstatProcess.WaitForExit();

                // Extract the PID from the netstat output
                string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    string[] parts = lines[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 4)
                    {
                        string pid = parts[4];

                        // Kill the process using taskkill
                        Process taskkillProcess = new Process
                        {
                            StartInfo = new ProcessStartInfo
                            {
                                FileName = "cmd.exe",
                                Arguments = $"/c taskkill /PID {pid} /F",
                                RedirectStandardOutput = true,
                                UseShellExecute = false,
                                CreateNoWindow = true
                            }
                        };

                        taskkillProcess.Start();
                        taskkillProcess.WaitForExit();
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                if (logs) UnityEngine.Debug.LogError($"Error shutting down server: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Checks if a service is running on the specified port by using the netstat command to find the process ID (PID).
        /// </summary>
        /// <param name="checkPort">The port number to check for a running service.</param>
        /// <param name="logs">Indicates whether to log messages. Default is false.</param>
        /// <returns>A ServerState object indicating the status of the port and server process.</returns>
        /// <remarks>
        /// This method uses the netstat command to find the PID of the process using the specified port.
        /// It then attempts to retrieve the process name associated with the PID and checks if it matches the expected server process name.
        /// If the process name matches, it logs that the server is already running and returns (false, true).
        /// If the process name does not match, it logs a warning and returns (true, false).
        /// If any errors occur during the process, it logs the error and returns (false, false).
        /// </remarks>
        private ServerState CheckServiceRunningOnPort(int checkPort, bool logs = false)
        {
            // define empty state object
            ServerState state = new ServerState
            {
                portInUse = true,
                arielServerProcessRunning = false
            };

            try
            {
                // Find the process ID (PID) using netstat
                Process netstatProcess = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c netstat -ano | findstr :{checkPort}",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                netstatProcess.Start();
                string output = netstatProcess.StandardOutput.ReadToEnd();
                netstatProcess.WaitForExit();

                // Extract the PID from the netstat output
                string[] lines = output.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in lines)
                {
                    string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 4)
                    {
                        string pidString = parts[^1]; // PID is usually the last column
                        if (int.TryParse(pidString, out int pid))
                        {
                            try
                            {
                                Process process = Process.GetProcessById(pid);
                                if (logs) UnityEngine.Debug.Log($"Process on port {checkPort}: {process.ProcessName} (PID: {pid})");
                                if (process.ProcessName == processName)
                                {
                                    if (logs) UnityEngine.Debug.Log($"Ariel Server is already running on port {checkPort} - restarting!");
                                    state.arielServerProcessRunning = true;
                                    state.portInUse = true;
                                    return state;
                                }
                                else
                                {
                                    if (logs) UnityEngine.Debug.LogWarning($"Process on port {checkPort} is not the expected server process: {process.ProcessName}");
                                    state.arielServerProcessRunning = false;
                                    state.portInUse = true;
                                    return state;
                                }
                            }
                            catch (Exception ex)
                            {
                                if (logs) UnityEngine.Debug.LogError($"Error retrieving process name for PID {pid}: {ex.Message}");
                            }
                        }
                    }
                }
                state.arielServerProcessRunning = false;
                state.portInUse = false;
                return state;
            }
            catch (Exception ex)
            {
                if (logs) UnityEngine.Debug.LogError($"Error checking server status: {ex.Message}");
                return state;
            }
        }


        /// <summary>
        /// Retrieves the available speakers from the local server.
        /// </summary>
        /// <returns>A SpeakerSettings object containing the available speakers and languages.</returns>
        public SpeakerSettings GetSpeakers(bool logs = false)
        {
            SpeakerSettings settings = new SpeakerSettings() { };
            LanguageCodes languageCodes = new LanguageCodes();

            // Check in Plugin Folder / ArielVoiceGeneration / Editor / VoiceSynthesis / Local / models which files exist
            string[] filesJson = Directory.GetFiles("Assets/ArielVoiceGeneration/Local/models", "*.json");
            string[] filesModels = Directory.GetFiles("Assets/ArielVoiceGeneration/Local/models", "*.onnx");
            Speaker[] speakers = new Speaker[filesJson.Length];
            List<string> speakerLanguage = new List<string>();
            // Validate if for each speaker also a model exists
            for (int i = 0; i < filesJson.Length; i++)
            {
                string speaker = Path.GetFileNameWithoutExtension(filesJson[i]).Split(new string[] { ".onnx" }, StringSplitOptions.None)[0];
                string model = Path.GetFileNameWithoutExtension(filesModels[i]);
                if (speaker == model)
                {
                    // Open onnx.json file, load json and read language.name_english
                    string json = File.ReadAllText(filesJson[i]);
                    dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);
                    speakerLanguage.Add((string)jsonObj["language"]["name_english"]);

                    Speaker newSpeaker = new Speaker();
                    if (logs) UnityEngine.Debug.Log("Speaker object created successfully.");

                    newSpeaker.name = speaker; // Assuming speakerName is a string
                    newSpeaker.language = new List<string>(speakerLanguage); // Assuming speakerLanguage is a List<string>

                    speakers[i] = newSpeaker;
                    if (logs) UnityEngine.Debug.Log("Speaker object added to speakers array.");

                }
                else
                {
                    if (logs) UnityEngine.Debug.LogError($"Speaker {speaker} has no model file.");
                }
            }

            if (logs) UnityEngine.Debug.Log("Speakers: " + speakers.Length);

            Language[] allLanguages = new Language[0];
            foreach (var speaker in speakers)
            {
                // Define the speaker object
                foreach (var lang in speaker.language)
                {
                    var language = languageCodes.languages.FirstOrDefault(l => l.name == lang);
                    if (language != null)
                    {
                        allLanguages = allLanguages.Concat(new[] { language }).ToArray();
                    }
                }
            }

            // remove duplicates in allLanguages in array
            allLanguages = allLanguages.Distinct().ToArray();

            settings.speakers = speakers;
            settings.languages = allLanguages;

            return settings;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Converts text to audio and saves the audio file to disk in Editor Mode.
        /// </summary>
        /// <param name="option">The voice option to use.</param>
        /// <param name="phrase">The text to convert to audio.</param>
        /// <param name="title">The title of the audio file.</param>
        /// <param name="octave">The pitch of the audio.</param>
        /// <param name="speed">The speed of the audio.</param>
        /// <param name="effect">The audio effect to apply.</param>
        /// <param name="monostereo">The mono/stereo setting.</param>
        /// <param name="volume">The volume of the audio.</param>
        /// <param name="highSampleRate">The high sample rate setting.</param>
        /// <param name="voiceImprovement">The voice improvement setting.</param>
        /// <param name="savePath">The path to save the audio file.</param>
        /// <param name="apiKey">The API key required to access the Ariel API.</param>
        /// <param name="position">The position of the text in the list.</param>
        /// <param name="logs">Indicates whether to log messages. Default is false.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
        public async Task<bool> TextToAudio(
            string option, string phrase, string title, float octave = 0.0f, float speed = 1.0f, string effect = "",
            string monostereo = "false", float volume = 0.0f, string highSampleRate = "false", string voiceImprovement = "false", 
            string savePath = "", string apiKey = "", int position = 0, bool logs = false)
        {
            if (string.IsNullOrEmpty(apiKey))  // Actually apiKey is not required for a local version of the plugin
            {
                if (logs) UnityEngine.Debug.LogError("Please contact support at contact@xandimmersion.com");
                return true;
            }

            if (string.IsNullOrEmpty(phrase))
            {
                if (logs) UnityEngine.Debug.LogError($"Text {position + 1} is empty. It will be ignored.");
                return true;
            }

            if (option == null || option == "")
            {
                if (logs) UnityEngine.Debug.LogError("Speaker is empty. Please select a speaker.");
                return true;
            }

            string modelPath = $"models/{option}.onnx";
            string url = $"{this.url}{this.port}/tts";

            ArielLocalApiCall arielLocalApiCall = new ArielLocalApiCall(
                modelPath,
                phrase,
                "OUTPUT_RAW",
                "wav",
                (int)octave * 12,
                speed,
                volume,
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false
            );


            string jsonBody = JsonUtility.ToJson(arielLocalApiCall);

            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                www.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Api-Key {apiKey}");

                if (logs) UnityEngine.Debug.Log("Sending request: " + jsonBody);

                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (logs) UnityEngine.Debug.LogError($"Error While Sending: {www.error}");
                    return true;
                }

                // transform data to audio data
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);

                if (string.IsNullOrEmpty(title))
                {
                    if (logs) UnityEngine.Debug.Log($"Text {position} has no file name; file will be saved as untitled(X).wav.");
                    title = "untitled";
                }

                string tempName = getNextFileName($"{savePath}/{title}.wav");
                ArielVoiceGenSav.ArielSavWav.Save(tempName, audioClip);
                if (logs) UnityEngine.Debug.Log($"Audio saved at: {tempName}");
            }
            

            AssetDatabase.Refresh();


            return true;
        }
#endif

        /// <summary>
        /// Converts text to audio and returns the audio clip in Runtime Mode.
        /// </summary>
        /// <param name="option">The voice option to use.</param>
        /// <param name="phrase">The text to convert to audio.</param>
        /// <param name="octave">The pitch of the audio.</param>
        /// <param name="speed">The speed of the audio.</param>
        /// <param name="effect">The audio effect to apply.</param>
        /// <param name="monostereo">The mono/stereo setting.</param>
        /// <param name="volume">The volume of the audio.</param>
        /// <param name="highSampleRate">The high sample rate setting.</param>
        /// <param name="voiceImprovement">The voice improvement setting.</param>
        /// <param name="apiKey">The API key required to access the Ariel API.</param>
        /// <param name="logs">Indicates whether to log messages. Default is false.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the audio clip.</returns>
        public async Task<AudioClip> TextToAudioRuntime(
            string option, string phrase, float octave = 0.0f, float speed = 1.0f, string effect = "",
            string monostereo = "false", float volume = 0.0f, string highSampleRate = "false", string voiceImprovement = "false",
            string apiKey = "", bool logs = false)
        {
            if (apiKey == null) // Actually apiKey is not required for a local version of the plugin
            {
                if (logs) UnityEngine.Debug.LogError("Please contact the support at contact@xandimmersion.com");
                return null;
            }

            if (phrase == null || phrase == "")
            {
                if (logs) UnityEngine.Debug.LogError("Text is empty. Please enter a text.");
                return null;
            }

            if (option == null || option == "")
            {
                if (logs) UnityEngine.Debug.LogError("Speaker is empty. Please select a speaker.");
                return null;
            }

            string modelPath = $"models/{option}.onnx";
            string url = $"{this.url}{this.port}/tts";

            ArielLocalApiCall arielLocalApiCall = new ArielLocalApiCall(
                modelPath,
                phrase,
                "OUTPUT_RAW",
                "wav",
                (int)octave * 12,
                speed,
                volume,
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false, // replace with actual boolean values as needed
                false
            );


            string jsonBody = JsonUtility.ToJson(arielLocalApiCall);

            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(jsonBody));
                www.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);
                www.SetRequestHeader("Content-Type", "application/json");
                www.SetRequestHeader("Authorization", $"Api-Key {apiKey}");

                if (logs) UnityEngine.Debug.Log("Sending request: " + jsonBody);

                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield();
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (logs) UnityEngine.Debug.LogError($"Error While Sending: {www.error}");
                    return null;
                }

                // transform data to audio data
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);

                // Return the audio clip if the audio clip is not null
                if (audioClip != null)
                {
                    return audioClip;
                }
                else
                {
                    if (logs) UnityEngine.Debug.LogError("Audio Clip creation failed");
                    return null;
                }
            }
        }

        /// <summary>
        /// Saves the audio clip as an asset.
        /// </summary>
        /// <param name="audioClip">The audio clip to save.</param>
        /// <param name="title">The title of the audio file.</param>
        /// <param name="savePath">The path to save the audio file.</param>
        /// <param name="logs">Indicates whether to log messages. Default is false.</param>
        /// <returns>True if the audio clip was saved successfully; otherwise, false.</returns>
        public bool SaveAudioClip(AudioClip audioClip, string title, string savePath, bool logs = false)
        {
            // Info: If using this function in runtime, make sure to save it in the Resources folder
            if (title == null || title == "")
            {
                if (logs) UnityEngine.Debug.Log("No file name : file will be saved at untitled(X).wav");
                title = "untitled";
            }

            // If no save path, return false (error)
            if (savePath == null || savePath == "")
            {
                if (logs) UnityEngine.Debug.LogError("No save path provided.");
                return false;
            }

            // if audio clip is null, return false (error)
            if (audioClip == null)
            {
                if (logs) UnityEngine.Debug.LogError("Audio Clip is null.");
                return false;
            }

            // If no .wav, add it to title
            if (!title.Contains(".wav"))
            {
                title += ".wav";
            }
            string tempName = $"{savePath}/{title}";
            tempName = getNextFileName(tempName);

            ArielVoiceGenSav.ArielSavWav.Save($"{tempName}", audioClip);

#if UNITY_EDITOR
            if (logs) AssetDatabase.Refresh(); // Only refresh in editor mode
#endif

            return true;
        }

        /// <summary>
        /// Gets the next available file name by appending a number if the file already exists.
        /// </summary>
        /// <param name="fileName">The initial file name.</param>
        /// <returns>The next available file name.</returns>
        private string getNextFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            int i = 0;
            //We loop until we create a filename that doesnt exist yet
            while (File.Exists(fileName))
            {
                if (i == 0)
                    fileName = fileName.Replace(extension, "(" + ++i + ")" + extension);
                else
                    fileName = fileName.Replace("(" + i + ")" + extension, "(" + ++i + ")" + extension);
            }
            return fileName;
        }
    }
}
