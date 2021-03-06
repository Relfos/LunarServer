﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LunarLabs.WebServer.Core
{
    public static class MimeUtils
    {
        public static string GetContentType(string fileName, out bool shouldCompress, out bool shouldDownload)
        {
            var ext = Path.GetExtension(fileName);

            string contentType;
            shouldCompress = false;
            shouldDownload = true;

            switch (ext)
            {
                case ".png": contentType = "image/png"; shouldDownload = false; break;
                case ".jpg": contentType = "image/jpeg"; shouldDownload = false; break;
                case ".gif": contentType = "image/gif"; shouldDownload = false; break;
                case ".svg": contentType = "image/svg+xml"; shouldCompress = true; shouldDownload = false; break;


                case ".ogg": contentType = "audio/vorbis"; break;
                case ".mp4": contentType = "audio/mpeg"; break;

                case ".css": contentType = "text/css"; shouldCompress = true; break;
                case ".html": contentType = "text/html"; shouldCompress = true; shouldDownload = false; break;
                case ".csv": contentType = "text/csv"; shouldCompress = true; break;
                case ".txt": contentType = "text/plain"; shouldCompress = true; break;

                case ".js": contentType = "application/javascript"; shouldCompress = true; break;
                case ".json": contentType = "application/json"; shouldCompress = true; shouldDownload = false; break;
                case ".xml": contentType = "application/xml"; shouldCompress = true; break;
                case ".zip": contentType = "application/zip"; break;
                case ".pdf": contentType = "application/pdf"; break;

                default: contentType = "application/octet-stream"; break;
            }

            return contentType;
        }
    }
}
