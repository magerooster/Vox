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
        private string _SpeechText = string.Empty;

		public string SpeechText
		{
			get { return GetField(ref _SpeechText); }
			set { SetField(ref _SpeechText, value); }
		}

        private IReadOnlyCollection<InstalledVoice> _WindowsVoices = new List<InstalledVoice>();

        public IReadOnlyCollection<InstalledVoice> WindowsVoices
        {
            get { return _WindowsVoices; }
            set { SetField(ref _WindowsVoices, value); }
        }

        private InstalledVoice? _SelectedWindowsVoice;

        public InstalledVoice? SelectedWindowsVoice
        {
            get { return _SelectedWindowsVoice; }
            set { SetField(ref _SelectedWindowsVoice, value); }
        }

        private string _PauseResumeText;

        public string PauseResumeText
        {
            get { return GetField(ref _PauseResumeText); }
            set { SetField(ref _PauseResumeText, value); }
        }

        private SpeechSynthesizer _Synth = new SpeechSynthesizer();

        private SynthesizerState _SpeechState;

        public SynthesizerState SpeechState
        {
            get { return GetField(ref _SpeechState); }
            set 
            { 
                SetField(ref _SpeechState, value);
                RaisePropertyChanged(nameof(AllowChangesToSettings));
            }
        }

        public bool AllowChangesToSettings
        {
            get { return SpeechState == SynthesizerState.Ready; }
        }

        public int SpeechRate
        {
            get { return _Synth.Rate; }
            set 
            { 
                _Synth.Rate = value;
                RaisePropertyChanged(nameof(SpeechRate));
            }
        }


        #endregion Proprties
        public VoxViewModel()
        {
            CommandGenerateSpeech = new CommandBinding(GenerateSpeech);
            CommandPauseResume = new CommandBinding(PauseResume, (o) => SpeechState != SynthesizerState.Ready);
            CommandStop = new CommandBinding(Stop, (o) => SpeechState != SynthesizerState.Ready);
            CommandWindowsSettings = new CommandBinding(WindowsSettings);

            WindowsVoices = _Synth.GetInstalledVoices();

            LoadSettingsFromDisk();

            PauseResumeText = "Pause";
            _Synth.SpeakStarted += _Synth_SpeakStarted;
            _Synth.SpeakCompleted += _Synth_SpeakCompleted;

            //GetWindowsVoices();
        }

        #region Events
        private void _Synth_SpeakCompleted(object? sender, SpeakCompletedEventArgs e)
        {
            PauseResumeText = "Pause";
            SpeechState = _Synth.State;
        }

        private void _Synth_SpeakStarted(object? sender, SpeakStartedEventArgs e)
        {
            PauseResumeText = "Pause";
            SpeechState = _Synth.State;
        }
        #endregion

        #region Commands
        public void GenerateSpeech(object? parameter)
        {
            if (_Synth.State != SynthesizerState.Ready)
            {
                _Synth.SpeakAsyncCancelAll();
                while (_Synth.State != SynthesizerState.Ready)
                    Thread.Sleep(50);
            }

            _Synth.SetOutputToDefaultAudioDevice();

            if (SelectedWindowsVoice != null)
            {
                _Synth.SelectVoice(SelectedWindowsVoice.VoiceInfo.Name);
            }

            var builder = new PromptBuilder();
            builder.StartVoice(new CultureInfo("en-US"));
            builder.AppendText(SpeechText);
            builder.EndVoice();
            Prompt p = _Synth.SpeakAsync(builder);
        }

        public void GetWindowsVoices()
        { 
            var internalVoicesField = typeof(SpeechSynthesizer).GetField("_voiceSynthesis", BindingFlags.NonPublic | BindingFlags.Instance);

            if (internalVoicesField != null)
            {
                // Potentially access and modify the internal voices list
                // This is a simplified example and might not be accurate
                object voice = internalVoicesField.GetValue(_Synth);

                //foreach (InstalledVoice voice in _Synth.GetInstalledVoices())
                //foreach (VoiceInfo info in voices)
                //{
                //    //var info = voice.VoiceInfo;

                //    Debug.WriteLine($"Voice found: {info.Name}, {info.Culture}, {info.Age}, {info.Gender}, {info.Description}, {info.Id}");
                //}
            }

        }

        public void PauseResume(object parameter)
        {
            switch (_Synth.State)
            {
                case SynthesizerState.Speaking:
                    _Synth.Pause();
                    PauseResumeText = "Resume";
                    break;
                case SynthesizerState.Paused:
                    _Synth.Resume();
                    PauseResumeText = "Pause";
                    break;
                case SynthesizerState.Ready:
                default:
                    break;
            }

            SpeechState = _Synth.State;
        }

        public void Stop(object parameter)
        {
            if (SpeechState != SynthesizerState.Ready)
                _Synth.SpeakAsyncCancelAll();
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
                    sw.WriteLine(SelectedWindowsVoice?.VoiceInfo.Name); //Voice
                    sw.WriteLine(SpeechRate); //Rate
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
                        string? voiceName = sr.ReadLine();
                        string? speechRate = sr.ReadLine();

                        if (voiceName != null)
                        {
                            SelectedWindowsVoice = WindowsVoices.FirstOrDefault(v => v.VoiceInfo.Name == voiceName);
                            if (SelectedWindowsVoice == null)
                                SelectedWindowsVoice = WindowsVoices.FirstOrDefault();
                        }

                        if (speechRate != null)
                        {
                            if (int.TryParse(speechRate, out int intRate))
                            {
                                SpeechRate = intRate;
                            }
                        }
                    }
                }
            }
            else
            {
                SelectedWindowsVoice = WindowsVoices.FirstOrDefault();
            }
        }
        #endregion
    }
}
