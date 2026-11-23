namespace TokenMeter;

/// <summary>
/// A rung of the capability scale every provider's model lines are placed on. The scale is ordered inside one provider -
/// each rung holds a model more capable (and usually dearer) than the one below it - and says nothing across providers:
/// <c>balanced</c> at one vendor is not "as good as" <c>balanced</c> at another.
/// </summary>
/// <remarks>
/// A provider fills only the rungs it has a line for; a rung without one is empty, and asking for it gives nothing, not the
/// neighbour (see <see cref="TierFallback"/>). The rung names no vendor line or generation, so an alias such as
/// <c>anthropic:balanced</c> keeps working when the vendor ships a newer model - which model it resolves to is the
/// catalog's, and a change of it is announced in the CHANGELOG. Price does not decide a rung: the vendors' own tier
/// names do not follow price either.
/// </remarks>
public enum CapabilityTier
{
    /// <summary>The cheapest, fastest line of the provider.</summary>
    Entry = 1,

    /// <summary>A plain everyday line. Vendors with only four lines leave it empty.</summary>
    Basic = 2,

    /// <summary>The line that balances quality and cost.</summary>
    Balanced = 3,

    /// <summary>A line for work that needs more capability than <see cref="Balanced"/>.</summary>
    Advanced = 4,

    /// <summary>The provider's most capable generally available line.</summary>
    Frontier = 5,
}

/// <summary>The names of the rungs.</summary>
public static class CapabilityTierExtensions
{
    /// <summary>The alias a rung is called by: its name, lower case (<c>balanced</c>).</summary>
    public static string Alias(this CapabilityTier tier) => tier switch
    {
        CapabilityTier.Entry => "entry",
        CapabilityTier.Basic => "basic",
        CapabilityTier.Balanced => "balanced",
        CapabilityTier.Advanced => "advanced",
        CapabilityTier.Frontier => "frontier",
        _ => throw new ArgumentOutOfRangeException(nameof(tier), tier, "Not a rung of the capability scale."),
    };

    /// <summary>
    /// Reads a rung alias (<c>entry</c> ... <c>frontier</c>, any case). A number or a line name is not an alias.
    /// </summary>
    public static bool TryParse(string? text, out CapabilityTier tier)
    {
        foreach (var candidate in Enum.GetValues<CapabilityTier>())
        {
            if (string.Equals(candidate.Alias(), text, StringComparison.OrdinalIgnoreCase))
            {
                tier = candidate;
                return true;
            }
        }

        tier = default;
        return false;
    }
}
