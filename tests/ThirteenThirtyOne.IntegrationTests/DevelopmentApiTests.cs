using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ThirteenThirtyOne.Application.DevelopmentGameplay;
using ThirteenThirtyOne.Infrastructure;
using Xunit;

namespace ThirteenThirtyOne.IntegrationTests;

public sealed class DevelopmentApiTests
{
    private static WebApplicationFactory<Program> Factory(string environment = "Development") =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder => builder.UseEnvironment(environment));

    private static async Task<JsonNode> Create(HttpClient client) => await Post(client, "/dev/games",
        new { gameId = "http-game", players = new[] { "a", "b", "c", "d" }, seedHex = new string('0', 64) }, HttpStatusCode.Created);

    private static async Task<JsonNode> Post(HttpClient client, string path, object? body, HttpStatusCode status = HttpStatusCode.OK)
    {
        using var response = body is null ? await client.PostAsync(path, null) : await client.PostAsJsonAsync(path, body);
        var content = await response.Content.ReadAsStringAsync();
        Assert.True(response.StatusCode == status, $"{path}: {(int)response.StatusCode}, expected {(int)status}: {content}");
        if (status == HttpStatusCode.Created)
        {
            Assert.Equal("/dev/games/http-game", response.Headers.Location!.OriginalString);
        }
        return JsonNode.Parse(content)!;
    }

    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    [InlineData("Testing")]
    public async Task HarnessAndSwaggerAreAbsentOutsideDevelopment(string environment)
    {
        await using var factory = Factory(environment);
        using var client = factory.CreateClient();
        foreach (var path in new[] { "/dev/games", "/dev/games/id", "/swagger/index.html", "/swagger/v1/swagger.json" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync(path)).StatusCode);
        }
        foreach (var path in new[] { "/dev/games", "/dev/games/id/decisions", "/dev/games/id/timeouts", "/dev/games/id/continue" })
        {
            Assert.Equal(HttpStatusCode.NotFound, (await client.PostAsJsonAsync(path, new { })).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/dev/games/id")).StatusCode);
        Assert.Null(factory.Services.GetService<IGameSessionStore>());
    }

    [Fact]
    public async Task SwaggerDocumentsAllOperationsAndStringEnums()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var html = await client.GetStringAsync("/swagger/index.html");
        Assert.Contains("swagger-ui", html, StringComparison.Ordinal);
        var spec = JsonNode.Parse(await client.GetStringAsync("/swagger/v1/swagger.json"))!;
        var paths = spec["paths"]!.AsObject();
        Assert.Equal(6, paths.Sum(path => path.Value!.AsObject().Count));
        Assert.NotNull(paths["/dev/games"]!["post"]!["responses"]!["201"]);
        Assert.NotNull(paths["/dev/games/{gameId}"]!["delete"]!["responses"]!["204"]);
        var schemas = spec["components"]!["schemas"]!;
        Assert.Equal("string", schemas["DevelopmentPlayerAction"]!["type"]!.GetValue<string>());
        Assert.Contains("SelectTarget", schemas["DevelopmentPlayerAction"]!["enum"]!.ToJsonString(), StringComparison.Ordinal);
        Assert.Contains("EffectTarget", schemas["DevelopmentDecisionKind"]!["enum"]!.ToJsonString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateGetStopContinueDeleteAndDuplicate()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var created = await Create(client);
        var fetched = JsonNode.Parse(await client.GetStringAsync("/dev/games/http-game"))!;
        Assert.True(JsonNode.DeepEquals(created["game"], fetched["game"]));
        var duplicate = await Post(client, "/dev/games", new { gameId = "http-game", players = new[] { "a", "b" }, seedHex = new string('0', 64) }, HttpStatusCode.Conflict);
        Assert.Equal("DuplicateGameId", duplicate["rejection"]!.GetValue<string>());
        var pending = created["game"]!["pendingDecision"]!;
        var stopped = await Post(client, "/dev/games/http-game/decisions", new
        {
            decisionId = pending["decisionId"]!.GetValue<long>(),
            playerId = pending["ownerPlayerId"]!.GetValue<string>(),
            action = "Stop",
        });
        Assert.Equal("SafePostResolution", stopped["game"]!["boundary"]!.GetValue<string>());
        Assert.True(stopped["game"]!["isSafeGameplayBoundary"]!.GetValue<bool>());
        Assert.Null(stopped["game"]!["pendingDecision"]);
        var continued = await Post(client, "/dev/games/http-game/continue", null);
        Assert.Equal("WaitPlayerAction", continued["game"]!["boundary"]!.GetValue<string>());
        Assert.NotNull(continued["game"]!["pendingDecision"]);
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync("/dev/games/http-game")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync("/dev/games/http-game")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.DeleteAsync("/dev/games/http-game")).StatusCode);
        Assert.True(JsonNode.DeepEquals(created, await Create(client)));
    }

    [Theory]
    [InlineData("{\"gameId\":\"g\",\"players\":[\"a\",\"b\"],\"seedHex\":\"00\"}")]
    [InlineData("{\"gameId\":\"\",\"players\":null,\"seedHex\":null}")]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{bad json")]
    public async Task MalformedCreationReturns400(string json)
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        using var response = await client.PostAsync("/dev/games", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("decisions", "{\"decisionId\":1,\"playerId\":\"a\",\"action\":\"Unknown\"}")]
    [InlineData("decisions", "{\"decisionId\":1,\"playerId\":\"a\",\"action\":0}")]
    [InlineData("decisions", "{\"decisionId\":1,\"playerId\":\"a\"}")]
    [InlineData("decisions", "{\"decisionId\":0,\"playerId\":\"a\",\"action\":\"Draw\"}")]
    [InlineData("decisions", "{\"decisionId\":1,\"playerId\":\"a\",\"action\":\"SelectTarget\"}")]
    [InlineData("decisions", "{\"decisionId\":1,\"playerId\":\"a\",\"action\":\"Stop\",\"targetPlayerId\":\"b\"}")]
    [InlineData("timeouts", "{\"decisionId\":1,\"decisionKind\":\"Unknown\"}")]
    [InlineData("timeouts", "{\"decisionId\":1,\"decisionKind\":0}")]
    [InlineData("timeouts", "{\"decisionId\":1}")]
    public async Task MalformedDecisionSyntaxReturns400WithoutMutation(string operation, string json)
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var before = await Create(client);
        using var response = await client.PostAsync($"/dev/games/http-game/{operation}", new StringContent(json, Encoding.UTF8, "application/json"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var after = JsonNode.Parse(await client.GetStringAsync("/dev/games/http-game"))!;
        Assert.True(JsonNode.DeepEquals(before["game"], after["game"]));
    }

    [Fact]
    public async Task NormalRejectionsPreserveCurrentState()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var created = await Create(client);
        var pending = created["game"]!["pendingDecision"]!;
        var id = pending["decisionId"]!.GetValue<long>();
        var owner = pending["ownerPlayerId"]!.GetValue<string>();
        var stale = await Post(client, "/dev/games/http-game/decisions", new { decisionId = id + 1, playerId = owner, action = "Draw" }, HttpStatusCode.Conflict);
        Assert.Equal("DecisionMismatch", stale["rejection"]!.GetValue<string>());
        var wrong = await Post(client, "/dev/games/http-game/decisions", new { decisionId = id, playerId = "outsider", action = "Draw" }, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("WrongDecisionOwner", wrong["rejection"]!.GetValue<string>());
        var illegal = await Post(client, "/dev/games/http-game/decisions", new { decisionId = id, playerId = owner, action = "SelectTarget", targetPlayerId = owner }, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("IllegalAction", illegal["rejection"]!.GetValue<string>());
        var continuation = await Post(client, "/dev/games/http-game/continue", null, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("ContinuationNotAllowed", continuation["rejection"]!.GetValue<string>());
        foreach (var result in new[] { stale, wrong, illegal, continuation })
        {
            Assert.False(result["accepted"]!.GetValue<bool>());
            Assert.Empty(result["eventTypes"]!.AsArray());
            Assert.True(JsonNode.DeepEquals(created["game"], result["game"]));
        }
        await Post(client, "/dev/games/missing/continue", null, HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ActionTimeoutStopsOwnerAndReportsTimeoutReason()
    {
        await using var factory = Factory();
        using var client = factory.CreateClient();
        var created = await Create(client);
        var pending = created["game"]!["pendingDecision"]!;
        var expired = await Post(client, "/dev/games/http-game/timeouts", new
        {
            decisionId = pending["decisionId"]!.GetValue<long>(),
            decisionKind = "PlayerAction",
        });
        var player = expired["game"]!["players"]!.AsArray().Single(item =>
            item!["playerId"]!.GetValue<string>() == pending["ownerPlayerId"]!.GetValue<string>())!;
        Assert.Equal("Stopped", player["roundStatus"]!.GetValue<string>());
        Assert.Equal("Timeout", player["terminationReason"]!.GetValue<string>());
        Assert.Equal("SafePostResolution", expired["game"]!["boundary"]!.GetValue<string>());
        var rejected = await Post(client, "/dev/games/http-game/timeouts", new
        {
            decisionId = pending["decisionId"]!.GetValue<long>(),
            decisionKind = "PlayerAction",
        }, HttpStatusCode.UnprocessableEntity);
        Assert.Equal("NoDecisionPending", rejected["rejection"]!.GetValue<string>());
    }

    [Fact]
    public async Task ConcurrentHttpDecisionsHaveOneWinnerAndReturn409ForLoser()
    {
        var store = new RacingStore();
        await using var factory = Factory().WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IGameSessionStore>();
            services.AddSingleton<IGameSessionStore>(store);
        }));
        using var client = factory.CreateClient();
        var created = await Create(client);
        var pending = created["game"]!["pendingDecision"]!;
        store.Race = true;
        var request = new { decisionId = pending["decisionId"]!.GetValue<long>(), playerId = pending["ownerPlayerId"]!.GetValue<string>(), action = "Stop" };
        var responses = await Task.WhenAll(client.PostAsJsonAsync("/dev/games/http-game/decisions", request),
            client.PostAsJsonAsync("/dev/games/http-game/decisions", request));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        var loser = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        var body = JsonNode.Parse(await loser.Content.ReadAsStringAsync())!;
        Assert.Equal("ConcurrencyConflict", body["rejection"]!.GetValue<string>());
        Assert.Equal("SafePostResolution", body["game"]!["boundary"]!.GetValue<string>());
        Assert.Empty(body["eventTypes"]!.AsArray());
        Assert.Equal(2, store.Replacements);
        foreach (var response in responses) { response.Dispose(); }
    }

    [Fact]
    public async Task CompleteHttpGameReplaysExactlyAndNeverLeaksAuthoritativeSecrets()
    {
        await using var firstFactory = Factory();
        await using var secondFactory = Factory();
        using var first = firstFactory.CreateClient();
        using var second = secondFactory.CreateClient();
        var current = await Create(first);
        Assert.True(JsonNode.DeepEquals(current, await Create(second)));
        var targetSelections = 0;
        var targetTimeouts = 0;
        var draws = 0;
        for (var step = 0; step < 10000; step++)
        {
            CheckSecrets(current);
            var game = current["game"]!;
            var boundary = game["boundary"]!.GetValue<string>();
            if (boundary == "GameTerminal")
            {
                Assert.NotNull(game["winnerPlayerId"]);
                Assert.True(draws > 0 && targetSelections > 0 && targetTimeouts > 0);
                var terminal = await Post(first, "/dev/games/http-game/continue", null, HttpStatusCode.Conflict);
                Assert.Equal("GameAlreadyTerminal", terminal["rejection"]!.GetValue<string>());
                Assert.True(JsonNode.DeepEquals(game, terminal["game"]));
                return;
            }
            string path;
            object? body;
            if (boundary == "SafePostResolution")
            {
                path = "/dev/games/http-game/continue";
                body = null;
            }
            else
            {
                var pending = game["pendingDecision"]!;
                var id = pending["decisionId"]!.GetValue<long>();
                var kind = pending["decisionKind"]!.GetValue<string>();
                var owner = pending["ownerPlayerId"]!.GetValue<string>();
                if (kind == "EffectTarget" && targetSelections % 2 == 1)
                {
                    targetSelections++;
                    targetTimeouts++;
                    path = "/dev/games/http-game/timeouts";
                    body = new { decisionId = id, decisionKind = kind };
                }
                else if (kind == "EffectTarget")
                {
                    Assert.NotEmpty(game["resolutionStack"]!.AsArray());
                    var rejected = await Post(first, "/dev/games/http-game/decisions", new
                    { decisionId = id, playerId = owner, action = "SelectTarget", targetPlayerId = "outsider" }, HttpStatusCode.UnprocessableEntity);
                    Assert.Equal("IllegalTarget", rejected["rejection"]!.GetValue<string>());
                    Assert.True(JsonNode.DeepEquals(game, rejected["game"]));
                    targetSelections++;
                    path = "/dev/games/http-game/decisions";
                    body = new { decisionId = id, playerId = owner, action = "SelectTarget", targetPlayerId = pending["allowedTargets"]![0]!.GetValue<string>() };
                }
                else
                {
                    var score = game["players"]!.AsArray().Single(player => player!["playerId"]!.GetValue<string>() == owner)!["currentScore"]!.GetValue<int>();
                    var action = score >= 20 ? "Stop" : "Draw";
                    draws += action == "Draw" ? 1 : 0;
                    path = "/dev/games/http-game/decisions";
                    body = new { decisionId = id, playerId = owner, action };
                }
            }
            current = await Post(first, path, body);
            var replay = await Post(second, path, body);
            Assert.True(JsonNode.DeepEquals(current, replay), $"Replay differed at step {step}.");
        }
        Assert.Fail("Game did not terminate within the deterministic trace bound.");
    }

    private static void CheckSecrets(JsonNode node)
    {
        if (node is JsonObject obj)
        {
            foreach (var (name, value) in obj)
            {
                Assert.DoesNotContain(name.ToLowerInvariant(), new[] { "seed", "seedhex", "randomstate", "gameplaystate", "drawpile", "sourcecard", "rawwords" });
                if (value is not null) { CheckSecrets(value); }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var value in array) { if (value is not null) { CheckSecrets(value); } }
        }
    }

    private sealed class RacingStore : IGameSessionStore
    {
        private readonly InMemoryGameSessionStore inner = new();
        private readonly TaskCompletionSource gate = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int reads;
        private int replacements;
        public bool Race { get; set; }
        public int Replacements => replacements;
        public async ValueTask<StoredGameSession?> GetAsync(string gameId, CancellationToken cancellationToken)
        {
            var current = await inner.GetAsync(gameId, cancellationToken);
            if (Race && Interlocked.Increment(ref reads) <= 2)
            {
                if (reads == 2) { gate.TrySetResult(); }
                await gate.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return current;
        }
        public ValueTask<bool> TryCreateAsync(StoredGameSession session, CancellationToken cancellationToken) => inner.TryCreateAsync(session, cancellationToken);
        public ValueTask<bool> TryReplaceAsync(string gameId, string expectedStateHash, StoredGameSession replacement, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref replacements);
            return inner.TryReplaceAsync(gameId, expectedStateHash, replacement, cancellationToken);
        }
        public ValueTask<bool> DeleteAsync(string gameId, CancellationToken cancellationToken) => inner.DeleteAsync(gameId, cancellationToken);
    }
}
