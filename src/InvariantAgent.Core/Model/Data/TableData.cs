using System.Collections.Generic;
using System.Linq;
namespace InvariantAgent.Core.Model.Data;

public class TableData : CapabilityData
{
    public List<string[]> Rows { get; set; } = new();

    public override string ToString()
    => string.Join(" | ", Rows.Select(r => string.Join(",", r)));
}