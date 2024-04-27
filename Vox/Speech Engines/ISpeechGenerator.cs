using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Vox.Speech_Engines
{
    public interface ISpeechGenerator
    {
        string VoiceName { get; set; }
        int VoiceSpeed { get; set; } //As a percentage, so 100 = 1.0x speed.

        string ApiAddress { get; set; }
        string ApiKey { get; set; }
        string ApiToken { get; set; }

        GeneratorState State { get; }

        event EventHandler<SpeechStateUpdatedEventArgs>? SpeechStateUpdated;

        void Initialize();
        void Cleanup();

        void Start(string text, string culture = "en-US");
        void PauseUnpause();
        void Stop();

        void SaveSettings(StreamWriter settingsFile);
        void LoadSettings(StreamReader? settingsFile); //If null, generate default values.
    }

    public enum GeneratorState
    {
        Uninitialized,
        Ready,
        Playing,
        Paused,
        Errored,
        Disposed,
    }

    public class SpeechStateUpdatedEventArgs : EventArgs
    {
        public GeneratorState State { get; set; }
        public SpeechStateUpdatedEventArgs(GeneratorState NewState)
        {
            this.State = NewState;
        }
    }
}
