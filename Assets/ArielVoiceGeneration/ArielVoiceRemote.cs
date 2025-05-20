#if UNITY_EDITOR
    using UnityEditor;
#endif

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
using ArielVoiceGenSav;

namespace ArielVoiceRemote
{
    /// <summary>
    /// <para><b>Ariel Remote Class:</b></para>
    /// This class is used to interact with the Ariel API.
    /// It contains the functions to get the speakers and to convert text to audio.
    /// It also contains the function to save the audio file.
    /// The class is used in Editor Mode or in Runtime.
    /// <para><b>Editor Mode:</b> The audio file is saved to the disk.</para>
    /// <para><b>Runtime:</b> The audio file is returned as an <see cref="AudioClip"/>.</para>
    /// </summary>
    /// <remarks>
    /// <para><b>API Key:</b> The API Key is required to use the Ariel API.</para>
    /// </remarks>
    public static class ArielRemote
    {
        public static UnityWebRequest www;

        /// <summary>
        /// Gets the available speakers from the Ariel API.
        /// </summary>
        /// <param name="apiKey">The API key required to access the Ariel API.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the speaker settings.</returns>
        public static async Task<SpeakerSettings> GetSpeakers(string apiKey, bool logs = false)
        {
            // Define empty state object
            SpeakerSettings settings = new SpeakerSettings();
            LanguageCodes languageCodes = new LanguageCodes();

            if (string.IsNullOrEmpty(apiKey))
            {
                if (logs) UnityEngine.Debug.LogError("API Key is missing.");
                return null;
            }
            else
            {
                if (logs) UnityEngine.Debug.Log("API Key is set.");
            }


            using (UnityWebRequest www = UnityWebRequest.Get("https://ariel-api.xandimmersion.com/speakers"))
            {
                www.SetRequestHeader("Authorization", "Api-Key " + apiKey);

                // Send the request asynchronously and wait for it to complete
                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield(); // Prevent blocking the main thread
                }

                if (www.result == UnityWebRequest.Result.ProtocolError || www.result == UnityWebRequest.Result.ConnectionError)
                {
                    if (logs) UnityEngine.Debug.LogError("Error While Sending: " + www.error);
                    return null;
                }
                else
                {
                    try
                    {
                        string jsonResponse = www.downloadHandler.text;

                        // Unity's JsonUtility doesn't handle arrays directly, so we'll fix the JSON format
                        string fixedJson = "{\"speakers\":" + jsonResponse + "}";
                        Speaker[] speakers = JsonUtility.FromJson<SpeakerObject>(fixedJson).speakers;

                        Language[] allLanguages = new Language[0];
                        foreach (var speaker in speakers)
                        {
                            foreach (var lang in speaker.language)
                            {
                                var language = languageCodes.languages.FirstOrDefault(l => l.name == lang);
                                if (language != null)
                                {
                                    allLanguages = allLanguages.Concat(new[] { language }).ToArray();
                                }
                            }
                        }

                        // Remove duplicates in the array
                        allLanguages = allLanguages.Distinct().ToArray();

                        settings.speakers = speakers;
                        settings.languages = allLanguages;
                        return settings;
                    }
                    catch (System.Exception ex)
                    {
                        if (logs) UnityEngine.Debug.LogError($"Error parsing JSON: {ex.Message}");
                        return settings;
                    }
                }
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Converts text to audio and saves the audio file to disk in Editor Mode.
        /// </summary>
        /// <param name="option">The speaker name.</param>
        /// <param name="phrase">The text to convert to audio.</param>
        /// <param name="title">The name of the audio file.</param>
        /// <param name="octave">The pitch of the audio.</param>
        /// <param name="speed">The speed of the audio.</param>
        /// <param name="effect">The audio effect to apply. Have a closer look at the documentation which effects can be used.</param>
        /// <param name="monostereo">The mono/stereo setting.</param>
        /// <param name="volume">The volume of the audio.</param>
        /// <param name="highSampleRate">The high sample rate setting: 44kHz.</param>
        /// <param name="voiceImprovement">The voice improvement setting. Info: For high quality voices, this parameter can lead to a decrease in audio quality. Use only on speakers with lower quality.</param>
        /// <param name="savePath">The path to save the audio file.</param>
        /// <param name="apiKey">The API key required to access the Ariel API.</param>
        /// <param name="position">The position of the text in the list of generation elements. Only used for logging.</param>
        /// <returns>A task that represents the asynchronous operation. The task result indicates whether the operation was successful.</returns>
        public static async Task<bool> TextToAudio(string option, string phrase, string title, float octave = 0.0f, float speed = 1.0f, string effect = "", string monostereo = "false", float volume = 0.0f, string highSampleRate = "false", string voiceImprovement = "false", string savePath = "", string apiKey = "", int position = 0, bool logs = false)
        {
            if (apiKey == null)
            {
                if (logs) UnityEngine.Debug.LogError("Please contact the support at contact@xandimmersion.com");
                return true;
            }

            if (phrase == null || phrase == "")
            {
                if (logs) UnityEngine.Debug.LogError($"Text {position + 1} is empty. It will be ignored");
                return true;
            }

            if (option == null || option == "")
            {
                if (logs) UnityEngine.Debug.LogError("Speaker is empty. Please select a speaker.");
                return true;
            }

            WWWForm form = new WWWForm();
            form.AddField("sentence", phrase);
            string s_octave = octave.ToString();
            form.AddField("octave", s_octave.Replace(',', '.'));
            string s_speed = speed.ToString();
            form.AddField("speed", s_speed.Replace(',', '.'));
            form.AddField("volume", volume.ToString().Replace(',', '.'));
            form.AddField("effect", effect.Replace(',', '.'));
            form.AddField("stereo", monostereo.ToString().Replace(',', '.'));
            form.AddField("high_framerate", highSampleRate.ToString().Replace(',', '.'));
            form.AddField("voice_improvement", voiceImprovement.ToString().Replace(',', '.'));
            form.AddField("audio_stream", "true");
            string url = $"https://ariel-api.xandimmersion.com/tts/" + option;

            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(form.data);
                www.downloadHandler = new DownloadHandlerAudioClip(www.uri, AudioType.WAV);
                www.SetRequestHeader("Authorization", "Api-Key " + apiKey);
                www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield(); // Prevent blocking the main thread
                }

                if (www.result != UnityWebRequest.Result.Success)
                {
                    if (logs) UnityEngine.Debug.LogError($"Error While Sending: {www.error}");
                    return true;
                }

                // transform data to audio data
                AudioClip audioClip = DownloadHandlerAudioClip.GetContent(www);

                if (title == null || title == "")
                {
                    if (logs) UnityEngine.Debug.Log($"Text {position} no file name : file will be saved at untitled(X).wav");
                    title = "untitled";
                }
                string tempName = $"{savePath}/{title}.wav";
                tempName = getNextFileName(tempName);

                if (audioClip == null)
                {
                    if (logs) UnityEngine.Debug.LogError($"Audio Clip creation failed for text {position + 1}. It will be ignored");
                    return true;
                }
                ArielVoiceGenSav.ArielSavWav.Save($"{tempName}", audioClip);
                AssetDatabase.Refresh();
                
                return true;
            }
        }
#endif

        /// <summary>
        /// /// Converts text to audio and returns the audio clip in Runtime Mode.
        /// </summary>
        ///  <param name="option">The speaker name.</param>
        /// <param name="phrase">The text to convert to audio.</param>
        /// <param name="octave">The pitch of the audio.</param>
        /// <param name="speed">The speed of the audio.</param>
        /// <param name="effect">The audio effect to apply. Have a closer look at the documentation which effects can be used.</param>
        /// <param name="monostereo">The mono/stereo setting.</param>
        /// <param name="volume">The volume of the audio.</param>
        /// <param name="highSampleRate">The high sample rate setting: 44kHz.</param>
        /// <param name="voiceImprovement">The voice improvement setting. Info: For high quality voices, this parameter can lead to a decrease in audio quality. Use only on speakers with lower quality.</param>
        /// <param name="apiKey">The API key required to access the Ariel API.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the audio clip.</returns>
        public static async Task<AudioClip> TextToAudioRuntime(string option, string phrase, float octave = 0.0f, float speed = 1.0f, string effect = "", string monostereo = "false", float volume = 1, string highSampleRate = "false", string voiceImprovement = "false", string apiKey = "", bool logs = false)
        {
            if (apiKey == null)
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

            WWWForm form = new WWWForm();
            form.AddField("sentence", phrase);
            string s_octave = octave.ToString();
            form.AddField("octave", s_octave.Replace(',', '.'));
            string s_speed = speed.ToString();
            form.AddField("speed", s_speed.Replace(',', '.'));
            form.AddField("volume", volume.ToString().Replace(',', '.'));
            form.AddField("effect", effect.Replace(',', '.'));
            form.AddField("stereo", monostereo.ToString().Replace(',', '.'));
            form.AddField("high_framerate", highSampleRate.ToString().Replace(',', '.'));
            form.AddField("voice_improvement", voiceImprovement.ToString().Replace(',', '.'));
            form.AddField("audio_stream", "true");
            string url = $"https://ariel-api.xandimmersion.com/tts/" + option;

            using (UnityWebRequest www = new UnityWebRequest(url, "POST"))
            {
                www.uploadHandler = new UploadHandlerRaw(form.data);
                www.downloadHandler = new DownloadHandlerAudioClip(www.uri, AudioType.WAV);
                www.SetRequestHeader("Authorization", "Api-Key " + apiKey);
                www.SetRequestHeader("Content-Type", "application/x-www-form-urlencoded");

                var operation = www.SendWebRequest();
                while (!operation.isDone)
                {
                    await Task.Yield(); // Prevent blocking the main thread
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
        /// <returns>True if the audio clip was saved successfully; otherwise, false.</returns>
        public static bool SaveAudioClip(AudioClip audioClip, string title, string savePath, bool logs = false)
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
                AssetDatabase.Refresh(); // Only refresh in editor mode
#endif

            return true;
        }

        /// <summary>
        /// Gets the next available file name by appending a number if the file already exists.
        /// </summary>
        /// <param name="fileName">The initial file name.</param>
        /// <returns>The next available file name.</returns>
        private static string getNextFileName(string fileName)
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
