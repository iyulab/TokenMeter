using Iyu.Conventions.Testing;
using Xunit;

namespace TokenMeter.Tests;

/// <summary>
/// The public surface follows the two API rules of the ecosystem: every public async method takes a
/// <see cref="CancellationToken"/>, and failure is reported by an exception rather than by a returned object carrying a
/// success flag and an error. The scans are <c>Iyu.Conventions.Testing</c>'s, over the same assemblies as the
/// operational-language scan.
/// </summary>
/// <remarks>
/// The rosters are the methods that break a rule today. Shrink them; never grow them silently. A change to a listed
/// method's parameters changes its entry, which is a roster change on purpose.
/// </remarks>
public class PublicApiConventionTests
{
    private static readonly string[] KnownUncancellable = [];

    private static readonly string[] KnownResultReturns = [];

    [Fact]
    public void PublicAsyncMethods_TakeACancellationToken() =>
        AsyncCancellation.Scan(OptionsReachabilityRosterTests.Libraries).ShouldMatchRoster(KnownUncancellable);

    [Fact]
    public void PublicMethods_DoNotReturnResultObjects() =>
        ResultReturns.Scan(OptionsReachabilityRosterTests.Libraries).ShouldMatchRoster(KnownResultReturns);

    // Positive control: an empty roster would also pass if the scan saw no public method at all.
    [Fact]
    public void Scan_SeesThePublicSurface() =>
        Assert.True(ResultReturns.Scan(OptionsReachabilityRosterTests.Libraries).MembersRead > 0, "the scan read too few public methods");
}
