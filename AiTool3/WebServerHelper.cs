using AiTool3.UI;
using System.Net;
using System.Text;

namespace AiTool3
{
    internal static class WebServerHelper
    {


        public async static Task CreateWebServerAsync(ChatWebView chatWebView, Func<Task<string>> fetchAiInputResponse)
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://+:8080/");
            listener.Start();

            Console.WriteLine("Web server started. Listening on http://*:8080/");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                if (request.HttpMethod == "GET")
                {
                    string responseString = @"<!DOCTYPE html>
<html>
<head>
    <title>AI Chat</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <style>
        #voiceButton {
            background-color: #4CAF50;
            border: none;
            color: white;
            padding: 10px 20px;
            text-align: center;
            text-decoration: none;
            display: inline-block;
            font-size: 16px;
            margin: 4px 2px;
            cursor: pointer;
        }
    </style>
</head>
<body>
    <h1>AI Chat</h1>
    <p><strong>You:</strong> {WebUtility.HtmlEncode(userInput)}</p>
    <p><strong>AI:</strong> {WebUtility.HtmlEncode(responseText)}</p>
    <form method='post' id='chatForm'>
        <input type='text' name='userInput' id='userInput' placeholder='Enter your message'>
        <input type='submit' value='Send'>
        <button type='button' id='voiceButton'>Voice</button>
    </form>

    <script>
        const voiceButton = document.getElementById('voiceButton');
        const userInput = document.getElementById('userInput');
        const chatForm = document.getElementById('chatForm');

        if ('SpeechRecognition' in window || 'webkitSpeechRecognition' in window) {
            const SpeechRecognition = window.SpeechRecognition || window.webkitSpeechRecognition;
            const recognition = new SpeechRecognition();
            recognition.continuous = false;
            recognition.interimResults = false;
            recognition.lang = 'en-US';

            voiceButton.addEventListener('click', () => {
                recognition.start();
                voiceButton.textContent = 'Listening...';
            });

            recognition.onresult = (event) => {
                const transcript = event.results[0][0].transcript;
                userInput.value = transcript;
                voiceButton.textContent = 'Voice';
            };

            recognition.onend = () => {
                voiceButton.textContent = 'Voice';
                chatForm.submit();
            };

            recognition.onerror = (event) => {
                console.error('Speech recognition error:', event.error);
                voiceButton.textContent = 'Voice';
            };
        } else {
            voiceButton.style.display = 'none';
            console.log('Speech Recognition API is not supported in this browser.');
        }
    </script>
</body>
</html>";

                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }
                else if (request.HttpMethod == "POST")
                {
                    string userInput = "";
                    using (var reader = new StreamReader(request.InputStream, request.ContentEncoding))
                    {
                        string requestBody = await reader.ReadToEndAsync();
                        userInput = WebUtility.UrlDecode(requestBody.Split('=')[1]);
                    }

                    // Populate the chatwebview user input
                    await chatWebView.SetUserPrompt(userInput);

                    // Fetch AI response using the func
                    var responseText = await fetchAiInputResponse();


                    string responseString = $@"<!DOCTYPE html>
<html>
<head>
    <title>AI Chat</title>
    <meta name=""viewport"" content=""width=device-width, initial-scale=1"">
    <style>
        #voiceButton {{
            background-color: #4CAF50;
            border: none;
            color: white;
            padding: 10px 20px;
            text-align: center;
            text-decoration: none;
            display: inline-block;
            font-size: 16px;
            margin: 4px 2px;
            cursor: pointer;
        }}
    </style>
</head>
<body>
    <h1>AI Chat</h1>
    <p><strong>You:</strong> {{WebUtility.HtmlEncode(userInput)}}</p>
    <p><strong>AI:</strong> {{WebUtility.HtmlEncode(responseText)}}</p>
    <form method='post' id='chatForm'>
        <input type='text' name='userInput' id='userInput' placeholder='Enter your message'>
        <input type='submit' value='Send'>
        <button type='button' id='voiceButton'>Voice</button>
    </form>

    <script>
        const voiceButton = document.getElementById('voiceButton');
        const userInput = document.getElementById('userInput');
        const chatForm = document.getElementById('chatForm');

        if ('webkitSpeechRecognition' in window) {{
            const recognition = new webkitSpeechRecognition();
            recognition.continuous = false;
            recognition.interimResults = false;
            recognition.lang = 'en-US';

            voiceButton.addEventListener('click', () => {{
                recognition.start();
                voiceButton.textContent = 'Listening...';
            }});

            recognition.onresult = (event) => {{
                const transcript = event.results[0][0].transcript;
                userInput.value = transcript;
                voiceButton.textContent = 'Voice';
            }};

            recognition.onend = () => {{
                voiceButton.textContent = 'Voice';
                chatForm.submit();
            }};

            recognition.onerror = (event) => {{
                console.error('Speech recognition error:', event.error);
                voiceButton.textContent = 'Voice';
            }};
        }} else {{
            voiceButton.style.display = 'none';
            console.log('Web Speech API is not supported in this browser.');
        }}
    </script>
</body>
</html>";

                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }

                response.Close();
            }
        }
    }
}