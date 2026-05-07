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

        if (tool == null)
        {
            return new AgentOutcome
            {                
                Tool = action.Tool,
                Input = action.Input,
                Result = ToolResult.Fail(action.Tool, "Unkown tool"),
                StateVersion = state.Version
            };           
        }

        ToolResult result;

        try
        {
            var raw = tool.Run(action.Input, state);

            result = raw as ToolResult ?? ToolResult.Ok(action.Tool, raw);
        }
        catch (Exception ex)
        {
            result = ToolResult.Fail(action.Tool, ex.Message);
        }

        return new AgentOutcome
        {
            Tool = action.Tool,
            Input = action.Input,
            Result = result,
            StateVersion = state.Version
        };
    }
}