
namespace InvariantAgent.Core.Model.Control
{
    /// <summary>Severity of an invariant violation or drift measurement. Error and Critical are blocking by default.</summary>
    public enum InvariantSeverity
    {
        /// <summary>Informational; overrideable.</summary>
        Info,
        /// <summary>Soft violation; overrideable under HIG.</summary>
        Warning,
        /// <summary>Hard violation; blocking.</summary>
        Error,
        /// <summary>Severe violation; blocking.</summary>
        Critical
    }
}
