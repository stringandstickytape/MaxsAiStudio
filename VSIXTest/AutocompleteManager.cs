using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace VSIXTest
{
    public class AutocompleteManager
    {
        private readonly DTE2 _dte;

        public AutocompleteManager(DTE2 dte)
        {
            _dte = dte;
        }

        public async Task HandleAutocompleteResponseAsync(string content)
        {
            if (content.StartsWith("{\"code="))
            {
                var firstIndex = content.IndexOf("{\"code=");
                if (firstIndex > -1)
                    content = content.Substring(0, firstIndex) + "{\"Code" + content.Substring(firstIndex + 7);
                firstIndex = content.IndexOf("{\"Code=");
                if (firstIndex > -1)
                    content = content.Substring(0, firstIndex) + "{\"Code" + content.Substring(firstIndex + 7);
            }
            var response = JsonConvert.DeserializeObject<AutocompleteResponse>(content);
            if (response != null && !string.IsNullOrEmpty(response.Code))
            {
                await ShowCompletionAsync(response.Code);
            }
        }

        private async Task ShowCompletionAsync(string completionText)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            if (_dte?.ActiveDocument?.Object("TextDocument") is TextDocument textDocument)
            {
                if (textDocument.Selection is TextSelection selection)
                {
                    // Store the starting point
                    var startPoint = selection.ActivePoint.CreateEditPoint();

                    // Insert a carriage return and then the completion text
                    selection.Insert(Environment.NewLine + completionText);

                    // Move the cursor to the start of the inserted text (after the carriage return)
                    var afterCarriageReturn = startPoint.CreateEditPoint();
                    afterCarriageReturn.LineDown(1);
                    afterCarriageReturn.StartOfLine();

                    // Calculate the end point based on the length of the inserted text
                    var endPoint = afterCarriageReturn.CreateEditPoint();
                    endPoint.CharRight(completionText.Length);

                    try
                    {
                        // Attempt to format the inserted text
                        selection.MoveToPoint(afterCarriageReturn);
                        selection.MoveToPoint(endPoint, true);
                        _dte.ExecuteCommand("Edit.FormatSelection");
                    }
                    catch (Exception ex)
                    {
                        // If formatting fails, just continue without formatting
                        System.Diagnostics.Debug.WriteLine($"Formatting failed: {ex.Message}");
                    }

                    // Ensure the inserted text is selected after formatting
                    selection.MoveToPoint(afterCarriageReturn);
                    selection.MoveToPoint(endPoint, true);
                }
            }
        }

        private class AutocompleteResponse
        {
            public string Code { get; set; }
            public string Explanation { get; set; }
        }
    }
}