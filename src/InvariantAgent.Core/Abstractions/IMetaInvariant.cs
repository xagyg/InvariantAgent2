using InvariantAgent.Core.Model.Control;

namespace InvariantAgent.Core.Abstractions
{
    /// <summary>
    /// A meta-invariant governs <em>how</em> invariant conflicts are resolved (HIG).
    /// </summary>
    /// <remarks>
    /// Meta-invariants are consulted for each candidate violation that might be overridden. If any
    /// meta-invariant rejects, the violation stays blocking. Meta-invariants may also contribute
    /// metadata (justification text, audit flags, review reasons) to the resulting override record.
    /// </remarks>
    public interface IMetaInvariant
    {
        /// <summary>Unique meta-invariant name recorded with override decisions.</summary>
        string Name { get; }

        /// <summary>The governance concern addressed (priority, severity, justification, audit, review).</summary>
        MetaInvariantCategory Category { get; }

        /// <summary>Decides whether the candidate violation in <paramref name="context"/> may be overridden.</summary>
        /// <param name="context">The candidate violation together with all violations and evaluation records.</param>
        /// <returns>Allow (optionally with metadata) or reject with a reason.</returns>
        MetaInvariantResult Evaluate(MetaInvariantContext context);
    }
}
