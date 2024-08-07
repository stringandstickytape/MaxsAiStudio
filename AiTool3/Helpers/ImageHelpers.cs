using Microsoft.VisualBasic.ApplicationServices;
using NAudio.Utils;
using Newtonsoft.Json.Linq;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Media;

namespace AiTool3.Helpers
{
    internal class ImageHelpers
    {
        internal static string GetImageType(string fileName)
        {
            // return the mime type for png, jpg, jpeg, bmp
            var fileExt = Path.GetExtension(fileName).ToLower();
            switch (fileExt)
            {
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".bmp":
                    return "image/bmp";
                default:
                    return "image/png";
            }
        }

        internal static string ImageToBase64(string fileName)
        {
            //open the image file and return it as a base64 string
            using (var image = Image.FromFile(fileName))
            {
                using (var ms = new MemoryStream())
                {
                    image.Save(ms, image.RawFormat);
                    return Convert.ToBase64String(ms.ToArray());
                }
            }
        }

        public static OpenFileDialog ShowAttachImageFileDialog(string defaultPath)
        {
            // prompt the user for an image file.
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = defaultPath;
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.gif;*.bmp";
            openFileDialog.Title = "Select an Image File";
            openFileDialog.Multiselect = false;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.ShowDialog();
            return openFileDialog;
        }

        public static OpenFileDialog ShowAttachTextFilesDialog(string defaultPath)
        {
            // prompt the user for an image file.
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = defaultPath;
            openFileDialog.Filter = "All files|*.*";
            openFileDialog.Title = "Select text file(s)";
            openFileDialog.Multiselect = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.ShowDialog();
            return openFileDialog;
        }
    }
}