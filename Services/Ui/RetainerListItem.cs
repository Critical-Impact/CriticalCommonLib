using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class RetainerListItem
    {
        public AtkTextNode* textNode;
        public ByteColor originalColor;
        public string originalText;
        public float RelativeX;
        public float RelativeY;
        public string RetainerName;

        public RetainerListItem(AtkTextNode* textNode, float relativeX, float relativeY, string retainerName)
        {
            RelativeX = relativeX;
            RelativeY = relativeY;
            RetainerName = retainerName;
            this.textNode = textNode;
            this.originalColor = textNode->TextColor;
            this.originalText = retainerName;
        }
    }
}