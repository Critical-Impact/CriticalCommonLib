namespace CriticalCommonLib.MarketBoard;

public class UniversalisUserAgent
{
    public string PluginName { get; }
    public string PluginVersion { get;  }

    public UniversalisUserAgent(string pluginName, string pluginVersion)
    {
        PluginName = pluginName;
        PluginVersion = pluginVersion;
    }
}