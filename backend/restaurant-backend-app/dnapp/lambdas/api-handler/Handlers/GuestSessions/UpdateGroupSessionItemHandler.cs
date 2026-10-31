using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using APIHandler.Exceptions;
using APIHandler.Helpers;
using APIHandler.Models.GuestSessions;
using APIHandler.Services.GuestSessions;
using System.Threading.Tasks;

namespace APIHandler.Handlers.GuestSessions;

/// <summary>
/// Reservation owner changes a dish in any session of the group.
/// additive = true: quantity is added to the current one (POST); false: quantity replaces it (PUT).
/// </summary>
public class UpdateGroupSessionItemHandler(IGuestSessionService guestSessionService, bool additive) : IApiRequestHandler
{
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

        if (!RequestBodyHelper.TryParseAndValidate(request, context, out UpdateGroupSessionItemRequest? body, out APIGatewayProxyResponse? errorResponse))
            return errorResponse;

        try
        {
            var snapshot = await guestSessionService.UpdateGroupSessionDishAsync(userId, groupReservationId, guestSessionId, dishId, body.Quantity, additive);
            return ResponseBuilder.CreateJsonResponse(200, snapshot);
        }
        catch (ActionForbiddenException ex)
        {
            return ResponseBuilder.CreateErrorResponse(403, ex.Message, context.Logger);
        }
        catch (System.Exception ex) when (ex is GroupReservationNotFoundException or ReservationNotFoundException
                                              or GuestSessionNotFoundException)
        {
            return ResponseBuilder.CreateErrorResponse(404, ex.Message, context.Logger);
        }
        catch (GroupReservationSubmittedException ex)
        {
            return ResponseBuilder.CreateErrorResponse(409, ex.Message, context.Logger);
        }
        catch (ValidationException ex)
        {
            return ResponseBuilder.CreateErrorCodeResponse(ex);
        }
    }
}
