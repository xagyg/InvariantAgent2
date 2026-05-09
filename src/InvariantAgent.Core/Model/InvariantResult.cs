
using System.Xml.Linq;

namespace InvariantAgent.Core.Model
{
    public class InvariantResult
    {
        public bool IsValid { get; init; }
        public string Reason { get; init; }

        public static InvariantResult Pass(string name) => new() { InvariantName = name, IsValid = true };
        public static InvariantResult Fail(string name, string reason) => new() { InvariantName = name, IsValid = false, Reason = reason };

        public string InvariantName { get; set; }
    }
}
