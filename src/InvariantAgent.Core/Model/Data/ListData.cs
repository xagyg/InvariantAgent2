using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Data
{
    public class ListData : CapabilityData
    {
        public List<string> Rows { get; set; } = new();

        public override string ToString() => string.Join(" | ", Rows);
    }
}
