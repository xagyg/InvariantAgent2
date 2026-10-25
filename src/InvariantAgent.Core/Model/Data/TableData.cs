using System.Collections.Generic;
using System.Linq;
namespace InvariantAgent.Core.Model.Data;

/// <summary>A tabular payload; cells are comma-joined and rows separated by " | ".</summary>
public class TableData : CapabilityData
{
    /// <summary>Table rows, each an array of cells.</summary>
    public List<string[]> Rows { get; set; } = new();

    public override string ToString()
    => string.Join(" | ", Rows.Select(r => string.Join(",", r)));
}