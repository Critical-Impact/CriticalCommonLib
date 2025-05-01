
using System;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using System.Runtime.InteropServices;
using Lumina.Excel.Sheets;

namespace CriticalCommonLib.Agents;

[StructLayout(LayoutKind.Explicit, Size = 2880)]
public unsafe struct CSRecipeNote
{
    [FieldOffset(0x118)] public ushort ActiveCraftRecipeId;

    public static CSRecipeNote* Instance()
    {
        return (CSRecipeNote*)RecipeNote.Instance();
    }
}