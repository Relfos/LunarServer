using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using LunarLabs.Parser;
using LunarLabs.Parser.JSON;
using System;
using System.Collections.Generic;

namespace LunarLabs.WebServer.Protocols
{
    public class RPCException: Exception
    {
        public RPCException(string msg) : base(msg)
        {

        }
    }

    public class RPCPlugin : ServerPlugin
    {
        private Dictionary<string, Func<DataNode, object>> _handlers = new Dictionary<string, Func<DataNode, object>>();

        public RPCPlugin(HTTPServer server, string path = null) : base(server, path)
        {
        }

        public void RegisterHandler(string methodName, Func<DataNode, object> handler)
        {
            _handlers[methodName] = handler;
        }

        protected override bool OnInstall()
        {
           Server.Get(this.Path, (request) =>
            {
                var version = request.GetVariable("jsonrpc");
                if (version != "2" && version != "2.0")
                {
                    return GenerateRPCError("Invalid jsonrpc version", -32602);
                }

                var method = request.GetVariable("method");
                var id = request.GetVariable("id");
                if (string.IsNullOrEmpty(id))
                {
                    id = "0";
                }

                var encodedParams = request.GetVariable("params");

                var decodedParams = encodedParams.UrlDecode();

                DataNode paramNode;
                try
                {
                    paramNode = JSONReader.ReadFromString(decodedParams);
                }
                catch
                {
                    return GenerateRPCError("Parsing error", -32700);
                }

                return HandleRPCRequest(id, method, paramNode);

            });

            Server.Post(this.Path, (request) =>
            {
                if (string.IsNullOrEmpty(request.postBody))
                {
                    return GenerateRPCError("Invalid request", -32600);
                }
                else
                {
                    DataNode root;
                    try
                    {
                        root = JSONReader.ReadFromString(request.postBody);
                    }
                    catch
                    {
                        return GenerateRPCError("Parsing error", -32700);
                    }

                    var version = root.GetString("jsonrpc");
                    if (version != "2" && version != "2.0")
                    {
                        return GenerateRPCError("Invalid jsonrpc version", -32602);
                    }

                    var method = root.GetString("method");
                    var id = root.GetString("id", "0");

                    var paramNode = root.GetNode("params");

                    return HandleRPCRequest(id, method, paramNode);
                }

            });

            return true;
        }

        private object HandleRPCRequest(string id, string method, DataNode paramNode)
        {
            object result = null;
            if (_handlers.ContainsKey(method))
            {
                if (paramNode == null)
                {
                    return GenerateRPCError("Invalid params", -32602);
                }

                try
                {
                    var handler = _handlers[method];
                    result = handler(paramNode);
                }
                catch (RPCException e)
                {
                    return GenerateRPCError(e.Message, -32603);
                }
                catch
                {
                    return GenerateRPCError("Internal error", -32603);
                }

            }
            else
            { 
                    return GenerateRPCError("Method not found", -32601);
            }

            if (result == null)
            {
                return GenerateRPCError("Missing result", -32603);
            }

            if (result is DataNode)
            {
                var content = JSONWriter.WriteToString((DataNode)result);
                return "{\"jsonrpc\": \"2.0\", \"result\": " + content + ", \"id\": \"" + id + "\"}";
            }
            else
            {
                return GenerateRPCError("Not implemented", -32603);
            }

        }

        private string GenerateRPCError(string msg, int code = -32000, int id = 0)
        {
            return "{\"jsonrpc\": \"2.0\", \"error\": {\"code\": " + code + ", \"message\": \"" + msg + "\"}, \"id\": \"" + id + "\"}";
        }
    }
}
