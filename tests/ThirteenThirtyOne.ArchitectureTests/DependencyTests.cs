using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using NetArchTest.Rules;
using Xunit;

namespace ThirteenThirtyOne.ArchitectureTests;

public sealed class DependencyTests
{
    private static readonly IReadOnlyDictionary<string, string[]> AllowedReferences =
        new Dictionary<string, string[]>
        {
            ["Game.Domain"] = [],
            ["Game.Engine"] = ["Game.Domain"],
            ["Application"] = ["Game.Domain", "Game.Engine"],
            ["Infrastructure"] = ["Application", "Game.Domain"],
            ["Protocol"] = [],
            ["GameBackend"] = ["Application", "Infrastructure", "Protocol"],
        };

    public static TheoryData<string> Layers => new(AllowedReferences.Keys);

    [Theory]
    [MemberData(nameof(Layers))]
    public async Task EvaluatedProjectReferencesMatchTheDeclaredGraph(string layer)
    {
        var root = FindRepositoryRoot();
        var name = $"ThirteenThirtyOne.{layer}";
        var project = Path.Combine(root, "src", name, $"{name}.csproj");

        foreach (var configuration in new[] { "Debug", "Release" })
        {
            using var snapshot = await EvaluateProject(project, configuration, root);
            var items = snapshot.RootElement.GetProperty("Items");
            var actual = items.GetProperty("ProjectReference").EnumerateArray()
                .Select(item => Path.GetFullPath(item.GetProperty("FullPath").GetString()!))
                .Order(StringComparer.Ordinal).ToArray();
            var expected = AllowedReferences[layer]
                .Select(dependency => Path.Combine(root, "src", $"ThirteenThirtyOne.{dependency}",
                    $"ThirteenThirtyOne.{dependency}.csproj"))
                .Order(StringComparer.Ordinal).ToArray();

            Assert.Equal(expected, actual);

            if (layer is "Game.Domain" or "Game.Engine" or "Application" or "Protocol")
            {
                // These boundaries currently need only the BCL and the declared project graph.
                // Introducing any package requires an explicit architecture review/test update.
                Assert.Empty(items.GetProperty("PackageReference").EnumerateArray());
                Assert.All(items.GetProperty("FrameworkReference").EnumerateArray(),
                    item => Assert.Equal("Microsoft.NETCore.App", item.GetProperty("Identity").GetString()));
                Assert.Empty(items.GetProperty("Reference").EnumerateArray());
            }
        }
    }

    [Theory]
    [MemberData(nameof(Layers))]
    public void CompiledTypesDoNotDependOnForbiddenLayers(string layer)
    {
        var assembly = Assembly.Load($"ThirteenThirtyOne.{layer}");
        var forbidden = AllowedReferences.Keys
            .Except(AllowedReferences[layer].Append(layer))
            .Select(name => $"ThirteenThirtyOne.{name}").ToArray();

        Assert.NotEmpty(assembly.GetTypes());
        var result = Types.InAssembly(assembly).ShouldNot()
            .HaveDependencyOnAny(forbidden).GetResult();

        Assert.True(result.IsSuccessful, $"{layer} contains a dependency on a forbidden layer.");
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(),
            reference => forbidden.Contains(reference.Name, StringComparer.Ordinal));
    }

    [Theory]
    [InlineData("Game.Domain")]
    [InlineData("Game.Engine")]
    [InlineData("Application")]
    [InlineData("Protocol")]
    public void InnerBoundariesRemainIndependentOfInfrastructure(string layer)
    {
        string[] forbidden =
        [
            "Microsoft.AspNetCore", "Microsoft.EntityFrameworkCore", "Microsoft.Extensions",
            "Npgsql", "StackExchange.Redis", "Amazon", "AWSSDK", "System.Net",
        ];
        var assembly = Assembly.Load($"ThirteenThirtyOne.{layer}");
        var result = Types.InAssembly(assembly).ShouldNot()
            .HaveDependencyOnAny(forbidden).GetResult();

        Assert.True(result.IsSuccessful, $"{layer} contains an infrastructure dependency.");
        Assert.DoesNotContain(assembly.GetReferencedAssemblies(),
            reference => forbidden.Any(prefix => reference.Name!.StartsWith(prefix, StringComparison.Ordinal)));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ThirteenThirtyOne.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Run architecture tests from a built repository checkout.");
    }

    private static async Task<JsonDocument> EvaluateProject(string project, string configuration, string root)
    {
        var startInfo = new ProcessStartInfo("dotnet")
        {
            WorkingDirectory = root,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        foreach (var argument in new[]
        {
            "msbuild", project, "-nologo", $"-property:Configuration={configuration}",
            "-getItem:ProjectReference,PackageReference,FrameworkReference,Reference",
        })
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException("Could not start SDK project evaluation.");
        var output = process.StandardOutput.ReadToEndAsync();
        var error = process.StandardError.ReadToEndAsync();
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(60));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        Assert.True(process.ExitCode == 0, $"MSBuild evaluation failed: {await error}\n{await output}");
        return JsonDocument.Parse(await output);
    }
}
