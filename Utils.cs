using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using Newtonsoft.Json;

namespace CriticalCommonLib
{
    public static class Utils
    {
        private static Dictionary<string, ushort> _serverOpcodes;
        private static bool _loadingOpcodes = false;
        public static ushort GetOpcode(string opcodeName)
        {
            if (_serverOpcodes != null)
            {
                if (_serverOpcodes.ContainsKey(opcodeName))
                {
                    return _serverOpcodes[opcodeName];
                }
                else
                {
                    PluginLog.Log("Could not find opcode for " + opcodeName);
                    return (ushort) 0;
                }
            }

            if (!_loadingOpcodes)
            {
                _loadingOpcodes = true;
                var client = new HttpClient();
                client.GetStringAsync(
                        "https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.min.json")
                    .ContinueWith(ExtractOpCode);
            }

            return 0;
        }
        
        private static void ExtractOpCode(Task<string> task)
        {
            try
            {
                var regions = JsonConvert.DeserializeObject<List<OpcodeRegion>>(task.Result);
                if (regions == null)
                {
                    PluginLog.Warning("No regions found in opcode list");
                    return;
                }

                var region = regions.Find(r => r.Region == "Global");
                if (region == null || region.Lists == null)
                {
                    PluginLog.Warning("No global region found in opcode list");
                    return;
                }

                if (!region.Lists.TryGetValue("ServerZoneIpcType", out List<OpcodeList> serverZoneIpcTypes))
                {
                    PluginLog.Warning("No ServerZoneIpcType in opcode list");
                    return;
                }

                var newOpCodes = new Dictionary<string, ushort>();
                foreach (var opcode in serverZoneIpcTypes)
                {
                    newOpCodes[opcode.Name] = opcode.Opcode;
                }

                _serverOpcodes = newOpCodes;
                _loadingOpcodes = false;
            }
            catch (Exception e)
            {
                PluginLog.Error(e, "Could not download/extract opcodes: {}", e.Message);
                _loadingOpcodes = false;
            }
        }
        public static ByteColor ColorFromHex(string hexString, int alpha)
        {
            if (hexString.IndexOf('#') != -1)
                hexString = hexString.Replace("#", "");

            var r = int.Parse(hexString.Substring(0, 2), NumberStyles.AllowHexSpecifier);
            var g = int.Parse(hexString.Substring(2, 2), NumberStyles.AllowHexSpecifier);
            var b = int.Parse(hexString.Substring(4, 2), NumberStyles.AllowHexSpecifier);

            return new ByteColor() {R = (byte) r, B = (byte) b, G = (byte) g, A = (byte) alpha};
        }
    }
    
    public class OpcodeRegion
    {
        public string Version { get; set; }
        public string Region { get; set; }
        public Dictionary<string, List<OpcodeList>> Lists { get; set; }
    }

    public class OpcodeList
    {
        public string Name { get; set; }
        public ushort Opcode { get; set; }
    }
}