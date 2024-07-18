
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AiTool3.Helpers
{
    public static class FileTypeClassifier
    {
        public enum FileClassification
        {
            Video,
            Audio,
            Image,
            Text
        }

        private static readonly HashSet<string> VideoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp4", ".avi", ".mov", ".wmv", ".flv", ".webm", ".mkv", ".m4v", ".mpeg", ".mpg", ".3gp", ".3g2",
        ".vob", ".ogv", ".drc", ".gifv", ".mng", ".mts", ".m2ts", ".ts", ".mov", ".qt", ".yuv", ".rm",
        ".rmvb", ".viv", ".asf", ".amv", ".m4p", ".mpv", ".svi", ".mxf", ".roq", ".nsv", ".f4v", ".f4p",
        ".f4a", ".f4b"
    };

        private static readonly HashSet<string> AudioExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".mp3", ".wav", ".wma", ".aac", ".flac", ".ogg", ".m4a", ".oga", ".opus", ".3gp", ".aa", ".aax",
        ".aiff", ".alac", ".amr", ".ape", ".awb", ".dss", ".dvf", ".gsm", ".iklax", ".ivs", ".m4b",
        ".m4p", ".mmf", ".mpc", ".msv", ".nmf", ".nsf", ".mogg", ".ra", ".rm", ".raw", ".rf64", ".sln",
        ".tta", ".voc", ".vox", ".wv", ".webm", ".8svx", ".cda"
    };

        private static readonly HashSet<string> ImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp", ".svg", ".raw", ".ico", ".psd",
        ".ai", ".eps", ".indd", ".cr2", ".nef", ".orf", ".sr2", ".heic", ".avif", ".jfif", ".exif",
        ".arw", ".crw", ".dcr", ".dng", ".erf", ".kdc", ".mrw", ".pef", ".raf", ".rw2", ".yuv",
        ".pbm", ".pgm", ".ppm", ".pnm", ".tga", ".xcf", ".cgm", ".wmf", ".emf", ".art", ".xar",
        ".pct", ".pict", ".wpg", ".pcx", ".iff", ".lbm", ".mac", ".msp", ".sgi", ".tif", ".vtf"
    };

        public static FileClassification GetFileClassification(string fileExtension)
        {
            if (string.IsNullOrEmpty(fileExtension))
            {
                return FileClassification.Text;
            }

            fileExtension = fileExtension.StartsWith(".") ? fileExtension : "." + fileExtension;

            if (VideoExtensions.Contains(fileExtension))
            {
                return FileClassification.Video;
            }
            else if (AudioExtensions.Contains(fileExtension))
            {
                return FileClassification.Audio;
            }
            else if (ImageExtensions.Contains(fileExtension))
            {
                return FileClassification.Image;
            }
            else
            {
                return FileClassification.Text;
            }
        }
    }
}
