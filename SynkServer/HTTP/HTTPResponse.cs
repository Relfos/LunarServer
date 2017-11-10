using SynkServer.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SynkServer.HTTP
{
    public enum HTTPCode
    {
        OK = 200,
        Redirect = 303, //https://en.wikipedia.org/wiki/HTTP_302
        BadRequest = 400,
        Unauthorized = 401,
        Forbidden = 403,
        NotFound = 404,
        InternalServerError = 500,
        ServiceUnavailable= 503,
    }

    public class HTTPResponse
    {
        public HTTPCode code;
        public Dictionary<string, string> headers = new Dictionary<string, string>();
        public byte[] bytes;
        public DateTime date;
        public TimeSpan expiration;

        private bool compressed;

        public HTTPResponse()
        {
            this.date = DateTime.UtcNow;
            this.expiration = TimeSpan.FromHours(1);

            headers["Server"] = "LunarServer";
            headers["Connection"] = "close";
            //headers["Content-Type"] = "text/html";
            
            /*
Transfer-Encoding: chunked
Date: Sat, 28 Nov 2009 04:36:25 GMT
Server: LiteSpeed
Connection: close
X-Powered-By: W3 Total Cache/0.8
Pragma: public
Etag: "pub1259380237;gz"
Cache-Control: max-age=3600, public
Content-Type: text/html; charset=UTF-8
Last-Modified: Sat, 28 Nov 2009 03:50:37 GMT
X-Pingback: http://net.tutsplus.com/xmlrpc.php
Content-Encoding: gzip
Vary: Accept-Encoding, Cookie, User-Agent             
             */
        }

        public static object Redirect(string url)
        {
            var result = new HTTPResponse();
            result.code = HTTPCode.Redirect;
            result.bytes = new byte[0];
            result.headers["Location"] = url;
            return result;
        }

        public static HTTPResponse FromString(string content, HTTPCode code = HTTPCode.OK, string contentType= "text/html")
        {
            var result = new HTTPResponse();
            result.code = code;
            result.bytes = System.Text.Encoding.UTF8.GetBytes(content);
            result.headers["Content-Type"] = contentType;
            return result;
        }

        public static HTTPResponse FromFile(string fileName)
        {
            var result = new HTTPResponse();
            result.code = HTTPCode.OK;
            result.bytes = File.ReadAllBytes(fileName);

            var ext = Path.GetExtension(fileName);

            string contentType;
            bool shouldCompress = false;

            switch (ext)
            {
                case ".png": contentType = "image/png"; break;
                case ".jpg": contentType = "image/jpeg"; break;
                case ".gif": contentType = "image/gif"; break;
                case ".svg": contentType = "image/svg+xml"; shouldCompress = true; break;


                case ".ogg": contentType = "audio/vorbis"; break;
                case ".mp4": contentType = "audio/mpeg"; break;

                case ".css": contentType = "text/css"; shouldCompress = true; break;
                case ".html": contentType = "text/html"; shouldCompress = true; break;
                case ".csv": contentType = "text/csv"; shouldCompress = true; break;
                case ".txt": contentType = "text/plain"; shouldCompress = true; break;

                case ".js": contentType = "application/javascript"; shouldCompress = true; break;
                case ".json": contentType = "application/json"; shouldCompress = true; break;
                case ".xml": contentType = "application/xml"; shouldCompress = true; break;
                case ".zip": contentType = "application/zip"; break;
                case ".pdf": contentType = "application/pdf"; break;

                default: contentType = "application/octet-stream"; break;
            }

            result.headers["Content-Type"] = contentType;

            result.headers["Content-Description"] = "File Transfer";

            result.headers["Content-Disposition"] = "attachment; filename=\"" + fileName + "\"";
            result.headers["Content-Transfer-Encoding"] = "binary";
            result.headers["Connection"] =  "Keep-Alive";
            result.headers["Expires"] = "0";
            result.headers["Cache-Control"] = "must-revalidate, post-check=0, pre-check=0";
            result.headers["Pragma"] = "public";


            var lastModified = System.IO.File.GetLastWriteTime(fileName);
            result.headers["Last-Modified"] = lastModified.ToString("r");

            if (shouldCompress && result.bytes.Length > 1400 && result.bytes.Length < 1024 * 128)
            {
                result.Compress();
            }

            return result;
        }


        public static HTTPResponse FromBytes(byte[] bytes, bool compress = false, string contentType = "application/octet-stream")
        {
            var result = new HTTPResponse();
            result.code = HTTPCode.OK;
            result.bytes = bytes;

            result.headers["Content-Type"] = contentType;

            return result;
        }

        public void Compress()
        {
            if (compressed)
            {
                return;
            }

            this.bytes = this.bytes.GZIPCompress();
            this.headers["Content-Encoding"] = "gzip";

            compressed = true;
        }

    }
}
