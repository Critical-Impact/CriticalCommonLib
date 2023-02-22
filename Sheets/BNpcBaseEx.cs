using Lumina.Excel.GeneratedSheets;

namespace CriticalCommonLib.Sheets;

public class BNpcBaseEx : BNpcBase
{
    private NpcType? _npcType;
    public NpcType NpcType
    {
        get
        {
            if (_npcType == null)
            {
                var modelChara = ModelChara.Value;
                if (modelChara != null)
                {
                    switch (modelChara.Type)
                    {
                        case 0:
                            _npcType = NpcType.Misc;
                            break;
                        case 1:
                            _npcType = NpcType.Humanoid;
                            break;
                        case 2:
                            _npcType = NpcType.Monster;
                            break;
                        case 3:
                            _npcType = NpcType.Monster;
                            break;
                        case 4:
                            _npcType = NpcType.Nest;
                            break;
                    }
                }
                _npcType ??= NpcType.Unknown;
            }

            return _npcType.Value;
        }
    }
}

public enum NpcType
{
    Unknown,
    Humanoid,
    Nest,
    Misc,
    Monster
}