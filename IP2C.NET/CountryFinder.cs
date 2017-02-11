using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace IP2C.Net
{
    public class IPCountryFinder
    {
        Dictionary<string, string> CountryNames = new Dictionary<string, string>();
        Dictionary<uint, string> IP2CN = new Dictionary<uint, string>();
        uint[] IPRanges;
        public string DupDataInfo = string.Empty;

        public IPCountryFinder(string path)
        {
            if (!File.Exists(path)) throw new ArgumentException($"{path} not found!");
            string dupInfo = null;
            StringBuilder dupData = new StringBuilder();
            uint lastRangeEnd = 0;
            string unknownCode = "--";
            CountryNames.Add(unknownCode, "Unknown");
            int count = 0;
            try
            {
                foreach (var line in File.ReadAllLines(path))
                {
                    if (line.StartsWith("#")) continue;
                    try
                    {
                        //"87597056","87599103","ripencc","1338422400","ES","ESP","Spain"
                        var p = line.Split(',').Select(o => o.Trim('"')).ToArray();
                        var st = uint.Parse(p[0]);
                        var ed = uint.Parse(p[1]);
                        var cn = p[4];

                        //range gap found
                        if (lastRangeEnd > 0 && st > lastRangeEnd)
                        {
                            //padding unknown range
                            IP2CN.Add(lastRangeEnd, unknownCode);
                            IP2CN.Add(st - 1, unknownCode);
                            count += 2;
                        }

                        dupInfo = $"{st}-{ed}-{cn}";
                        IP2CN.Add(st, cn);
                        IP2CN.Add(ed, cn);
                        lastRangeEnd = ed + 1;
                        if (!CountryNames.ContainsKey(cn))
                            CountryNames.Add(cn, p[6]);
                    }
                    catch (ArgumentException aex)
                    {
                        dupData.AppendLine($"Duplicated {dupInfo}: {aex.Message}");
                    }
                }
                IPRanges = IP2CN.Select(o => o.Key).OrderBy(o => o).ToArray();
            }
            catch (Exception ex)
            {
                throw new ApplicationException($"CSV parsing error: {ex.Message}");
            }
            DupDataInfo = dupData.ToString();
        }
        public uint GetIPAddrDec(string ipAddr)
        {
            byte[] b = IPAddress.Parse(ipAddr).GetAddressBytes();
            Array.Reverse(b);
            return BitConverter.ToUInt32(b, 0);
        }

        public string GetCountryCode(string ipAddr)
        {
            uint ip = GetIPAddrDec(ipAddr);
            int idx = Array.BinarySearch(IPRanges, ip);
            if (idx < 0)
            {
                int idxNearest = ~idx;
                if (idxNearest > 0) idxNearest--;
                idx = idxNearest;
            }
            return IP2CN[IPRanges[idx]];
        }

        public string ConvertCountryCodeToName(string cnCode)
        {
            if (CountryNames.ContainsKey(cnCode))
                return CountryNames[cnCode];
            return cnCode;
        }

        public string GetCountryName(string ipAddr)
        {
            return ConvertCountryCodeToName(GetCountryCode(ipAddr));
        }
    }
}
