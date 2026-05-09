using System.Collections.Generic;
namespace InvariantAgent.Core.Model.ToolData;

public class TableData : ToolData
{
    public List<string[]> Rows { get; set; } = new();
}