using System.Collections.Generic;
using System.Linq;

namespace InvariantAgent.Core.Model.ToolData
{
    public class ListData : ToolData
    {
        public List<string> Rows { get; set; } = new();

        public override string ToString() => string.Join(" | ", Rows);
    }
}
