
namespace InvariantAgent.Core.Model
{
    public class ControlDecision
    {
        public bool Allowed { get; set; }
        public ViolationType ViolationType { get; set; }
        public string Reason { get; set; }
    }
}
