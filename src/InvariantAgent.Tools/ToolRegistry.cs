using InvariantAgent.Core.Abstractions;

namespace InvariantAgent.Tools;

public class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ITool> _tools;

    public ToolRegistry(IEnumerable<ITool> tools)
    {
        _tools = tools.ToDictionary(t => t.Name);
    }

    public ITool Get(string name)
    {
        if (_tools.TryGetValue(name, out var tool))
            return tool;

        throw new InvalidOperationException($"Tool not registered: {name}");
    }
}