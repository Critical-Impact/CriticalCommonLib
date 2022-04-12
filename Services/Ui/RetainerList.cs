using System.Collections.Generic;

namespace CriticalCommonLib.Services.Ui
{
    public unsafe class RetainerList : GameWindow
    {
        public List<RetainerListItem> _sortedItems;
            
        public RetainerList(List<RetainerListItem> sortedItems)
        {
            _sortedItems = sortedItems;
        }
            
        public void ClearColors()
        {
            foreach (var item in _sortedItems)
            {
                item.textNode->TextColor = item.originalColor;
                item.textNode->SetText(item.originalText);
            }
        }

        public void SetColor(string retainerName, string hexCode)
        {
            if (_sortedItems.Exists(c => c.RetainerName == retainerName))
            {
                var textNode = _sortedItems.Find(c => c.RetainerName == retainerName);
                if (textNode != null)
                {
                    textNode.textNode->TextColor = Utils.ColorFromHex(hexCode, 255);
                }
            }
        }

        public void SetText(string retainerName, string newText)
        {
            if (_sortedItems.Exists(c => c.RetainerName == retainerName))
            {
                var textNode = _sortedItems.Find(c => c.RetainerName == retainerName);
                if (textNode != null)
                {
                    textNode.textNode->SetText(newText);
                }
            }
        }

        public void SetTextAndColor(string retainerName, string newText, string hexCode)
        {
            if (_sortedItems.Exists(c => c.RetainerName == retainerName))
            {
                var textNode = _sortedItems.Find(c => c.RetainerName == retainerName);
                if (textNode != null)
                {
                    textNode.textNode->SetText(newText);
                    textNode.textNode->TextColor = Utils.ColorFromHex(hexCode, 255);
                }
            }
        }
    }
}