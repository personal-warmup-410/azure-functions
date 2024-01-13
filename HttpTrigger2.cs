using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;

namespace warmupb.f2
{
    public static class HttpTrigger2
    {
        private static readonly Lazy<CosmosClient> lazyClient = new Lazy<CosmosClient>(() =>
        {
            var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException("CosmosDB connection string is not set.");
            }
            return new CosmosClient(connectionString);
        });

        private static CosmosClient cosmosClient => lazyClient.Value;

        [FunctionName("HttpTrigger2")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function started processing a request.");

            dynamic data;
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                data = JsonConvert.DeserializeObject(requestBody);
                log.LogInformation("Deserialization successful.");
            }
            catch (Exception ex)
            {
                log.LogError($"Error during deserialization: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            string name = data?.name;

            if (string.IsNullOrEmpty(name))
            {
                log.LogWarning("Name parameter is empty.");
                return new BadRequestObjectResult("Please pass a name in the request body");
            }
            log.LogInformation("point 3");

            string databaseName = Environment.GetEnvironmentVariable("DatabaseName");
            log.LogInformation("point 4");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");
            log.LogInformation("point 5");


            if (string.IsNullOrEmpty(databaseName) || string.IsNullOrEmpty(containerName))
            {
                log.LogError("Database name or container name is not set in environment variables.");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            log.LogInformation("point 6");
            try
            {
                log.LogInformation("point 7");

                log.LogInformation($"Attempting to get database: {databaseName}");
                var database = cosmosClient.GetDatabase(databaseName);
                log.LogInformation("point 8");

                log.LogInformation($"Attempting to get container: {containerName}");
                var container = database.GetContainer(containerName);
                log.LogInformation("point 9");
                var item = new { id = Guid.NewGuid().ToString(), name = name };
                log.LogInformation($"Creating item with name: {name}");

                await container.CreateItemAsync(item, new PartitionKey(item.id));
                string usersPartitionKey = "/users"
                log.LogInformation("Item created successfully.");

                return new OkObjectResult($"Item with name {name} created successfully");
            }
            catch (Exception e)
            {
                log.LogError($"Error adding item to Cosmos DB: {e.Message}");
                if (e.InnerException != null)
                {
                    log.LogError($"Inner exception: {e.InnerException.Message}");
                }
                // set the partition key to our single partition '/users'
                await container.CreateItemAsync(newUser, new PartitionKey(usersPartitionKey));
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
