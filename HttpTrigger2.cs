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
// using Microsoft.Extensions.Configuration;

namespace warmupb.f2
{
    public static class HttpTrigger2
    {
        // Lazy initialization of CosmosClient
        private static readonly Lazy<CosmosClient> lazyClient = new Lazy<CosmosClient>(() =>
        {
            var connectionString = Environment.GetEnvironmentVariable("CosmosDBConnection");
            return new CosmosClient(connectionString);
        });

        private static CosmosClient cosmosClient => lazyClient.Value;

        [FunctionName("HttpTrigger2")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log,
            ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            var name = data?.name;

            if (string.IsNullOrEmpty(name))
            {
                return new BadRequestObjectResult("Please pass a name in the request body");
            }

            // Access configuration
            // var config = new ConfigurationBuilder()
            //     .SetBasePath(context.FunctionAppDirectory)
            //     .AddJsonFile("local.settings.json", optional: true, reloadOnChange: true)
            //     .AddEnvironmentVariables()
            //     .Build();

            // string databaseName = config["DatabaseName"];
            // string containerName = config["ContainerName"];
            string databaseName = Environment.GetEnvironmentVariable("DatabaseName");
            string containerName = Environment.GetEnvironmentVariable("ContainerName");

            var database = cosmosClient.GetDatabase(databaseName);
            var container = database.GetContainer(containerName);

            // Create a new item object
            var item = new { id = Guid.NewGuid().ToString(), Name = name };

            try
            {
                // Add the item to the container
                await container.CreateItemAsync(item, new PartitionKey(item.id));

                return new OkObjectResult($"Item with name {name} created successfully");
            }
            catch (Exception e)
            {
                log.LogError($"Error adding item to Cosmos DB: {e.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}
