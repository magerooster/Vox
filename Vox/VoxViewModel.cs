using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Speech.Synthesis;
using System.Diagnostics;
using System.IO;
using System.Globalization;
using System.Reflection;
using Vox.Speech_Engines;

namespace Vox
{
    public class VoxViewModel : PropertyChangedBase
    {
        #region Commands

        public ICommand	CommandGenerateSpeech { get; set; }
        public ICommand CommandPauseResume { get; set; }
        public ICommand CommandStop { get; set; }
        public ICommand CommandWindowsSettings { get; set; }
        #endregion

        #region Properties
        private string _SpeechText;

        public string SpeechText
        {
            get { return GetField(ref _SpeechText); }
            set { SetField(ref _SpeechText, value); }
        }


        private ISpeechGenerator _SpeechGenerator;

        public ISpeechGenerator SpeechGenerator
        {
            get { return _SpeechGenerator; }
            set { SetField(ref _SpeechGenerator, value); }
        }


        private string _PauseResumeText;

        public string PauseResumeText
        {
            get { return GetField(ref _PauseResumeText); }
            set { SetField(ref _PauseResumeText, value); }
        }

        public bool AllowChangesToSettings
        {
            get { return SpeechGenerator.State == GeneratorState.Ready; }
        }

        #endregion Proprties
        public VoxViewModel()
        {
            CommandGenerateSpeech = new CommandBinding(GenerateSpeech);
            CommandPauseResume = new CommandBinding(PauseResume, (o) => SpeechGenerator.State != GeneratorState.Ready);
            CommandStop = new CommandBinding(Stop, (o) => SpeechGenerator.State != GeneratorState.Ready);
            CommandWindowsSettings = new CommandBinding(WindowsSettings);

            this.SpeechGenerator = new MicrosoftWindowsTTS();
            this.SpeechGenerator.Initialize();
            this.SpeechGenerator.SpeechStateUpdated += SpeechGenerator_SpeechStateUpdated;

            LoadSettingsFromDisk();

            PauseResumeText = "Pause";

            //GetWindowsVoices();
        }

        private void SpeechGenerator_SpeechStateUpdated(object? sender, SpeechStateUpdatedEventArgs e)
        {
            switch (e.State)
            {
                case GeneratorState.Uninitialized:
                    break;
                case GeneratorState.Ready:
                    break;
                case GeneratorState.Playing:
                    PauseResumeText = "Pause";
                    break;
                case GeneratorState.Paused:
                    PauseResumeText = "Resume";
                    break;
                case GeneratorState.Errored:
                    break;
                case GeneratorState.Disposed:
                    break;
                default:
                    break;
            }
        }

        #region Events

        #endregion

        #region Commands
        public void GenerateSpeech(object? parameter)
        {
            this.SpeechGenerator.Start(SpeechText);
        }

        public void PauseResume(object parameter)
        {
            this.SpeechGenerator.PauseUnpause();
        }

        public void Stop(object parameter)
        {
            this.SpeechGenerator.Stop();
        }

        public void WindowsSettings(object parameter)
        {
            // Expand the environment variable
            string sapiPath = Environment.ExpandEnvironmentVariables("%SystemRoot%\\System32\\Speech\\SpeechUX\\sapi.cpl");

            var processStartInfo = new ProcessStartInfo
            {
                FileName = "rundll32.exe",
                Arguments = $"shell32.dll,Control_RunDLL {sapiPath},,1",
                UseShellExecute = false,
                RedirectStandardOutput = true, // Optional: Captures output from the DLL
            };

            var process = Process.Start(processStartInfo);
            process.WaitForExit();

            // Optional: Read the captured output (if enabled)
            string output = process.StandardOutput.ReadToEnd();
            Console.WriteLine($"Output from DLL: {output}");
        }
        #endregion

        #region Settings
        public void SaveSettingsToDisk()
        {
            using (FileStream fs = new FileStream(".\\settings.txt", FileMode.OpenOrCreate))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    this.SpeechGenerator.SaveSettings(sw);
                }
            }
        }

        public void LoadSettingsFromDisk()
        {
            if (File.Exists(".\\settings.txt"))
            {
                using (FileStream fs = new FileStream(".\\settings.txt", FileMode.Open))
                {
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        this.SpeechGenerator.LoadSettings(sr);
                    }
                }
            }
            else
            {
                this.SpeechGenerator.LoadSettings(null);
            }
        }
        #endregion
    }
}
