
namespace InvariantAgent.Core.Abstractions;

public interface IToolRegistry
{
    ITool Get(string toolName);
}