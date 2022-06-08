using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.String;
using Newtonsoft.Json;

namespace CriticalCommonLib
{
    public static class Utils
    {
        private static Dictionary<string, ushort>? _serverOpcodes;
        private static HashSet<string> _failedOpCodes = new HashSet<string>();
        private static bool _loadingOpcodes = false;
        public static ushort? GetOpcode(string opcodeName)
        {
            if (Service.Data.ServerOpCodes.ContainsKey(opcodeName))
            {
                return Service.Data.ServerOpCodes[opcodeName];
            }
            if (_serverOpcodes != null)
            {
                if (_serverOpcodes.ContainsKey(opcodeName))
                {
                    return _serverOpcodes[opcodeName];
                }

                if (!_failedOpCodes.Contains(opcodeName))
                {
                    _failedOpCodes.Add(opcodeName);
                    PluginLog.Error("Could not find opcode for " + opcodeName);
                }

                return null;
            }

            if (!_loadingOpcodes)
            {
                _loadingOpcodes = true;
                var client = new HttpClient();
                client.GetStringAsync(
                        "https://raw.githubusercontent.com/karashiiro/FFXIVOpcodes/master/opcodes.min.json")
                    .ContinueWith(ExtractOpCode);
            }

            return null;
        }
        
        public static unsafe SeString ReadSeString(Utf8String xivString) {
            var len = (int) (xivString.BufUsed > int.MaxValue ? int.MaxValue : xivString.BufUsed);
            var bytes = new byte[len];
            Marshal.Copy(new IntPtr(xivString.StringPtr), bytes, 0, len);
            return SeString.Parse(bytes);
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

                if (!region.Lists.TryGetValue("ServerZoneIpcType", out List<OpcodeList>? serverZoneIpcTypes))
                {
                    PluginLog.Warning("No ServerZoneIpcType in opcode list");
                    return;
                }
                
                var client = new HttpClient();
                var result = client.GetStringAsync(
                    "https://raw.githubusercontent.com/Critical-Impact/OpCodeStatus/main/status.json").Result;
                var opCodesValid = JsonConvert.DeserializeObject<OpcodeStatus>(result);
                if (opCodesValid == null)
                {
                    PluginLog.Error("Could not parse the current state of opcodes.");
                    return;
                }

                if (opCodesValid.valid == false)
                {
                    PluginLog.Warning("Opcodes are currently flagged as invalid. This is a safety check and they should be re-enabled once they have been parsed correctly.");
                    _serverOpcodes = new Dictionary<string, ushort>();
                    _loadingOpcodes = false;
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
        public static ByteColor ColorFromVector4(Vector4 hexString)
        {
            return new () {R = (byte) (hexString.X * 0xFF), B = (byte) (hexString.Z * 0xFF), G = (byte) (hexString.Y * 0xFF), A = (byte) (hexString.W * 0xFF)};
        }
    }
#pragma warning disable 8618

    public class OpcodeRegion
    {
        public string Version { get; set; } = null!;
        public string Region { get; set; }
        public Dictionary<string, List<OpcodeList>>? Lists { get; set; }
    }
    
    public class OpcodeStatus
    {
        public bool valid { get; set; } = false;
    }

    public class OpcodeList
    {
        public string Name { get; set; } = null!;
        public ushort Opcode { get; set; }
    }
#pragma warning restore 8618
}