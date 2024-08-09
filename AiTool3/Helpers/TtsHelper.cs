using System.Speech.Synthesis;

namespace AiTool3.Helpers
{
    public static class TtsHelper
    {

        public static void ReadAloud(string txt)
        {
            using (SpeechSynthesizer synthesizer = new SpeechSynthesizer())
            {
                // Configure the synthesizer
                synthesizer.SetOutputToDefaultAudioDevice();

                // Get available voices
                foreach (InstalledVoice voice in synthesizer.GetInstalledVoices())
                {
                    Console.WriteLine($"Voice: {voice.VoiceInfo.Name}");
                }

                // Select a specific voice (optional)
                // synthesizer.SelectVoice("Microsoft David Desktop");

                // Adjust speech settings (optional)
                synthesizer.Rate = 0; // Range: -10 to 10
                synthesizer.Volume = 100; // Range: 0 to 100

                // Speak synchronously
                //synthesizer.Speak("Hello, this is an example of Windows Text-to-Speech using C#.");

                // Speak asynchronously
                //synthesizer.SpeakAsync("This is an asynchronous speech example.");

                // Use SSML for more advanced control
                string ssml = $@"
                <speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-GB'>
                    <voice gender='female'>
                        <prosody rate='+20%' pitch='+5%'>
                            {txt}
                        </prosody>
                    </voice>
                </speak>";

                synthesizer.SpeakSsml(ssml);

            }
        }
    }
}