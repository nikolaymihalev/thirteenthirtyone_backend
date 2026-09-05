using ThirteenThirtyOne.Application.DevelopmentGameplay;

namespace ThirteenThirtyOne.GameBackend.DevelopmentGameplay;

internal static class DevelopmentGameEndpoints
{
    public static void MapDevelopmentGames(this WebApplication app)
    {
        var group = app.MapGroup("/dev/games").WithTags("Development / Gameplay");
        Describe(group.MapPost("", async (CreateGameRequest request, IDevelopmentGameplayService service, CancellationToken token) =>
            Respond(await service.CreateGameAsync(new(request.GameId, request.Players, request.SeedHex), token), 201)),
            "CreateDevelopmentGame", "Create a deterministic in-memory game with an explicit 32-byte hex seed.", 201);

        Describe(group.MapGet("/{gameId}", async (string gameId, IDevelopmentGameplayService service, CancellationToken token) =>
            Respond(await service.GetGameAsync(gameId, token))),
            "GetDevelopmentGame", "Inspect current scores, decision, boundary and safe stack diagnostics.");

        Describe(group.MapPost("/{gameId}/decisions", async (string gameId, SubmitDecisionRequest request,
            IDevelopmentGameplayService service, CancellationToken token) =>
            request.Action is null ? InvalidRequest() : Respond(await service.SubmitDecisionAsync(
                new(gameId, request.DecisionId, request.PlayerId, request.Action.Value, request.TargetPlayerId), token))),
            "SubmitDevelopmentDecision", "Submit Draw, Stop or SelectTarget using the current decision ID and owner.");

        Describe(group.MapPost("/{gameId}/timeouts", async (string gameId, ExpireDecisionRequest request,
            IDevelopmentGameplayService service, CancellationToken token) =>
            request.DecisionKind is null ? InvalidRequest() : Respond(await service.ExpireDecisionAsync(
                new(gameId, request.DecisionId, request.DecisionKind.Value), token))),
            "ExpireDevelopmentDecision", "Simulate PlayerAction or EffectTarget expiry; no real timer is scheduled.");

        Describe(group.MapPost("/{gameId}/continue", async (string gameId, IDevelopmentGameplayService service, CancellationToken token) =>
            Respond(await service.ContinueAsync(gameId, token))),
            "ContinueDevelopmentGame", "Explicitly advance from SafePostResolution into the next turn, round or terminal state.");

        Describe(group.MapDelete("/{gameId}", async (string gameId, IDevelopmentGameplayService service, CancellationToken token) =>
            Respond(await service.DeleteAsync(gameId, token), 204)),
            "DeleteDevelopmentGame", "Delete an in-memory game so its ID can be reused.", 204);
    }

    private static IResult InvalidRequest() => Respond(new(DevelopmentResultKind.InvalidRequest, false, "InvalidRequest", [], null));

    private static IResult Respond(DevelopmentGameOperationResult result, int successStatus = 200)
    {
        var status = result.Kind switch
        {
            DevelopmentResultKind.Success => successStatus,
            DevelopmentResultKind.InvalidRequest => 400,
            DevelopmentResultKind.NotFound => 404,
            DevelopmentResultKind.Conflict => 409,
            DevelopmentResultKind.Rejected => 422,
            _ => throw new InvalidOperationException("Unknown application result."),
        };
        if (status == 204)
        {
            return Results.NoContent();
        }
        var response = DevelopmentGameResponse.From(result);
        return status == 201
            ? Results.Created($"/dev/games/{Uri.EscapeDataString(result.Game!.GameId)}", response)
            : Results.Json(response, statusCode: status);
    }

    private static void Describe(RouteHandlerBuilder route, string name, string summary, int successStatus = 200)
    {
        route.WithName(name).WithSummary(summary)
            .WithDescription("Development-only gameplay test harness. In-memory games disappear on process restart. This is not a production multiplayer API.")
            .Produces<DevelopmentGameResponse>(400).Produces<DevelopmentGameResponse>(404)
            .Produces<DevelopmentGameResponse>(409).Produces<DevelopmentGameResponse>(422);
        if (successStatus == 204)
        {
            route.Produces(204);
        }
        else
        {
            route.Produces<DevelopmentGameResponse>(successStatus);
        }
    }
}
