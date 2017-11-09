using System;
using System.Net;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using LunarParser;
using SynkServer.Core;

namespace SynkServer.HTTP
{
    public class APIRequestException : Exception
    {

        public APIRequestException(string message) : base(message)
        {

        }
    }

    public class API
    {
        private DataFormat format;
        private string mimeType;

        public API(DataFormat format = DataFormat.JSON)
        {
            this.format = format;
            this.mimeType = "application/" + format.ToString().ToLower();
        }

        protected HTTPResponse HandleRequest(HTTPRequest request)
        {
            var url = Utils.FixUrl(request.path);

            if (url.Equals("/favicon.ico"))
            {
                return null;
            }

            DataNode result;
            var query = request.args;

            RouteEntry route = null;// router.Find(url, query);

            if (route != null)
            {
                var handler = route.handler;

                try
                {
                    result = (DataNode) handler(request);
                }
                catch (Exception ex)
                {
                    result = OperationResult(ex.Message);
                }

            }
            else
            {
                result = DataNode.CreateObject("result");
                result.AddField("error", "invalid route");
            }

            var response = new HTTPResponse();
            

            var content = DataFormats.SaveToString(format, result);
            response.bytes = System.Text.Encoding.UTF8.GetBytes(content);

            response.headers["Content-Type"] = mimeType;

            return response;
            
        }

        public static DataNode OperationResult(string errorMsg = null)
        {
            var result = DataNode.CreateObject("operation");
            result.AddField("result", string.IsNullOrEmpty(errorMsg) ? "success" : "error");
            if (!string.IsNullOrEmpty(errorMsg))
            {
                result.AddField("error", errorMsg);
            }
            return result;
        }

    }
}