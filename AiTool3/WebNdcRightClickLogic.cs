using AiTool3.Conversations;
using Microsoft.CodeAnalysis;
using System.Data;
using System.Diagnostics;
using System.Text;

namespace AiTool3
{
    public static class WebNdcRightClickLogic
    {
        public static void ProcessWebNdcContextMenuOption(List<CompletionMessage> nodes, string option)
        {
            switch (option)
            {
                case "saveTxt":
                    // pretty-print the conversation from the nodes list
                    string conversation = nodes.Aggregate("", (acc, node) => acc + $"{node.Role.ToString()}: {node.Content}" + "\n\n");

                    // get a filename from the user
                    SaveFileDialog saveFileDialog = new SaveFileDialog();
                    saveFileDialog.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                    saveFileDialog.RestoreDirectory = true;
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(saveFileDialog.FileName, conversation);
                        // open the file in default handler
                        Process.Start(new ProcessStartInfo(saveFileDialog.FileName) { UseShellExecute = true });
                    }
                    break;
                case "saveHtml":

                    StringBuilder htmlBuilder = new StringBuilder();
                    htmlBuilder.Append(@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Conversation Export</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            line-height: 1.6;
            color: #e0e0e0;
            max-width: 800px;
            margin: 0 auto;
            padding: 20px;
            background-color: #1a1a1a;
        }
        .conversation {
            background-color: #1a1a1a;
            border-radius: 8px;
            overflow: hidden;
        }
        .message {
            background-color: #2a2a2a;
            padding: 15px;
            margin-bottom: 10px;
            border-radius: 12px;
            transition: max-height 1s ease, background-color 0.5s ease;
            max-height: 200px;
            overflow: hidden;
            cursor: pointer;
            position: relative;
            box-shadow: 0 2px 4px rgba(0, 0, 0, 0.3);
        }
        .message.expanded {
            max-height: 20000000px;
        }
        .message:last-child {
            margin-bottom: 0;
        }
        .message:hover {
            background-color: #333333;
        }
        .role {
            font-weight: bold;
            color: #b0b0b0;
            margin-bottom: 5px;
        }
        .content {
            white-space: pre-wrap;
        }
        .human .role {
            color: #64b5f6;
        }
        .assistant .role {
            color: #81c784;
        }
        .more {
            position: absolute;
            bottom: 0;
            left: 0;
            right: 0;
            height: 40px;
            background: linear-gradient(to bottom, rgba(42, 42, 42, 0) 0%, rgba(42, 42, 42, 1) 100%);
            display: flex;
            align-items: flex-end;
            justify-content: center;
            padding-bottom: 5px;
            transition: opacity 0.8s ease;
            border-bottom-left-radius: 12px;
            border-bottom-right-radius: 12px;
        }
        .more span {
            background-color: rgba(255, 255, 255, 0.2);
            color: #e0e0e0;
            padding: 2px 8px;
            border-radius: 10px;
            font-size: 14px;
            transition: transform 0.8s ease;
        }
        .message.expanded .more {
            opacity: 0;
            pointer-events: none;
        }
        .message.expanded .more span {
            transform: rotate(180deg);
        }
        @media (max-width: 600px) {
            body {
                padding: 10px;
            }
            .message {
                padding: 10px;
                margin-bottom: 8px;
            }
        }
    </style>
    <script>
        function toggleExpand(element) {
            element.classList.toggle('expanded');
            checkOverflow(element);
        }

        function checkOverflow(element) {
            const content = element.querySelector('.content');
            const more = element.querySelector('.more');
            if (content.scrollHeight > element.clientHeight) {
                more.style.display = 'flex';
            } else {
                more.style.display = 'none';
            }
        }

        window.onload = function() {
            document.querySelectorAll('.message').forEach(checkOverflow);
        };
    </script>
</head>
<body>
    <div class='conversation'>
");

                    foreach (var node in nodes.Where(x => !x.Omit))
                    {
                        string roleClass = node.Role.ToString().ToLower();
                        htmlBuilder.Append($@"
        <div class='message {roleClass}' onclick='toggleExpand(this)'>
            <div class='role'>{node.Role}:</div>
            <div class='content'>{System.Web.HttpUtility.HtmlEncode(node.Content)}</div>
            <div class='more'><span>more...</span></div>
        </div>
");
                    }

                    htmlBuilder.Append(@"
    </div>
</body>
</html>
");

                    SaveFileDialog saveFileDialog2 = new SaveFileDialog();
                    saveFileDialog2.Filter = "HTML files (*.html)|*.html|All files (*.*)|*.*";
                    saveFileDialog2.RestoreDirectory = true;
                    if (saveFileDialog2.ShowDialog() == DialogResult.OK)
                    {
                        System.IO.File.WriteAllText(saveFileDialog2.FileName, htmlBuilder.ToString());
                        Process.Start(new ProcessStartInfo(saveFileDialog2.FileName) { UseShellExecute = true });
                    }
                    break;
                default:
                    throw new NotImplementedException();
            }
        }
    }

}
