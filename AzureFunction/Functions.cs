using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;

namespace AzureFunction
{
    public static class Functions
    {
        [FunctionName("Get")]
        [OpenApiOperation(operationId: "Get", tags: new[] { "DummyObject" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiParameter(name: "id", In = ParameterLocation.Query, Required = true, Type = typeof(string), Description = "The **Id** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Get(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            ILogger log)
        {
	        if (req.Query?.ContainsKey("id") != true)
	        {
		        return new BadRequestResult();
	        }

	        var container = GetContainer();
	        try
	        {
		        var res = await container.ReadItemAsync<object>(req.Query["id"], new PartitionKey(req.Query["id"]));
		        return new OkObjectResult(res.Resource);
	        }
			catch
			{
				return new NotFoundResult();
			}
        }

        [FunctionName("Post")]
        [OpenApiOperation(operationId: "Post", tags: new[] { "DummyObject" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
		[OpenApiRequestBody("application/json", typeof(object))]
		[OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/json", bodyType: typeof(string), Description = "The OK response")]
        public static async Task<IActionResult> Post(
	        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
	        ILogger log)
        {
	        var container = GetContainer();
	        using var sr = new StreamReader(req.Body);
	        var bodyStr = await sr.ReadToEndAsync();
	        var bodyObj = JsonConvert.DeserializeObject<Dictionary<string, object>>(bodyStr);
	        bodyObj["id"] = Guid.NewGuid().ToString();
	        await container.CreateItemAsync(bodyObj);
	        return new OkObjectResult(new
	        {
		        id = bodyObj["id"]
	        });
        }

        private static Container GetContainer()
        {
	        var connString = Environment.GetEnvironmentVariable("CosmosDbConnectionString");
	        var cosmosClient =
		        new CosmosClient(
			        $"AccountEndpoint={connString}");

	        var db = cosmosClient.GetDatabase("testdatabase");
	        var container = db.GetContainer("testcollection");
            return container;
        }
    }
}

