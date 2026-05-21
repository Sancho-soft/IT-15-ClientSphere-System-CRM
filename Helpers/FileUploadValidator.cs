using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.IO;

namespace ClientSphere.Helpers
{
    public static class FileUploadValidator
    {
        private static readonly HashSet<string> AllowedContentTypes = new(System.StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
        };

        private static readonly HashSet<string> AllowedExtensions = new(System.StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public static bool IsValidImageType(IFormFile file)
        {
            if (file == null) return false;
            var ext = Path.GetExtension(file.FileName);
            return AllowedContentTypes.Contains(file.ContentType)
                && AllowedExtensions.Contains(ext);
        }

        public static bool IsWithinSizeLimit(IFormFile file)
        {
            return file != null && file.Length <= MaxFileSizeBytes;
        }
    }
}
