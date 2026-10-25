using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Data
{
    /// <summary>A list-of-rows payload, rendered as rows separated by " | ".</summary>
    public class ListData : CapabilityData
    {
        /// <summary>The rows.</summary>
        public List<string> Rows { get; set; } = new();

        public override string ToString() => string.Join(" | ", Rows);
    }
}
