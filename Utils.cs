using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using CriticalCommonLib.Helpers;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Logging;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.String;
using ImGuiNET;
using Lumina.Excel;
using Lumina.Excel.GeneratedSheets;
using Newtonsoft.Json;

namespace CriticalCommonLib
{
    public static class Utils
    {
        private static Dictionary<string, ushort>? _serverOpcodes;
        private static Dictionary<string, ushort>? _clientOpCodes;
        private static HashSet<string> _failedOpCodes = new HashSet<string>();
        private static bool _loadingOpcodes = false;
        public static Vector4 ConvertUIColorToColor(UIColor uiColor)
        {
            var temp = BitConverter.GetBytes(uiColor.UIForeground);
            return new Vector4((float) temp[3] / 255,
                (float) temp[2] / 255,
                (float) temp[1] / 255,
                (float) temp[0] / 255);
        }

        public static string GenerateRandomId()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var stringChars = new char[8];
            var random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                stringChars[i] = chars[random.Next(chars.Length)];
            }

            var finalString = new String(stringChars);
            return finalString;
        }
        
        public static unsafe SeString ReadSeString(Utf8String xivString) {
            var len = (int) (xivString.BufUsed > int.MaxValue ? int.MaxValue : xivString.BufUsed);
            var bytes = new byte[len];
            Marshal.Copy(new IntPtr(xivString.StringPtr), bytes, 0, len);
            return SeString.Parse(bytes);
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
        
        private static ulong beginModule = 0;
        private static ulong endModule = 0;
        public static void ClickToCopyText(string text, string? textCopy = null) {
            textCopy ??= text;
            ImGui.Text($"{text}");
            if (ImGui.IsItemHovered()) {
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
                if (textCopy != text) ImGui.SetTooltip(textCopy);
            }
            if (ImGui.IsItemClicked()) ImGui.SetClipboardText($"{textCopy}");
        }
        
        public static unsafe void PrintOutObject(object obj, ulong addr, List<string> path, bool autoExpand = false, string? headerText = null) {
            if (obj is Utf8String utf8String) {

                var text = string.Empty;
                Exception err = null!;
                try {
                    var s = utf8String.BufUsed > int.MaxValue ? int.MaxValue : (int) utf8String.BufUsed;
                    if (s > 1) {
                        text = Encoding.UTF8.GetString(utf8String.StringPtr, s - 1);
                    }
                } catch (Exception ex) {
                    err = ex;
                }


                if (err != null) {
                    ImGui.TextDisabled(err.Message);
                    ImGui.SameLine();
                } else {
                    ImGui.Text($"\"{text}\"");
                    ImGui.SameLine();
                }

            }

            var pushedColor = 0;
            var openedNode = false;
            try {
                if (endModule == 0 && beginModule == 0) {
                    try {
                        beginModule = (ulong) Process.GetCurrentProcess().MainModule!.BaseAddress.ToInt64();
                        endModule = (beginModule + (ulong)Process.GetCurrentProcess().MainModule!.ModuleMemorySize);
                    } catch {
                        endModule = 1;
                    }
                }

                ImGui.PushStyleColor(ImGuiCol.Text, 0xFF00FFFF);
                pushedColor++;
                if (autoExpand) {
                    ImGui.SetNextItemOpen(true, ImGuiCond.Appearing);
                }

                headerText ??= $"{obj}";

                if (ImGui.TreeNode($"{headerText}##print-obj-{addr:X}-{string.Join("-", path)}")) {
                    openedNode = true;
                    ImGui.PopStyleColor();
                    pushedColor--;
                    foreach (var f in obj.GetType().GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance)) {

                        var fixedBuffer = (FixedBufferAttribute) f.GetCustomAttribute(typeof(FixedBufferAttribute))!;
                        if (fixedBuffer != null) {
                            ImGui.Text($"fixed");
                            ImGui.SameLine();
                            ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{fixedBuffer.ElementType.Name}[0x{fixedBuffer.Length:X}]");
                        } else {

                            if (f.FieldType.IsArray) {
                                var arr = (Array) f.GetValue(obj)!;
                                ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{f.FieldType.GetElementType()?.Name ?? f.FieldType.Name}[{arr.Length}]");
                            } else {
                                ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{f.FieldType.Name}");
                            }
                        }

                        ImGui.SameLine();

                        ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.4f, 1), $"{f.Name}: ");
                        ImGui.SameLine();

                        PrintOutValue(addr, new List<string>(path) { f.Name }, f.FieldType, f.GetValue(obj)!, f);
                    }

                    foreach (var p in obj.GetType().GetProperties()) {
                        if (p.PropertyType.IsGenericType) {
                            var gTypeName = string.Join(',', p.PropertyType.GetGenericArguments().Select(gt => gt.Name));
                            var baseName = p.PropertyType.Name.Split('`')[0];
                            ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{baseName}<{gTypeName}>");
                        } else {
                            ImGui.TextColored(new Vector4(0.2f, 0.9f, 0.9f, 1), $"{p.PropertyType.Name}");
                        }

                        ImGui.SameLine();
                        ImGui.TextColored(new Vector4(0.2f, 0.6f, 0.4f, 1), $"{p.Name}: ");
                        ImGui.SameLine();

                        PrintOutValue(addr, new List<string>(path) { p.Name }, p.PropertyType, p.GetValue(obj)!, p);
                    }

                    openedNode = false;
                    ImGui.TreePop();
                } else {
                    ImGui.PopStyleColor();
                    pushedColor--;
                }
            } catch (Exception ex) {
                ImGui.Text($"{{{ ex }}}");
            }

            if (openedNode) ImGui.TreePop();
            if (pushedColor > 0) ImGui.PopStyleColor(pushedColor);

        }
        
        private static unsafe void PrintOutValue(ulong addr, List<string> path, Type type, object value, MemberInfo member) {
            try {
                var valueParser = member.GetCustomAttribute(typeof(ValueParser));
                var fieldOffset = member.GetCustomAttribute(typeof(FieldOffsetAttribute));
                if (valueParser is ValueParser vp) {
                    vp.ImGuiPrint(type, value, member, addr);
                    return;
                }

                if (type.IsPointer) {
                    var val = (Pointer) value;
                    var unboxed = Pointer.Unbox(val);
                    if (unboxed != null) {
                        var unboxedAddr = (ulong) unboxed;
                        ClickToCopyText($"{(ulong) unboxed:X}");
                        if (beginModule > 0 && unboxedAddr >= beginModule && unboxedAddr <= endModule) {
                            ImGui.SameLine();
                            ImGui.PushStyleColor(ImGuiCol.Text, 0xffcbc0ff);
                            ClickToCopyText($"ffxiv_dx11.exe+{(unboxedAddr - beginModule):X}");
                            ImGui.PopStyleColor();
                        }

                        try {
                            var eType = type.GetElementType();
                            var ptrObj = Marshal.PtrToStructure(new IntPtr(unboxed), eType!);
                            ImGui.SameLine();
                            PrintOutObject(ptrObj!, (ulong) unboxed, new List<string>(path));
                        } catch {
                            // Ignored
                        }
                    } else {
                        ImGui.Text("null");
                    }
                } else {

                    if (type.IsArray) {

                        var arr = (Array) value;
                        if (ImGui.TreeNode($"Values##{member.Name}-{addr}-{string.Join("-", path)}")) {
                            for (var i = 0; i < arr.Length; i++) {
                                ImGui.Text($"[{i}]");
                                ImGui.SameLine();
                                PrintOutValue(addr, new List<string>(path) { $"_arrValue_{i}" }, type.GetElementType()!, arr.GetValue(i)!, member);
                            }
                            ImGui.TreePop();
                        }


                    } else if (!type.IsPrimitive) {
                        switch (value) {
                            case ILazyRow ilr:
                                var p = ilr.GetType().GetProperty("Value", BindingFlags.Public | BindingFlags.Instance);
                                if (p != null) {
                                    var getter = p.GetGetMethod();
                                    if (getter != null) {
                                        var rowValue = getter.Invoke(ilr, new object?[] { });
                                        PrintOutObject(rowValue!, addr, new List<string>(path));
                                        break;
                                    }
                                }
                                PrintOutObject(value, addr, new List<string>(path));
                                break;
                            case Lumina.Text.SeString seString:
                                ImGui.Text($"{seString.RawString}");
                                break;
                            default:
                                PrintOutObject(value, addr, new List<string>(path));
                                break;
                        }
                    } else {
                        if (value is IntPtr p) {
                            var pAddr = (ulong)p.ToInt64();
                            ClickToCopyText($"{p:X}");
                            if (beginModule > 0 && pAddr >= beginModule && pAddr <= endModule) {
                                ImGui.SameLine();
                                ImGui.PushStyleColor(ImGuiCol.Text, 0xffcbc0ff);
                                ClickToCopyText($"ffxiv_dx11.exe+{(pAddr - beginModule):X}");
                                ImGui.PopStyleColor();
                            }
                        } else {
                            ImGui.Text($"{value}");
                        }

                    }
                }
            } catch (Exception ex) {
                ImGui.Text($"{{{ex}}}");
            }

        }
        
        public static Vector3 WorldToMap(Vector3 pos, ushort sizeFactor, short offsetX, short offsetY) {
            var scale = sizeFactor / 100f;
            var x = (10 - ((pos.X + offsetX) * scale + 1024f) * -0.2f / scale) / 10f;
            var y = (10 - ((pos.Z + offsetY) * scale + 1024f) * -0.2f / scale) / 10f;
            x = MathF.Round(x, 1, MidpointRounding.ToZero);
            y = MathF.Round(y, 1, MidpointRounding.ToZero);
            return new Vector3(x, y, pos.Z);
        }

        public static string ToTitleCase(string npcNameSingular)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(npcNameSingular.ToLower()); 
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