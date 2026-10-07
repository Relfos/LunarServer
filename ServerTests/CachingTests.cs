using System.Text;
using LunarLabs.WebServer.Core;
using LunarLabs.WebServer.HTTP;
using NUnit.Framework;
namespace Tests
{
    public class CachingTests
    {
        private static HTTPRequest Request(string path, string url = null)
        {
            return new HTTPRequest { method = HTTPRequest.Method.Get, path = path, url = url ?? path };
        }
        private static string Body(HTTPResponse response) { return Encoding.UTF8.GetString(response.bytes); }

        [TestCase("/public", "/public", "/public", false, 60, true)]
        [TestCase("/booking/{id}", "/booking/123", "/booking/123", false, 60, false)]
        [TestCase("/assets/*", "/assets/test", "/assets/test", false, 60, false)]
        [TestCase("/status", "/status", "/status?flag", false, 60, false)]
        [TestCase("/status", "/status", "/status", true, 60, false)]
        [TestCase("/public", "/public", "/public", false, 0, false)]
        public void CacheEligibility(string route, string path, string url, bool arguments, int seconds, bool cached)
        {
            using (var server = new HTTPServer(new ServerSettings { CacheResponseTime = seconds }))
            {
                int calls = 0;
                server.Get(route, request => (++calls).ToString());
                for (int i = 1; i <= 2; i++)
                {
                    var request = Request(path, url);
                    if (arguments) request.args["flag"] = "true";
                    Assert.AreEqual((cached ? 1 : i).ToString(), Body(server.HandleRequest(request)));
                }
                Assert.AreEqual(cached ? 1 : 2, calls);
            }
        }
        [Test]
        public void QueryRequestsNeitherReadNorPopulateLiteralCache()
        {
            using (var server = new HTTPServer(new ServerSettings { CacheResponseTime = 60 }))
            {
                int calls = 0;
                server.Get("/status", request => (++calls).ToString());
                var first = Request("/status", "/status?booking=1");
                first.args["booking"] = "1";
                Assert.AreEqual("1", Body(server.HandleRequest(first)));
                Assert.AreEqual("2", Body(server.HandleRequest(Request("/status"))));
                var second = Request("/status", "/status?booking=2");
                second.args["booking"] = "2";
                Assert.AreEqual("3", Body(server.HandleRequest(second)));
                Assert.AreEqual("2", Body(server.HandleRequest(Request("/status"))));
                Assert.AreEqual(3, calls);
            }
        }
    }
}
