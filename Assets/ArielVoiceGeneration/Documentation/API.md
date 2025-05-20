# API Reference

**[← Table of contents](../README.md#table-of-contents)**

### On this page

[Introduction](#introduction)<br/>
#### [Ariel Voice Remote](#ariel-voice-remote)<br/>
[Ariel Remote Class](#ariel-remote-class)<br/>
    [- Get available Speakers](#get-available-speakers-remote)<br/>
    [- Text To Audio (Remote | Editor Version)](#text-to-audio-remote--editor-version)<br/>
    [- Text To Audio (Remote | Runtime Version)](#text-to-audio-remote--runtime-version)<br/>
    [- Save Audio Clip (Remote)](#save-audio-clip)<br/>
    [- Get Next Filename (Remote)](#get-next-filename)<br/>
#### [Ariel Voice Local](#ariel-voice-local)<br/>
[Ariel Local Class](#ariel-local-class)<br/>
    [- Class Attributes](#class-attributes-local)<br/>
    [- Start Local Server](#start-local-server)<br/>
    [- Shutdown Local Server](#shutdown-local-server)<br/>
    [- Restart Server](#restart-server)<br/>
    [- Force Restart Server](#force-restart-server)<br/>
    [- Is Server Running](#is-server-running)<br/>
    [- Kill Process On Port](#kill-process-on-port)<br/>
    [- Check Service Running On Port](#check-service-running-on-port)<br/>
    [- Get available Speakers](#get-available-speakers-local)<br/>
    [- Text To Audio (Local | Editor Version)](#text-to-audio-local--editor-version)<br/>
    [- Text To Audio (Local | Runtime Version)](#text-to-audio-local--runtime-version)<br/>
    [- Save Audio Clip (Local)](#save-audio-clip-local)<br/>
    [- Get Next Filename (Local)](#get-next-filename-local)<br/>
#### [Glossary](#glossary)
[Glossary Class](#glossary-class)<br/>
    [- Class Attributes](#glossary-class-attributes)<br/>
    [- Write CSV](#write-csv)<br/>
    [- Read CSV](#read-csv)<br/>
    [- Edit CSV](#edit-csv)<br/>
    [- Compare Sentence To Glossary](#compare-sentence-to-glossary)<br/>
#### [Ariel Save Wav](#ariel-save-wav)<br/>
    [Save bytes to file](#save-bytes-to-file)<br/>
#### [Ariel Common Types](#ariel-common-types)<br/>
[- Ariel TTS Class (remote)](#ariel-tts-class-editor-version)<br/>
[- Ariel TTS Class (local)](#ariel-tts-class-local)<br/>
[- Ariel Csv](#ariel-csv)<br/>
[- Speaker](#speaker)<br/>
[- Speaker Object](#speaker-object)<br/>
[- Language](#language)<br/>
[- Language Codes](#language-codes)<br/>
[- Speaker Settings](#speaker-settings)<br/>
[- Server State](#server-state)<br/>

<!------------------------------------------------------------------------------------------------------------------------------->
<br/>

# Introduction

The Ariel plugin provides a set of methods to generate audio speech from text sentences. The plugin can be used in the Unity Editor and in runtime projects. The plugin is divided into two parts: the **Remote** and the **Local** version. The **Remote** version uses the Ariel API to generate the audio, while the **Local** version uses a local server to generate the audio. The **Local** version is faster and can be used offline, but the audio quality is lower than the **Remote** version.

The plugin is divided into four main classes:

* **Ariel Voice Remote**: The remote version of the Ariel plugin. It uses the Ariel API to generate the audio.
* **Ariel Voice Local**: The local version of the Ariel plugin. It uses a local server to generate the audio.
* **Glossary**: A class to manage glossaries. A glossary is a list of sentences and their corresponding audio files.
* **Ariel Save Wav**: A class to save audio files to disk.

The plugin also provides a set of common types used by the other classes.

[Back to Top](#api-reference)

<!------------------------------------------------------------------------------------------------------------------------------->

# Ariel Voice Remote

## Ariel Remote Class

The Ariel Remote class is used to generate audio speech from text sentences using the Ariel API. The class provides methods to get available speakers, generate audio from text, save audio to disk, and get the next available filename.

This is a static class, because we are not storing any data in the class itself. We are just using it to call the methods. You don't need to create an instance of this class to use it.

### Get available Speakers (Remote)

C# Function (public | async): `ArielRemote.GetSpeakers`
Namespace: `ArielVoiceRemote`

#### Description

Gets the available speakers from the Ariel API. Using your personal Api Key, you can get the list of available speakers and languages. These may vary depending on your subscription plan.
 
#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| apiKey            | string         | The API key used to authenticate the request. |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| speakers          | Task\<SpeakerSettings\> | An object containing all available speakers and languages. For more information, see [Speaker Settings](#speaker-settings). |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ArgumentNullException` | Thrown when the `apiKey` is null or empty. |
| `UnityWebRequestException` | Thrown when there is an error with the web request. |
| `JsonException` | Thrown when there is an error parsing the JSON response. |

[Back to Top](#api-reference)

### Text To Audio (Remote | Editor Version)

C# Function (public | async): `ArielRemote.TextToAudio`
Namespace: `ArielVoiceRemote`

#### Description

Generates audio from text using the Ariel API. The audio is generated using the specified speaker, text, and other settings. The audio is saved to the specified path.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| option            | string         | The speaker name. |
| phrase            | string         | The text to convert to audio. |
| title             | string         | The name of the audio file. |
| octave            | float          | The pitch of the audio. |
| speed             | float          | The speed of the audio. |
| effect            | string         | The audio effect to apply. |
| monostereo        | string         | The mono/stereo setting. |
| volume            | float          | The volume of the audio. |
| highSampleRate    | string         | The high sample rate setting. |
| voiceImprovement  | string         | The voice improvement setting. |
| savePath          | string         | The path to save the audio file. |
| apiKey            | string         | The API key required to access the Ariel API. |
| position          | int            | The position of the text in the list of generation elements. |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | Task\<bool\>   | Indicates whether the operation was successful. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ArgumentNullException` | Thrown when the `apiKey`, `phrase`, or `option` is null or empty. |
| `UnityWebRequestException` | Thrown when there is an error with the web request. |
| `AudioClipException` | Thrown when there is an error creating the audio clip. |

[Back to Top](#api-reference)

### Text To Audio (Remote | Runtime Version)

C# Function (public | async): `ArielRemote.TextToAudioRuntime`
Namespace: `ArielVoiceRemote`

#### Description

Generates audio from text using the Ariel API. The audio is generated using the specified speaker, text, and other settings. It returns an Audio Clip that you can use in your Runtime project.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| option            | string         | The speaker name. |
| phrase            | string         | The text to convert to audio. |
| octave            | float          | The pitch of the audio. |
| speed             | float          | The speed of the audio. |
| effect            | string         | The audio effect to apply. |
| monostereo        | string         | The mono/stereo setting. |
| volume            | float          | The volume of the audio. |
| highSampleRate    | string         | The high sample rate setting. |
| voiceImprovement  | string         | The voice improvement setting. |
| apiKey            | string         | The API key required to access the Ariel API. |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| audioClip         | Task\<AudioClip\> | The generated audio clip. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ArgumentNullException` | Thrown when the `apiKey`, `phrase`, or `option` is null or empty. |
| `UnityWebRequestException` | Thrown when there is an error with the web request. |
| `AudioClipException` | Thrown when there is an error creating the audio clip. |

[Back to Top](#api-reference)

### Save Audio Clip

C# Function (public): `ArielRemote.SaveAudioClip`
Namespace: `ArielVoiceRemote`

#### Description

Saves an audio clip to disk.

> If being used in Editor mode, it is recommended to use the [Ariel Text To Audio](#text-to-audio-remote--editor-version) function, because it already saves the Audio Clip directly.

> If being used in Runtime, you will need to save the asset to the Resources folder, but it is recommended to use the Audio Clip directly as a variable to bind it to an audio source.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| audioClip         | AudioClip      | The audio clip to save. |
| title             | string         | The title of the audio file. |
| savePath          | string         | The path to save the audio file. |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | bool           | Indicates whether the audio clip was saved successfully. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ArgumentNullException` | Thrown when the `audioClip` or `savePath` is null or empty. |
| `IOException` | Thrown when there is an error saving the audio file. |

[Back to Top](#api-reference)

### Get Next Filename

C# Function (private): `ArielRemote.getNextFileName`
Namespace: `ArielVoiceRemote`

#### Description

Generates the next available file name. The file name is generated by appending a number to the initial file name, if it already exists.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| fileName          | string         | The initial file name. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| nextFileName      | string         | The next available file name. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `IOException` | Thrown when there is an error generating the next file name. |

[Back to Top](#api-reference)

<!------------------------------------------------------------------------------------------------------------------------------->

# Ariel Voice Local

## Ariel Local Class

The Ariel Local class is used to generate audio speech from text sentences using a local server. The class provides methods to start and stop the local server, get available speakers, generate audio from text, save audio to disk, and get the next available filename.

This is not a static class, because we are storing data (such as the server process) in the class itself. You need to create an instance of this class to use it.

### Class Attributes (Local)

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| url               | string         | The base URL of the local server. |
| processName       | string         | The name of the local server process. |
| port              | int            | The port number used by the local server. |
| process           | Process        | The process object for the local server. |

[Back to Top](#api-reference)

### Start Local Server

C# Function (public): `ArielLocal.StartServer`
Namespace: `ArielVoiceLocal`

#### Description

Starts the local server. The server must be running to generate audio from text.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | bool           | Indicates whether the server started successfully. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ProcessException` | Thrown when there is an error starting the server process. |

[Back to Top](#api-reference)

### Shutdown Local Server

C# Function (public): `ArielLocal.ShutdownServer`
Namespace: `ArielVoiceLocal`

#### Description

Stops the local server. The server should be stopped when it is no longer needed.

> Make sure to stop the server when exiting the game, otherwise it will keep running as a background task and needs to be closed manually in the task manager.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | bool           | Indicates whether the server was stopped successfully. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ProcessException` | Thrown when there is an error stopping the server process. |

[Back to Top](#api-reference)

### Restart Server

C# Function (public): `ArielLocal.RestartServer`
Namespace: `ArielVoiceLocal`

#### Description

Restarts the local server. The server must be running to generate audio from text.

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | bool           | Indicates whether the server was restarted successfully. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ProcessException` | Thrown when there is an error restarting the server process. |

[Back to Top](#api-reference)

### Force Restart Server

C# Function (public): `ArielLocal.ForceRestartServer`
Namespace: `ArielVoiceLocal`

#### Description

Restarts the local server. 

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | bool           | Indicates whether the server was restarted successfully. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ProcessException` | Thrown when there is an error restarting the server process. |

[Back to Top](#api-reference)

### Is Server Running

C# Function (public): `ArielLocal.IsServerRunning`
Namespace: `ArielVoiceLocal`

#### Description

Checks if the local server is running already or not.

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | bool           | Indicates whether the server is running. |

[Back to Top](#api-reference)

### Kill Process On Port

C# Function (private): `ArielLocal.KillProcessOnPort`
Namespace: `ArielVoiceLocal`

#### Description

Kills the process running on the specified port.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| killPort          | int            | The port number to check for an active process. |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | bool           | Indicates whether the process was successfully killed. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ProcessException` | Thrown when there is an error killing the process. |

[Back to Top](#api-reference)

### Check Service Running On Port

C# Function (private): `ArielLocal.CheckServiceRunningOnPort`
Namespace: `ArielVoiceLocal`

#### Description

Checks if a service is running on the specified port.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| checkPort         | int            | The port number to check for a running service. |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| state             | ServerState    | An object indicating the status of the port and server process. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ProcessException` | Thrown when there is an error checking the server status. |

[Back to Top](#api-reference)

### Get available Speakers (Local)

C# Function (public): `ArielLocal.GetSpeakers`
Namespace: `ArielVoiceLocal`

#### Description
Gets the available speakers that are available for usage with the local server. It returns all local `.onnx` models that also have a corresponding `{speaker_name}.onnx.json` file stored in the `Assets\ArielVoiceGeneration\Local\models` folder. If you want to have access to more voices, get in touch with [X&Immersion](mailto:contact@xandimmersion.com).

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| speakers          | SpeakerSettings | An object containing all available speakers and languages. For more information, see [Speaker Settings](#speaker-settings). |

[Back to Top](#api-reference)

### Text To Audio (Local | Editor Version)

C# Function (public | async): `ArielLocal.TextToAudio`
Namespace: `ArielVoiceLocal`

#### Description

Generates audio from text using the local server. The audio is generated using the specified speaker, text, and other settings. The audio is saved to the specified path.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| option            | string         | The voice option to use. |
| phrase            | string         | The text to convert to audio. |
| title             | string         | The title of the audio file. |
| octave            | float          | The pitch of the audio. |
| speed             | float          | The speed of the audio. |
| effect            | string         | The audio effect to apply. |
| monostereo        | string         | The mono/stereo setting. |
| volume            | float          | The volume of the audio. |
| highSampleRate    | string         | The high sample rate setting. |
| voiceImprovement  | string         | The voice improvement setting. |
| savePath          | string         | The path to save the audio file. |
| apiKey            | string         | The API key required to access the Ariel API. |
| position          | int            | The position of the text in the list. |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | Task\<bool\>   | Indicates whether the operation was successful. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ArgumentNullException` | Thrown when the `apiKey`, `phrase`, or `option` is null or empty. |
| `UnityWebRequestException` | Thrown when there is an error with the web request. |
| `AudioClipException` | Thrown when there is an error creating the audio clip. |

[Back to Top](#api-reference)

### Text To Audio (Local | Runtime Version)

C# Function (public | async): `ArielLocal.TextToAudioRuntime`
Namespace: `ArielVoiceLocal`

#### Description

Generates audio from text using the local server. The audio is generated using the specified speaker, text, and other settings. It returns an Audio Clip that you can use in your Runtime project.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| option            | string         | The voice option to use. |
| phrase            | string         | The text to convert to audio. |
| octave            | float          | The pitch of the audio. |
| speed             | float          | The speed of the audio. |
| effect            | string         | The audio effect to apply. |
| monostereo        | string         | The mono/stereo setting. |
| volume            | float          | The volume of the audio. |
| highSampleRate    | string         | The high sample rate setting. |
| voiceImprovement  | string         | The voice improvement setting. |
| apiKey            | string         | The API key required to access the Ariel API. |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| audioClip         | Task\<AudioClip\> | The generated audio clip. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ArgumentNullException` | Thrown when the `apiKey`, `phrase`, or `option` is null or empty. |
| `UnityWebRequestException` | Thrown when there is an error with the web request. |
| `AudioClipException` | Thrown when there is an error creating the audio clip. |

[Back to Top](#api-reference)

### Save Audio Clip (Local)

C# Function (public): `ArielLocal.SaveAudioClip`
Namespace: `ArielVoiceLocal`

#### Description

Saves an audio clip to disk.

> If being used in Editor mode, it is recommended to use the [Ariel Text To Audio](#text-to-audio-local--editor-version) function, because it already saves the Audio Clip directly.

> If being used in Runtime, you will need to save the asset to the Resources folder, but it is recommended to use the Audio Clip directly as a variable to bind it to an audio source.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| audioClip         | AudioClip      | The audio clip to save. |
| title             | string         | The title of the audio file. |
| savePath          | string         | The path to save the audio file. |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | bool           | Indicates whether the audio clip was saved successfully. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ArgumentNullException` | Thrown when the `audioClip` or `savePath` is null or empty. |
| `IOException` | Thrown when there is an error saving the audio file. |

[Back to Top](#api-reference)

### Get Next Filename (Local)

C# Function (private): `ArielLocal.getNextFileName`
Namespace: `ArielVoiceLocal`

#### Description

Generates the next available file name. The file name is generated by appending a number to the initial file name, if it already exists.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| fileName          | string         | The initial file name. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| nextFileName      | string         | The next available file name. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `IOException` | Thrown when there is an error generating the next file name. |

[Back to Top](#api-reference)

<!------------------------------------------------------------------------------------------------------------------------------->

# Glossary

## Glossary Class

The Glossary class is used to manage glossaries. A glossary is used to change the way a speaker pronounces a specific word. The class provides methods to write, read, and edit glossary files, as well as to compare a sentence to a glossary.

### Glossary Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| sentence          | string         | The sentence to generate. | "" |
| newGlossaryName   | string         | The name of the new glossary. | "" |
| glossary          | TextAsset      | The glossary to read or edit. | null |
| wordList          | WordList       | The list of words and their pronunciations. | new WordList() |

### Words Class

The Words class represents a word and its pronunciation in the glossary.

#### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| word              | string         | The word in the glossary. | "" |
| pronunciation     | string         | The pronunciation of the word. | "" |

### WordList Class

The WordList class represents a list of words and their pronunciations in the glossary.

#### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| words             | List<Words>    | The list of words and their pronunciations. | new List<Words>() |

### Write CSV

C# Function (public): `ArielGlossary.WriteCSV`

#### Description

Generates a new glossary or overwrites an existing one. The glossary is saved as a CSV file.

#### Parameters

None

#### Return values

None

#### Exceptions

None

### Read CSV

C# Function (private): `ArielGlossary.ReadCSV`

#### Description

Reads the current glossary and populates the word list.

#### Parameters

None

#### Return values

None

#### Exceptions

None

### Edit CSV

C# Function (public): `ArielGlossary.EditCSV`

#### Description

Edits the current glossary and saves the changes to the CSV file.

#### Parameters

None

#### Return values

None

#### Exceptions

None

### Compare Sentence To Glossary

C# Function (private): `ArielGlossary.CompareSentenceToGlossary`

#### Description

Compares a sentence to the glossary and replaces words in the sentence with their pronunciations from the glossary.

#### Parameters

None

#### Return values

None

#### Exceptions

None

[Back to Top](#api-reference)

<!------------------------------------------------------------------------------------------------------------------------------->

# Ariel Save Wav

## Ariel Save Wav Class

C# Class: `ArielSavWav`
Namespace: `ArielVoiceGenSav`

### Description

The Ariel Save Wav class is used to save audio bytes to a WAV file.

### Save bytes to file

C# Function (public): `ArielSavWav.Save`

> If you are using this function in a Runtime project, you will need to save the asset to the Resources folder, but it is recommended to use the Audio Clip directly as a variable to bin it to an audio source.

#### Description

Saves an AudioClip as a WAV file.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| filename          | string         | The name of the file to save. |
| clip              | AudioClip      | The AudioClip to save. |
| logs              | bool           | Indicate if logs should be printed to the console. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | bool           | Indicates whether the file was saved successfully. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ArgumentNullException` | Thrown when the `filename` or `clip` is null or empty. |
| `IOException` | Thrown when there is an error saving the file. |

### Trim Silence

C# Function (public): `ArielSaveWav.TrimSilence`

#### Description

Trims silence from the beginning and end of an AudioClip.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| clip              | AudioClip      | The AudioClip to trim. |
| min               | float          | The minimum amplitude to consider as non-silence. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | AudioClip      | A new AudioClip with the silence trimmed. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ArgumentNullException` | Thrown when the `clip` is null. |

### Trim Silence (List of samples)

C# Function (public): `ArielSaveWav.TrimSilence`

#### Description

Trims silence from the beginning and end of a list of samples.

#### Parameters

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| samples           | List<float>    | The list of samples to trim. |
| min               | float          | The minimum amplitude to consider as non-silence. |
| channels          | int            | The number of channels in the audio. |
| hz                | int            | The sample rate of the audio. |
| _3D               | bool           | Indicates whether the audio should be 3D. |
| stream            | bool           | Indicates whether the audio should be streamed. |

#### Return values

| Name              | Type           | Description |
| ----------------- | -------------- | ----------- |
| result            | AudioClip      | A new AudioClip with the silence trimmed. |

#### Exceptions

| Name              | Description |
| ----------------- | ----------- |
| `ArgumentNullException` | Thrown when the `samples` list is null. |

[Back to Top](#api-reference)

<!------------------------------------------------------------------------------------------------------------------------------->

# Ariel Common Types

## Ariel TTS Class (Editor Version)

C# Class: `ArielTts`
Namespace: `ArielCommonTypes`

### Description

The Ariel TTS class is used for the Editor Version of the Ariel plugin. It contains all required information for each Audio line to generate. You can use it in Runtime projects, to keep track of your generation settings.

### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| Phrase            | string         | The text to convert to audio. | "" |
| Octave            | float          | The pitch of the audio. | 0.0f |
| Speed             | float          | The speed of the audio. | 1.0f |
| Title             | string         | The title of the audio file. | "" |
| Selected_O        | int            | The selected option. | 0 |
| Selected_L        | int            | The selected language. | 9 |
| glossaryToUsePath | string         | The path to the glossary to use. | "" |
| useGlossary       | bool           | Indicates whether to use the glossary. | false |
| Effect            | int            | The audio effect to apply. | 0 |
| MonoStereo        | string         | The mono/stereo setting. | "" |
| isStereo          | bool           | Indicates whether the audio is stereo. | false |
| Volume            | float          | The volume of the audio. | 1.0f |
| HighSampleRate    | string         | The high sample rate setting. | "" |
| isHSRate          | bool           | Indicates whether to use high sample rate. | false |
| AdvancedOptions   | bool           | Indicates whether to use advanced options. | false |
| VoiceImprovement  | string         | The voice improvement setting. | "" |
| useVoiceImprovement | bool         | Indicates whether to use voice improvement. | false |

[Back to Top](#api-reference)

## Ariel TTS Class (local)

C# Class: `ArielLocalApiCall`
Namespace: `ArielCommonTypes`

### Description

The Ariel Local API Call class is used for the Local Version of the Ariel plugin. It is an internal class that is used within the [ArielVoiceLocal](#ariel-local-class) class. It contains all information that is passed to the local server to generate the audio.  

### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| modelPath         | string         | The path to the model. | "" |
| sentence          | string         | The text to convert to audio. | "" |
| outputType        | string         | The output type. | "" |
| format            | string         | The format of the audio. | "" |
| semitones         | int            | The number of semitones to shift. | 0 |
| speed             | float          | The speed of the audio. | 1.0f |
| volume            | float          | The volume of the audio. | 1.0f |
| voice_improvement | bool           | Indicates whether to use voice improvement. | false |
| high_framerate    | bool           | Indicates whether to use high frame rate. | false |
| telephone         | bool           | Indicates whether to apply telephone effect. | false |
| cave              | bool           | Indicates whether to apply cave effect. | false |
| smallcave         | bool           | Indicates whether to apply small cave effect. | false |
| gasmask           | bool           | Indicates whether to apply gas mask effect. | false |
| badreception      | bool           | Indicates whether to apply bad reception effect. | false |
| nextroom          | bool           | Indicates whether to apply next room effect. | false |
| alien             | bool           | Indicates whether to apply alien effect. | false |
| alien2            | bool           | Indicates whether to apply alien2 effect. | false |
| stereo            | bool           | Indicates whether the audio is stereo. | false |

[Back to Top](#api-reference)


## Ariel Csv

C# Class: `ArielCsv`
Namespace: `ArielCommonTypes`

### Description

The Ariel Csv class is used to manage the [glossary](#glossary-class) csv files. The class keeps track of the glossary file path and the list of glossary items.

### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| Word              | string         | The word in the CSV file. | "" |
| Pronunciation     | string         | The pronunciation of the word. | "" |

[Back to Top](#api-reference)

## Speaker

C# Class: `Speaker`
Namespace: `ArielCommonTypes`

The Speaker class represents a speaker. The class provides properties for the speaker's name, language, and other settings. This class is marked as `Serializable`.

### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| name              | string         | The name of the speaker. | "" |
| id                | int            | The ID of the speaker. | 0 |
| emotion           | List<string>   | The list of emotions the speaker can express. | new List<string>() |
| language          | List<string>   | The list of languages the speaker can speak. | new List<string>() |
| gender            | string         | The gender of the speaker. | "" |

[Back to Top](#api-reference)


## Speaker Object

The Speaker Object class represents the return type of the [GetSpeakers](#get-available-speakers-remote) method. The class provides an array of speakers.

### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| speakers          | Speaker[]      | The array of speakers. | new Speaker[0] |

[Back to Top](#api-reference)

## Language

C# Class: `Language`
Namespace: `ArielCommonTypes`

### Description

The Language class represents a language. The class provides properties for the language's name and unified code representation for each [language](#language-codes). This class is marked as `Serializable`.

### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| name              | string         | The name of the language. | "" |
| code              | string         | The code of the language. | "" |

[Back to Top](#api-reference)

## Language Codes

C# Class: `LanguageCodes`
Namespace: `ArielCommonTypes`

The Language Codes class provides a list of language codes.

### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| languages         | List<Language> | The list of languages. | new List<Language>() |


### Language Codes

| Language          | Code           |
| ----------------- | -------------- |
| Chinese           | zh-cn          |
| Korean            | ko             |
| Dutch             | nl             |
| Turkish           | tr             |
| Swedish           | sv             |
| Indonesian        | id             |
| Filipino          | fil            |
| Japanese          | ja             |
| Ukrainian         | uk             |
| Greek             | el             |
| Czech             | cs             |
| Finnish           | fi             |
| Romanian          | ro             |
| Russian           | ru             |
| Danish            | da             |
| Bulgarian         | bg             |
| Malay             | ms             |
| Slovak            | sk             |
| Croatian          | hr             |
| Arabic            | ar             |
| Tamil             | ta             |
| English           | en             |
| Polish            | pl             |
| German            | de             |
| Spanish           | es             |
| French            | fr             |
| Italian           | it             |
| Hindi             | hi             |
| Portuguese        | pt             |

[Back to Top](#api-reference)

## Speaker Settings

C# Class: `SpeakerSettings`
Namespace: `ArielCommonTypes`

### Description

The Speaker Settings class represents the return value of the [GetSpeakers (Remote)](#get-available-speakers-remote) and [GetSpeakers (Local)](#get-available-speakers-local) method. The class provides an array of speakers and languages.

### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| speakers          | Speaker[]      | The array of speakers. | new Speaker[0] |
| languages         | Language[]     | The array of languages. | new Language[32] |

[Back to Top](#api-reference)

## Server State

C# Class: `ServerState`
Namespace: `ArielCommonTypes`

### Description

The Server State class represents the state of the server. The class provides properties for the server's status and other information.

### Class Attributes

| Name              | Type           | Description | Default Value |
| ----------------- | -------------- | ----------- | ------------- |
| portInUse         | bool           | Indicates whether the port is in use. | true |
| arielServerProcessRunning | bool   | Indicates whether the Ariel server process is running. | false |

[Back to Top](#api-reference)
