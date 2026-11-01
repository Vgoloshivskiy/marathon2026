using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.GuestSessions;
using APIHandler.Services.GuestSessions;
using System.Text.Json;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

/// <summary>Owner-only: sets the absolute quantity of a dish line in any guest session of the group (&lt;=0 removes it) and returns the updated group snapshot.</summary>
public class UpdateGroupSessionItemQuantityHandler(IGuestSessionService guestSessionService) : IApiRequestHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<APIGatewayProxyResponse> HandleRequest(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (!request.TryGetPathValue("groupReservationId", out var groupReservationId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'groupReservationId' in path parameters.", context.Logger);

        if (!request.TryGetPathValue("guestSessionId", out var guestSessionId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'guestSessionId' in path parameters.", context.Logger);

        if (!request.TryGetPathValue("dishId", out var dishId))
            return ResponseBuilder.CreateErrorResponse(400, "Missing 'dishId' in path parameters.", context.Logger);

        if (request.RequestContext?.Authorizer?.Claims == null ||
            !request.RequestContext.Authorizer.Claims.TryGetValue("sub", out var userId))
        {
            return ResponseBuilder.CreateErrorResponse(401, "Unauthorized.", context.Logger);
        }

        SetGroupSessionItemQuantityRequest? body;
        try
        {
            body = string.IsNullOrWhiteSpace(request.Body)
                ? null
                : JsonSerializer.Deserialize<SetGroupSessionItemQuantityRequest>(request.Body, JsonOptions);
        }
        catch (JsonException)
        {
            body = null;
        }

        if (body?.Quantity is not int quantity)
            return ResponseBuilder.CreateErrorResponse(400, "Invalid request body: 'quantity' (integer) is required.", context.Logger);

        try
        {
            var snapshot = await guestSessionService.SetDishQuantityInGroupSessionAsync(userId, groupReservationId, guestSessionId, dishId, quantity);
            return ResponseBuilder.CreateSuccessResponse(snapshot);
        }
        catch (ActionForbiddenException ex)
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger);
        }
        catch (System.Exception ex) when (ex is GroupReservationNotFoundException or ReservationNotFoundException
                                              or GuestSessionNotFoundException or DishNotFoundException)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorCodeResponse(ex);
        }
    }
}
