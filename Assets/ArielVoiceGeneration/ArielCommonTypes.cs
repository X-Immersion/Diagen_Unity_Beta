using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ArielCommonTypes;



namespace ArielCommonTypes
{
    public class ArielTts
    {
        public string Phrase;

        public float Octave = 0.0f;

        public float Speed = 1.0f;

        public string Title;

        public int Selected_O = 0;
        public int Selected_L = 9;
        public ArielTts(string phrase, float octave, float speed, string title, int selected_O, int selected_L,int effect, string monostereo, float volume, string highSampleRate)
        {
            this.Phrase = phrase;
            this.Octave = octave;
            this.Speed = speed;
            this.Title = title;
            this.Selected_O = selected_O;
            this.Selected_L = selected_L;
            this.Effect = effect;
            this.MonoStereo = monostereo;
            this.Volume = volume;
            this.HighSampleRate = highSampleRate;
        }

        public string glossaryToUsePath;
        public bool useGlossary = false;

        public int Effect;

        public string MonoStereo;
        public bool isStereo = false;

        public float Volume;

        public string HighSampleRate;
        public bool isHSRate;

        public bool AdvancedOptions;

        public string VoiceImprovement;
        public bool useVoiceImprovement = false;
    }

    public class ArielLocalApiCall
    {
        public string modelPath;
        public string sentence;
        public string outputType;
        public string format;
        public int semitones;
        public float speed;
        public float volume;
        public bool voice_improvement = false;
        public bool high_framerate = false;
        public bool telephone = false;
        public bool cave = false;
        public bool smallcave = false;
        public bool gasmask = false;
        public bool badreception = false;
        public bool nextroom = false;
        public bool alien = false;
        public bool alien2 = false;
        public bool stereo = false;
        public ArielLocalApiCall(
            string modelPath, string sentence, string outputType, string format, int semitones, 
            float speed, float volume, bool voice_improvement = false, bool high_framerate = false, 
            bool telephone = false, bool cave = false, bool smallcave = false, bool gasmask = false, 
            bool badreception = false, bool nextroom = false, bool alien = false, bool alien2 = false, 
            bool stereo = false)
        {
            this.modelPath = modelPath;
            this.sentence = sentence;
            this.outputType = outputType;
            this.format = format;
            this.semitones = semitones;
            this.speed = speed;
            this.volume = volume;
            this.voice_improvement = voice_improvement;
            this.high_framerate = high_framerate;
            this.telephone = telephone;
            this.cave = cave;
            this.smallcave = smallcave;
            this.gasmask = gasmask;
            this.badreception = badreception;
            this.nextroom = nextroom;
            this.alien = alien;
            this.alien2 = alien2;
            this.stereo = stereo;
        }
    }

    public class ArielCsv
    {
        public string Word;

        public string Pronunciation;

        public ArielCsv(string word, string pronunciation)
        {
            this.Word = word;
            this.Pronunciation = pronunciation;
        }
    }

    // Classes to match JSON structure
    [System.Serializable]
    public class Speaker
    {
        public string name;
        public int id;
        public List<string> emotion;
        public List<string> language;
        public string gender;
    }

    public class SpeakerObject
    {
        public Speaker[] speakers;
    }

    [System.Serializable]

    public class Language
    {
        public string name;
        public string code;
    }

    public class LanguageCodes
    {
        public List<Language> languages = new List<Language>
        {
            new Language { name = "Chinese", code = "zh-cn" },
            new Language { name = "Korean", code = "ko" },
            new Language { name = "Dutch", code = "nl" },
            new Language { name = "Turkish", code = "tr" },
            new Language { name = "Swedish", code = "sv" },
            new Language { name = "Indonesian", code = "id" },
            new Language { name = "Filipino", code = "fil" },
            new Language { name = "Japanese", code = "ja" },
            new Language { name = "Ukrainian", code = "uk" },
            new Language { name = "Greek", code = "el" },
            new Language { name = "Czech", code = "cs" },
            new Language { name = "Finnish", code = "fi" },
            new Language { name = "Romanian", code = "ro" },
            new Language { name = "Russian", code = "ru" },
            new Language { name = "Danish", code = "da" },
            new Language { name = "Bulgarian", code = "bg" },
            new Language { name = "Malay", code = "ms" },
            new Language { name = "Slovak", code = "sk" },
            new Language { name = "Croatian", code = "hr" },
            new Language { name = "Arabic", code = "ar" },
            new Language { name = "Tamil", code = "ta" },
            new Language { name = "English", code = "en" },
            new Language { name = "Polish", code = "pl" },
            new Language { name = "German", code = "de" },
            new Language { name = "Spanish", code = "es" },
            new Language { name = "French", code = "fr" },
            new Language { name = "Italian", code = "it" },
            new Language { name = "Hindi", code = "hi" },
            new Language { name = "Portuguese", code = "pt" }
        };
    }

    public class SpeakerSettings
    {
        public Speaker[] speakers = new Speaker[0];
        public Language[] languages = new Language[32];

    }

    public class ServerState
    {
        public bool portInUse = true;
        public bool arielServerProcessRunning = false;
    }
}