
namespace InvariantAgent.Core.Model
{
    public class InvariantResult
    {
        public bool IsValid { get; init; }
        public string Reason { get; init; }

        public static InvariantResult Pass() => new() { IsValid = true };
        public static InvariantResult Fail(string reason) => new() { IsValid = false, Reason = reason };
    }
}
