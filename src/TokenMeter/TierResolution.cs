namespace TokenMeter;

/// <summary>The model a capability alias resolved to, and how it was reached.</summary>
/// <param name="Request">What was asked for (<c>anthropic:balanced</c>, or <c>Anthropic:Balanced</c> built from the arguments).</param>
/// <param name="Model">The catalog model: its prices, limits and lifecycle.</param>
/// <param name="Tier">The rung the model sits on. Differs from the rung asked for when <paramref name="Substituted"/>.</param>
/// <param name="Substituted">The rung asked for was empty and a fallback rung answered.</param>
/// <param name="Id">The id to send to the provider: the account's own id when an available list was given, else the model's id.</param>
public sealed record TierResolution(string Request, ModelInfo Model, CapabilityTier Tier, bool Substituted, string Id);
