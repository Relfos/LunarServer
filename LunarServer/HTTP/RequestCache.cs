using LunarLabs.WebServer.HTTP;
using System;
using System.Collections.Generic;

namespace LunarLabs.WebServer.HTTP
{
    internal struct RequestCacheEntry
    {
        public readonly HTTPResponse Response;
        public readonly DateTime dateTime;

        public RequestCacheEntry(HTTPResponse response, DateTime dateTime)
        {
            Response = response;
            this.dateTime = dateTime;
        }
    }

    internal class RequestCache
    {
        private Dictionary<string, RequestCacheEntry> _cachedResponses = new Dictionary<string, RequestCacheEntry>();

        public HTTPResponse GetCachedResponse(string path, int maxSeconds)
        {
            if (_cachedResponses.ContainsKey(path))
            {
                var entry = _cachedResponses[path];
                var diff = DateTime.UtcNow - entry.dateTime;
                if (diff.TotalSeconds <= maxSeconds)
                {
                    return entry.Response;
                }
            }

            return null;
        }

        public void PutCachedResponse(string path, HTTPResponse response)
        {
            var entry = new RequestCacheEntry(response, DateTime.UtcNow);
            _cachedResponses[path] = entry;
        }
    }
}
