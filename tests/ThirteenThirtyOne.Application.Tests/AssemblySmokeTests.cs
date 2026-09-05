using System.Reflection;
using Xunit;

namespace ThirteenThirtyOne.Application.Tests;

public sealed class AssemblySmokeTests
{
    [Fact]
    public void BoundaryAssemblyCanBeLoaded()
    {
        var assembly = Assembly.Load("ThirteenThirtyOne.Application");

        Assert.Equal("ThirteenThirtyOne.Application", assembly.GetName().Name);
        Assert.Contains(assembly.GetTypes(), type => type.Name == "AssemblyMarker");
    }
}
