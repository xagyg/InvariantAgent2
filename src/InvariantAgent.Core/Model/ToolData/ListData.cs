using System.Collections.Generic;

namespace InvariantAgent.Core.Model.ToolData
{
    public class ListData : ToolData
    {
        public List<string> Rows { get; set; } = new();
    }
}
