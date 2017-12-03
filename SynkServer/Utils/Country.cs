using System.IO;

namespace SynkServer.Utils
{
    public static class CountryUtils
    {
        public struct IPRange
        {
            public uint start;
            public uint end;
            public string code;
        }

        private static IPRange[] _ipRanges;

        private static void InitRanges()
        {
            var lines = File.ReadAllLines("ranges.txt");
            _ipRanges = new IPRange[lines.Length];
            for (int i=0; i<lines.Length; i++)
            {
                var line = lines[i];
                string[] s = line.Split(',');
                var range = new IPRange();
                uint.TryParse(s[0], out range.start);
                uint.TryParse(s[1], out range.end);
                range.code = s[2];

                _ipRanges[i] = range;
            }
        }

        public static string IPToCountry(uint ip)
        {
            if (_ipRanges == null)
            {
                InitRanges();
            }

            foreach (IPRange p in _ipRanges)
            if (ip >= p.start && ip <= p.end)
            {
                return p.code;
            }

            return null;
        }

    }
}
