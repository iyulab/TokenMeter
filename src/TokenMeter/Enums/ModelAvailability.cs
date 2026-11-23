namespace TokenMeter;

/// <summary>Who can call a model — see <see cref="ModelInfo.Availability"/>.</summary>
public enum ModelAvailability
{
    /// <summary>Generally available: any account with the provider can call it.</summary>
    General,

    /// <summary>Offered as a preview: callable, but the vendor may change or withdraw it on short notice.</summary>
    Preview,

    /// <summary>Callable only by accounts the vendor has cleared (a verified-organization model).</summary>
    Restricted,
}
