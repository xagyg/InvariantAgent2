using InvariantAgent.Core.Abstractions;
using InvariantAgent.Core.Model;

namespace InvariantAgent.Execution.Engine;

public class ToolExecutor : IExecutor
{
    private readonly IToolRegistry _tools;

    public ToolExecutor(IToolRegistry tools)
    {
        _tools = tools;
    }

    public AgentOutcome Execute(AgentAction action, AgentState state)
    {
        var tool = _tools.Get(action.Tool);

        var result = tool.Run(action.Input, state);

        return new AgentOutcome
        {
            Tool = action.Tool,
            Input = action.Input,
            Result = result,
            StateVersion = state.Version
        };
    }
}