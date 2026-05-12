namespace InvariantAgent.Core.Model.SelfModification
{
    public sealed class SelfModificationRequest
    {
        public string Target { get; init; } = "";

        public string Operation { get; init; } = "";

        public string Key { get; init; } = "";

        public string Value { get; init; } = "";
    }
}