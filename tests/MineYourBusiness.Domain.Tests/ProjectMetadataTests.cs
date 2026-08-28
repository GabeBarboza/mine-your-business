using Xunit;

namespace MineYourBusiness.Domain.Tests;

public sealed class ProjectMetadataTests
{
    [Fact]
    public void ProductIdentityMatchesApprovedTitleAndTagline()
    {
        Assert.Equal("MINE YOUR BUSINESS", ProjectMetadata.DisplayName);
        Assert.Equal("Just do your job. Probably.", ProjectMetadata.Tagline);
    }

    [Fact]
    public void RulesetVersionIsValidSemanticVersion()
    {
        bool isValid = Version.TryParse(ProjectMetadata.RulesetVersion, out Version? version);

        Assert.True(isValid);
        Assert.NotNull(version);
        Assert.Equal(0, version.Major);
    }
}
