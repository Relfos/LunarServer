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
        Redirect = 302, //https://en.wikipedia.org/wiki/HTTP_302
        NotModified = 304,
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
        public TimeSpan expiration = TimeSpan.FromSeconds(0);

        public HTTPResponse()
        {
            this.date = DateTime.UtcNow;

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

        public static HTTPResponse Redirect(string url)
        {
            var result = new HTTPResponse();
            result.code = HTTPCode.Redirect;
            result.bytes = new byte[0];
            result.headers["Location"] = url;
            return result;
        }

        public static HTTPResponse NotModified()
        {
            var result = new HTTPResponse();
            result.code = HTTPCode.NotModified;
            result.bytes = new byte[0];
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
        
        public static HTTPResponse FromBytes(byte[] bytes, string contentType = "application/octet-stream")
        {
            var result = new HTTPResponse();
            result.code = HTTPCode.OK;
            result.bytes = bytes;

            result.headers["Content-Type"] = contentType;

            return result;
        }

    }
}
