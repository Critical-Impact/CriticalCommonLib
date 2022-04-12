using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class RadioButtonUiAddon : UiAtkAddon
    {
        private const uint UnselectedNineGridId = 4;
        private const uint SelectedNineGridId = 3;
            
        public void SetColor(Vector4 color)
        {
            _resNode->AddBlue = (ushort) (color.Z * 255.0f);
            _resNode->AddRed = (ushort) (color.X * 255.0f);
            _resNode->AddGreen = (ushort) (color.Y * 255.0f);
            _resNode->Color.A = (byte) (color.W * 255.0f);
        }
        public void SetColor(int red, int green, int blue)
        {
            _resNode->AddBlue = (ushort) (blue * 255.0f);
            _resNode->AddRed = (ushort) (red * 255.0f);
            _resNode->AddGreen = (ushort) (green * 255.0f);
        }
            
        public void ClearColor()
        {
            _resNode->AddBlue = 0;
            _resNode->AddRed = 0;
            _resNode->AddGreen = 0;
            _resNode->Color.A = 255;
        }
            
        public RadioButtonUiAddon(AtkResNode* resNode)
        {
            _resNode = resNode;
        }

        private AtkResNode* UnselectedNineGrid
        {
            get
            {
                return GetNodeById(UnselectedNineGridId);
            }
        }

        private AtkResNode* SelectedNineGrid
        {
            get
            {
                return GetNodeById(SelectedNineGridId);
            }
        }

        public bool IsSelected
        {
            get
            {
                if (SelectedNineGrid != null && SelectedNineGrid->IsVisible)
                {
                    return true;
                }

                return false;
            }
        }
    }
}