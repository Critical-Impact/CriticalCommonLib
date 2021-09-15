### Common Library for Dalamud Plugins

This is a small library for functions that I am going to potentially use across multiple dalamud plugins. Feel free to fork/use for yourself.

#### Models
1. InventoryCategory.cs - Less specific way of representing where an item is
2. InventoryItem.cs - Copy of the in memory item that adds several helper functions and tracks where the item is
3. InventorySortOrder.cs - An object that represents all the inventories at a single point in time
4. MemoryInventoryContainer.cs - The in memory representation of inventory containers
5. MemoryInventoryItem.cs - The in memory representation of an inventory item
6. Character.cs - An object that represents a given character(player or retainer)
7. NetworkRetainerInformation.cs - A struct that represents information received when the retainer list opens

### Services
1. CharacterMonitor.cs - Keeps track of the currently logged in character and active retainer and provides events when they change
2. ExcelCache.cs - Caches data that comes from the game sheets
3. GameInterface.cs - Provides a way to pull specific parts of game data out of memory
4. GameUi.cs - Manipulates the in game UI and can track when UI elements show/hide
5. InventoryMonitor.cs - Accesses the inventory in memory, sorts it and then returns the items with location data
6. NetworkDecoder.cs - Currently decodes retainer information
7. OdrScanner.cs - Decodes the sort order file
