using LunarLabs.WebServer.Core;
using NUnit.Framework;

namespace Tests
{
    internal struct DummyStruct
    {
        public int X;
        public int Y;
        public string name;
    }

    public class Tests
    {
        [Test]
        public void TestSessionStructs()
        {
            var storage = new MemorySessionStorage();

            var dummy = new DummyStruct()
            {
                name = "Hello",
                X = 10,
                Y = -20
            };

            var session = storage.CreateSession();

            session.SetStruct<DummyStruct>("entry", dummy);

            var other = session.GetStruct<DummyStruct>("entry");

            Assert.IsTrue(dummy.name == other.name);
            Assert.IsTrue(dummy.X == other.X);
            Assert.IsTrue(dummy.Y == other.Y);
        }
    }
}