// AiStudio4/InjectedDependencies/RequestHandlers/ClipboardImageRequestHandler.cs
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace AiStudio4.InjectedDependencies.RequestHandlers
{
    /// <summary>
    /// Handles clipboard image-related requests
    /// </summary>
    public class ClipboardImageRequestHandler
    {
        /// <summary>
        /// Handles a clipboard image request
        /// </summary>
        /// <param name="clientId">The ID of the client making the request</param>
        /// <param name="requestData">The request data as a JSON string</param>
        /// <returns>A JSON string response to send back to the client</returns>
        public async Task<string> HandleClipboardImageRequest(string clientId, string requestData)
        {
            try
            {
                // Call helper method to get clipboard image as attachment
                var attachment = await GetClipboardImageAttachmentAsync();
                if (attachment == null)
                {
                    return SerializeError("No image found in clipboard.");
                }
                return JsonConvert.SerializeObject(new { success = true, attachment });
            }
            catch (Exception ex)
            {
                return SerializeError($"Failed to get clipboard image: {ex.Message}");
            }
        }

        // Helper: Extracts image from clipboard and returns as attachment object
        private async Task<object> GetClipboardImageAttachmentAsync()
        {
            // Clipboard access must be on STA thread
            System.Drawing.Bitmap bitmap = null;
            await System.Threading.Tasks.Task.Run(() =>
            {
                var thread = new System.Threading.Thread(() =>
                {
                    if (System.Windows.Clipboard.ContainsImage())
                    {
                        var source = System.Windows.Clipboard.GetImage();
                        if (source != null)
                        {
                            using (var ms = new System.IO.MemoryStream())
                            {
                                var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
                                encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(source));
                                encoder.Save(ms);
                                ms.Position = 0;
                                bitmap = new System.Drawing.Bitmap(ms);
                            }
                        }
                    }
                });
                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.Start();
                thread.Join();
            });

            if (bitmap == null)
                return null;

            // Convert bitmap to PNG byte[]
            byte[] pngBytes;
            using (var ms = new System.IO.MemoryStream())
            {
                bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                pngBytes = ms.ToArray();
            }

            // Prepare attachment object
            var attachment = new
            {
                id = Guid.NewGuid().ToString(),
                name = "clipboard-image.png",
                type = "image/png",
                size = pngBytes.Length,
                content = Convert.ToBase64String(pngBytes),
                width = bitmap.Width,
                height = bitmap.Height,
                lastModified = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };
            bitmap.Dispose();
            return attachment;
        }

        private string SerializeError(string message) => JsonConvert.SerializeObject(new { success = false, error = message });
    }
}