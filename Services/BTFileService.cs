﻿using Microsoft.AspNetCore.Http;
using MVC_BugTracker.Services.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MVC_BugTracker.Services
{
    public class BTFileService : IBTFileService
    {
        private const int DefaultMaxFileSize = (2 * 1024 * 1024);

        public string ContentType(IFormFile file)
        {
            return file?.ContentType.Split('/')[1];
        }

        public string DecodeImage(byte[] data, string type)
        {
            if (data is null || type is null) return null;
            return $"data:image/{type};base64,{Convert.ToBase64String(data)}";
        }

        public async Task<byte[]> EncodeFileAsync(IFormFile file)
        {
            if (file is null) return null;

            // This triggers more aggressive garbage collection (using statement)
            using var ms = new MemoryStream();
            await file.CopyToAsync(ms);
            return ms.ToArray();
        }

        // Using SixLabors
        public byte[] EncodeFileAsync(Image image, string contentType)
        {
            if (image is null) return null;

            using (var ms = new MemoryStream())
            {
                // need type for a switch statement
                switch (contentType)
                {
                    case "png":
                        image.SaveAsPng(ms);
                        break;
                    case "jpeg":
                        image.SaveAsJpeg(ms);
                        break;
                    default:
                        image.SaveAsPng(ms);
                        break;
                }
                
                return ms.ToArray();
            }

        }

        public async Task<byte[]> EncodeFileAsync(string fileName)
        {
            var file = $"{Directory.GetCurrentDirectory()}/wwwroot/img/{fileName}";
            return await File.ReadAllBytesAsync(file);
        }

        public int Size(IFormFile file)
        {
            return Convert.ToInt32(file?.Length);
        }

        public bool ValidateFileSize(IFormFile file)
        {
            return Size(file) < DefaultMaxFileSize;
        }

        public bool ValidateFileSize(IFormFile file, int maxSize)
        {
            return Size(file) < maxSize;
        }

        public bool ValidateFileType(IFormFile file)
        {
            var authorizedTypes = new List<string> { ".jpg", ".png", ".gif", ".doc", ".docx", ".xls", ".xlsx", ".pdf" };
            var validExt = authorizedTypes.Contains(ContentType(file));
            return validExt;
        }

        public bool ValidateFileType(IFormFile file, List<string> fileTypes)
        {
            var validExt = fileTypes.Contains(ContentType(file));
            return validExt;
        }
    }
}
