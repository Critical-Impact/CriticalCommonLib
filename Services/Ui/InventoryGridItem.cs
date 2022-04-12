using System.Numerics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class InventoryGridItem
    {
        public AtkResNode* _resNode;
        private const int ImageId = 3; 

        public InventoryGridItem(AtkResNode* resNode)
        {
            _resNode = resNode;
        }

        public bool IsEmpty
        {
            get
            {
                var component = (AtkComponentNode*) _resNode;
                var componentInfo = component->Component->UldManager;
                for (var j = 0; j < componentInfo.NodeListCount; j++)
                {
                    if (componentInfo.NodeList[j]->NodeID == ImageId)
                    {
                        var imageNode = (AtkImageNode*) componentInfo.NodeList[j];
                        return imageNode->Flags == 0;
                    }
                }

                return false;
            }
        }
            
        public void SetColor(Vector4 color)
        {
            _resNode->AddBlue = (ushort) (color.Z * 255.0f);
            _resNode->AddRed = (ushort) (color.X * 255.0f);
            _resNode->AddGreen = (ushort) (color.Y * 255.0f);
            _resNode->Color.A = (byte) (color.W * 255.0f);
        }
            
        public void ClearColor()
        {
            _resNode->AddBlue = 0;
            _resNode->AddRed = 0;
            _resNode->AddGreen = 0;
            _resNode->Color.A = 255;
        }
    }
}