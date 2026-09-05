using System.Net;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using PosPipeline.Aws;
using PosPipeline.Core.Contracts;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace PosPipeline.StatusApi;

/// <summary>
/// GET /status/{flightId}
///
/// Reports the most recent state of a flight: its latest calculation if there is one, or the
/// latest parsed report if the calculator has not caught up yet. A flight whose report has
/// only just been accepted has neither, and gets a 404 - this is expected immediately after
/// posting a report, not an error.
/// </summary>
public class Function
{
    private const string FlightIdPathParameter = "flightId";

    public async Task<APIGatewayProxyResponse> HandleAsync(APIGatewayProxyRequest request, ILambdaContext context)
    {
        if (request.PathParameters is null
            || !request.PathParameters.TryGetValue(FlightIdPathParameter, out var flightId)
            || string.IsNullOrWhiteSpace(flightId))
        {
            return ApiResponses.Error(HttpStatusCode.BadRequest, "A flight ID is required.");
        }

        try
        {
            var status = await PipelineServices.CreateStatusUseCase().ExecuteAsync(flightId.Trim());

            return status is null
                ? ApiResponses.Error(HttpStatusCode.NotFound, $"No status is available for flight '{flightId}'.")
                : ApiResponses.Json(HttpStatusCode.OK, status);
        }
        catch (Exception error)
        {
            context.Logger.LogError($"Failed to read the status of flight '{flightId}'. {error}");

            return ApiResponses.Error(HttpStatusCode.InternalServerError, "Failed to read the flight status.");
        }
    }
}
