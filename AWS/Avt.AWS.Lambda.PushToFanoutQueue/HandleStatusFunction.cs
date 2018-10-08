using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Amazon;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Newtonsoft.Json;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace AVT.AWS.Lambda.PushToFanoutQueue
{
    public class HandleStatusFunction
    {
        public async Task<APIGatewayProxyResponse> ArrivalStatusHandler(APIGatewayProxyRequest gatewayEvent, ILambdaContext context)
        {
            string[] validParams = { "vin", "status", "date" };
            ProcessStatus processStatus = ProcessStatus.Initial;
            var model = new StatusModel();

            if (gatewayEvent == null)
            {
                context.Logger.LogLine("###### why Api Geteway request event is NULL?!!! ###### ");
                return new APIGatewayProxyResponse() { StatusCode = (int)HttpStatusCode.BadRequest };
            }
            else
            {
                if (gatewayEvent.QueryStringParameters == null)
                {
                    context.Logger.LogLine($">> QueryStringParameters in Api Gateway request is NULL");
                    return new APIGatewayProxyResponse() { StatusCode = (int)HttpStatusCode.BadRequest };
                }
                else
                {
                    if (gatewayEvent.QueryStringParameters.Keys.Count == 0 ||
                        !gatewayEvent.QueryStringParameters.ContainsKey("vin"))
                    {
                        context.Logger.LogLine(
                            $">> No item in the query string, you need to provide at least \"vin\" parameter...");
                        return new APIGatewayProxyResponse() { StatusCode = (int)HttpStatusCode.BadRequest };
                    }
                    else
                    {
                        var keys = gatewayEvent.QueryStringParameters.Keys;
                        foreach (var key in keys)
                        {
                            var _key = key.ToLowerInvariant();
                            if (validParams.Contains(_key))
                            {
                                switch (_key)
                                {
                                    case "vin":
                                        if (string.IsNullOrEmpty(_key))
                                        {
                                            processStatus = ProcessStatus.Invalid;
                                            break;
                                        }

                                        processStatus = ProcessStatus.Valid;
                                        model.VehicleId = gatewayEvent.QueryStringParameters[_key];
                                        break;
                                    case "status":
                                        model.VehicleStatus =
                                            !string.IsNullOrEmpty(gatewayEvent.QueryStringParameters[_key]) &&
                                            new[] { "0", "1" }.Contains(
                                                gatewayEvent.QueryStringParameters[_key])
                                                ? model.VehicleStatus =
                                                    int.Parse(gatewayEvent.QueryStringParameters[_key])
                                                : 1;
                                        break;
                                    case "date":
                                        model.StatusDate =
                                            !string.IsNullOrEmpty(gatewayEvent.QueryStringParameters[_key]) &&
                                            DateTime.TryParseExact(gatewayEvent.QueryStringParameters[_key],
                                                "yyyy-MM-dd_HH:mm:ss.fff",
                                                CultureInfo.InvariantCulture,
                                                DateTimeStyles.None,
                                                out var _date)
                                                ? _date.ToString("yyyy-MM-dd HH:mm:ss.fff")
                                                : DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");
                                        break;
                                }

                                if (processStatus == ProcessStatus.Invalid)
                                    break;
                            }
                        }

                        if (string.IsNullOrEmpty(model.StatusDate))
                            model.StatusDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fff");

                        if (processStatus == ProcessStatus.Valid)
                        {
                            var snsClient = new AmazonSimpleNotificationServiceClient(RegionEndpoint.EUWest1);

                            var msg = JsonHelper.SerializeObject(model);
                            var publishReq =
                                new PublishRequest("arn:aws:sns:eu-west-1:166778461577:VehicleStatusNotification",
                                    msg,
                                    "New Status Arrived");
                            await snsClient.PublishAsync(publishReq);
                        }
                        else
                        {
                            return new APIGatewayProxyResponse() { StatusCode = (int)HttpStatusCode.InternalServerError };
                        }
                    }
                }
            }

            return new APIGatewayProxyResponse(){ StatusCode = 200 };
        }
    }


    public enum ProcessStatus
    {
        Initial, Invalid, Valid
    }

    public class StatusModel
    {
        [JsonProperty("vin")]
        public string VehicleId { get; set; }

        [JsonProperty("status")]
        public int VehicleStatus { get; set; } = 1;

        [JsonProperty("date")]
        public string StatusDate { get; set; }
    }
}
