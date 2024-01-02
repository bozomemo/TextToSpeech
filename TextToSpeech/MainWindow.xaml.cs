using Google.Cloud.Speech.V1;
using Google.Cloud.TextToSpeech.V1;
using Google.Protobuf;
using NAudio.Wave;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using static Google.Cloud.Speech.V1.Speech;

namespace TextToSpeech
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly TextToSpeechClient _client;
        private readonly Google.Cloud.Speech.V1.SpeechClient _speechClient;
        private bool _isRecording = false;
        private readonly AudioRecorder _recorder;
        public MainWindow()
        {
            InitializeComponent();

            _client = TextToSpeechClient.Create();
            _speechClient = Google.Cloud.Speech.V1.SpeechClient.Create();
            _recorder = new AudioRecorder();
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var text = Txt_TextToSpeech.Text;

            var input = new SynthesisInput
            {
                Text = text
            };

            var voiceSelection = new VoiceSelectionParams
            {
                LanguageCode = "en-US",
                Name = SpeechModels.SelectedItem.ToString()
            };

            var audioConfig = new AudioConfig
            {
                AudioEncoding = AudioEncoding.Linear16,
            };

            var response = _client.SynthesizeSpeech(input, voiceSelection, audioConfig);

            using (var memoryStream = new MemoryStream())
            {
                response.AudioContent.WriteTo(memoryStream);
                memoryStream.Position = 0; // Reset memory stream position
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(memoryStream);
                player.Play(); // PlaySync will play it synchronously
            }

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var voices = _client.ListVoices("en-US");

            foreach (var voice in voices.Voices)
            {
                SpeechModels.Items.Add(voice.Name);
            }
        }

        private async void ConvertToText(object sender, RoutedEventArgs e)
        {
            _recorder.StopRecording();
            Txt_SpeechToText.Text = "Converting to text...";
            Txt_SpeechToText.Text = await RecognizeSpeechAsync(_recorder.MemoryStream.ToArray());
        }

        private void StartRecording(object sender, RoutedEventArgs e)
        {
            if (!_isRecording)
            {
                _recorder.StartRecording();
                Txt_SpeechToText.Text = "Recording...";
            }
        }
        private async Task<string> RecognizeSpeechAsync(byte[] audioData)
        {
            RecognitionConfig config = new()
            {
                Encoding = RecognitionConfig.Types.AudioEncoding.Linear16,
                SampleRateHertz = 44100, // Adjust based on your audio sample rate
                LanguageCode = "en-US", // Turkish language code
            };

            RecognitionAudio audio = RecognitionAudio.FromBytes(_recorder.MemoryStream.ToArray());

            RecognizeResponse response = await _speechClient.RecognizeAsync(config, audio);
            if (response.Results.Count > 0)
            {
                return response.Results[0].Alternatives[0].Transcript;
            }
            else
            {
                return "No speech recognized.";
            }
        }
        public class AudioRecorder
        {
            private WaveIn _waveIn;
            private MemoryStream _memoryStream;
            private WaveFileWriter _writer;

            public MemoryStream MemoryStream => _memoryStream;

            public void StartRecording()
            {
                // Initialize the WaveIn object
                _waveIn = new WaveIn();
                _waveIn.WaveFormat = new WaveFormat(44100, 1); // Sample Rate: 44100 Hz, Channels: 1 (Mono)

                // Initialize the MemoryStream and bind it with WaveFileWriter
                _memoryStream = new MemoryStream();
                _writer = new WaveFileWriter(_memoryStream, _waveIn.WaveFormat);

                // Attach event handler for incoming data
                _waveIn.DataAvailable += WaveIn_DataAvailable;

                // Start recording
                _waveIn.StartRecording();
            }

            private void WaveIn_DataAvailable(object sender, WaveInEventArgs e)
            {
                // Write the data to the memory stream
                if (_writer != null)
                {
                    _writer.Write(e.Buffer, 0, e.BytesRecorded);
                    _writer.Flush();
                }
            }

            public void StopRecording()
            {
                // Stop recording
                _waveIn.StopRecording();
                _waveIn.Dispose();
                _waveIn = null;

                // Finalize the WAV file
                _writer.Dispose();
                _writer = null;

                // At this point, memoryStream contains the recorded data
                // You can access it via memoryStream.ToArray() or similar methods
            }
        }
    }
}