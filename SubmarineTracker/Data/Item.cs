using Lumina.Excel.GeneratedSheets;

namespace SubmarineTracker.Data;

public struct FakeItem
{
    public string Name;
    public Item Item;

    public FakeItem(string name)
    {
        Name = name;
        Item = new Item();
    }

    public FakeItem(string name, Item item)
    {
        Name = name;
        Item = item;
    }

    public static FakeItem Empty() => new("Add ...");
}
