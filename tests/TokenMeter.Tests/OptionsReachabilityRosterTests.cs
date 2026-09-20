using System.Reflection;
using Iyu.Conventions.Testing;
using Xunit;

namespace TokenMeter.Tests;

/// <summary>
/// Every public option in this library is read by the library. An option nothing reads is a promise it does not keep:
/// a caller sets it, and nothing changes and nothing is reported. The roster fails both ways - a new unread option,
/// and a listed one that has since been wired - so each change is recorded on purpose.
/// </summary>
public class OptionsReachabilityRosterTests
{
    // Every assembly this repository ships: an option declared in one and read in another only counts as read
    // when both are scanned.
    private static readonly Assembly[] Libraries =
    [
        Assembly.Load("TokenMeter"),
    ];

    /// <summary>Options accepted as unread today. Shrink this list; never grow it silently.</summary>
    private static readonly Dictionary<string, string[]> KnownUnread = new()
    {
    };

    [Fact]
    public void EveryPublicOption_IsRead() =>
        OptionsReachability.Scan(Libraries, OptionsTypes.NamedWith("Options", "Config"))
            .ShouldMatchRoster(KnownUnread);
}
