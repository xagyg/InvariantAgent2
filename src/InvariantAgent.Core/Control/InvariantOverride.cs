using System;
using System.Collections.Generic;

namespace InvariantAgent.Core.Model.Control
{
    public sealed class InvariantOverride
    {
        public InvariantViolation OverriddenViolation { get; init; } = new();

        public IReadOnlyList<string> PreservedHigherPriorityInvariants { get; init; }
            = Array.Empty<string>();

        public string Justification { get; init; } = "";

        public string MetaInvariantCategory { get; init; } = "Priority";

        public IReadOnlyList<MetaInvariantCategory> MetaInvariantCategories { get; init; }
            = Array.Empty<MetaInvariantCategory>();

        public InvariantLayer OverriddenLayer => OverriddenViolation.Layer;
    }
}
