using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using MongoDB.Bson;
using MongoDB.Driver;
using Newtonsoft.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace AwsLambda
{
    public class Functions
    {
        /// <summary>
        /// Default constructor that Lambda will invoke.
        /// </summary>
        public Functions()
        {
        }

        private IMongoCollection<BsonDocument> GetCollection(ILambdaContext context)
        {
	        var client = new MongoClient(Environment.GetEnvironmentVariable("MongoDbConnectionString"));
	        var db = client.GetDatabase("lambdatest");
	        return db.GetCollection<BsonDocument>("testcollection");
		}

		/// <summary>
		/// A Lambda function to respond to HTTP Get methods from API Gateway
		/// </summary>
		/// <param name="request"></param>
		/// <returns>The API Gateway response.</returns>
		public async Task<APIGatewayProxyResponse> Get(APIGatewayProxyRequest request, ILambdaContext context)
        {
	        if (request.QueryStringParameters?.ContainsKey("id") != true)
	        {
		        return new APIGatewayProxyResponse
		        {
			        StatusCode = (int)HttpStatusCode.BadRequest
		        };
	        }

	        var collection = GetCollection(context);
	        var item = await collection.Find(
		        Builders<BsonDocument>.Filter.Eq("_id", new ObjectId(request.QueryStringParameters["id"]))).SingleOrDefaultAsync();

	        if (item == null)
	        {
		        return new APIGatewayProxyResponse
		        {
			        StatusCode = (int) HttpStatusCode.NotFound
		        };
	        }
			else
			{
				return new APIGatewayProxyResponse
				{
					StatusCode = (int)HttpStatusCode.OK,
					Body = item.ToJson(new MongoDB.Bson.IO.JsonWriterSettings
					{
						OutputMode = MongoDB.Bson.IO.JsonOutputMode.CanonicalExtendedJson
					}),
					Headers = new Dictionary<string, string> { { "Content-Type", "text/json" } }
				};
			}
        }


        /// <summary>
        /// A Lambda function to respond to HTTP Get methods from API Gateway
        /// </summary>
        /// <param name="request"></param>
        /// <returns>The API Gateway response.</returns>
        public async Task<APIGatewayProxyResponse> Post(APIGatewayProxyRequest request, ILambdaContext context)
        {
	        var body = new BsonDocument(JsonConvert.DeserializeObject<Dictionary<string, object>>(request.Body));
	        var collection = GetCollection(context);
	        await collection.InsertOneAsync(body);
	        var response = new APIGatewayProxyResponse
	        {
		        StatusCode = (int)HttpStatusCode.Created,
				Body = JsonConvert.SerializeObject(new { id = body["_id"]}),
				Headers = new Dictionary<string, string> { { "Content-Type", "text/json" } }
			};

	        return response;
        }
    }
}
