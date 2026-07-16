using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System;

namespace ClientSphere.Services
{
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file, string folderName);
    }

    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var account = new Account(
                config["Cloudinary:CloudName"],
                config["Cloudinary:ApiKey"],
                config["Cloudinary:ApiSecret"]);

            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }

        private static bool ValidateImageHeaders(System.IO.Stream stream)
        {
            var jpeg = new byte[] { 0xFF, 0xD8, 0xFF };
            var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
            var gif = new byte[] { 0x47, 0x49, 0x46, 0x38 }; // "GIF8"
            var webp = new byte[] { 0x52, 0x49, 0x46, 0x46 }; // RIFF header for webp

            var buffer = new byte[12];
            int read = stream.Read(buffer, 0, 12);
            stream.Position = 0; // Reset stream position

            if (read < 4) return false;

            if (System.Linq.Enumerable.SequenceEqual(System.Linq.Enumerable.Take(buffer, jpeg.Length), jpeg)) return true;
            if (read >= png.Length && System.Linq.Enumerable.SequenceEqual(System.Linq.Enumerable.Take(buffer, png.Length), png)) return true;
            if (System.Linq.Enumerable.SequenceEqual(System.Linq.Enumerable.Take(buffer, gif.Length), gif)) return true;

            // For WEBP, check RIFF at start and WEBP at index 8
            if (System.Linq.Enumerable.SequenceEqual(System.Linq.Enumerable.Take(buffer, webp.Length), webp))
            {
                var webpSignature = new byte[] { 0x57, 0x45, 0x42, 0x50 }; // "WEBP"
                if (read >= 12 && System.Linq.Enumerable.SequenceEqual(System.Linq.Enumerable.Take(System.Linq.Enumerable.Skip(buffer, 8), 4), webpSignature))
                {
                    return true;
                }
            }
            return false;
        }

        public async Task<string?> UploadImageAsync(IFormFile file, string folderName)
        {
            if (file == null || file.Length == 0) return null;

            // Security: Enforce 5MB size limit
            if (file.Length > 5 * 1024 * 1024)
            {
                throw new Exception("File size exceeds the 5MB limit.");
            }

            // Security: Enforce MIME type whitelist
            var allowedTypes = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
            if (!System.Linq.Enumerable.Contains(allowedTypes, file.ContentType.ToLower()))
            {
                throw new Exception("Invalid file type. Only JPG, PNG, GIF, and WEBP images are allowed.");
            }

            // Security: Enforce Magic Bytes Validation
            using (var validationStream = file.OpenReadStream())
            {
                if (!ValidateImageHeaders(validationStream))
                {
                    throw new Exception("Invalid file content. The file signature does not match a valid image.");
                }
            }

            // If keys are not set, simulate an upload for development
            if (_cloudinary.Api.Account.Cloud == "YOUR_CLOUDINARY_CLOUD_NAME" || string.IsNullOrEmpty(_cloudinary.Api.Account.Cloud))
            {
                 // Return a placeholder or null if not configured
                 return null;
            }

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folderName,
                // You can add transformations here
                // Transformation = new Transformation().Height(500).Width(500).Crop("fill")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception($"Cloudinary upload failed: {uploadResult.Error.Message}");
            }

            return uploadResult.SecureUrl.ToString();
        }
    }
}
