using System.Globalization;

using Azure.Storage.Blobs;
using CsvHelper.Configuration;
using EVN_NHTSA.Services;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Newtonsoft.Json;

var builder = FunctionsApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IBlobStorageService, BlobStorageService>();
builder.Services.AddSingleton<ICsvService, CsvService>();
builder.Services.AddSingleton<IDatabaseService, CosmosDBService>();
builder.Services.AddSingleton<IRequestWorkerFactory, RequestWorkerFactory>();
builder.Services.AddSingleton(new CosmosClient(builder.Configuration.GetValue<string>("CosmosDBConnectionString")));
builder.Services.AddSingleton(new BlobServiceClient(builder.Configuration.GetValue<string>("BlobConnectionString")));
builder.Services.AddSingleton<INHTSAService, NHTSAService>();

builder.Services.AddTransient<IRequestWorker, RequestWorker>();

builder.Services.AddSingleton(sp => new JsonSerializerSettings()
{
    Formatting = Formatting.Indented,
    MissingMemberHandling = MissingMemberHandling.Ignore,
    NullValueHandling = NullValueHandling.Ignore
});

builder.Services.AddSingleton(sp => new CsvConfiguration(CultureInfo.InvariantCulture)
{
	PrepareHeaderForMatch = args => args.Header.ToLower(),
	IgnoreBlankLines = true,
	MissingFieldFound = null,
});

await Task.WhenAll(
    CheckAndUploadCsvToBlobStorageAsync(builder.Configuration),
    CheckAndCreateCosmosContainerAsync(builder.Configuration)
);

var app = builder.Build();

app.Run();

async Task CheckAndUploadCsvToBlobStorageAsync(IConfiguration configuration)
{
	if (File.Exists("Data/sample-vin-data.csv"))
	{
		await using var fileStream = File.OpenRead("Data/sample-vin-data.csv");

		var blobServiceClient = new BlobServiceClient(configuration.GetValue<string>("BlobConnectionString"));
		var containerClient = blobServiceClient.GetBlobContainerClient(configuration.GetValue<string>("BlobContainerName"));
		await containerClient.CreateIfNotExistsAsync();
		var blobClient = containerClient.GetBlobClient(configuration.GetValue<string>("BlobContainerName") + ".csv");
		await blobClient.UploadAsync(fileStream, overwrite: true);
	}
	else
	{
		throw new FileNotFoundException($"CSV File not found at {"Data/sample-vin-data.csv"}");
	}
}

async Task CheckAndCreateCosmosContainerAsync(IConfiguration configuration)
{
    var client = new CosmosClient(configuration.GetValue<string>("CosmosDBConnectionString"));
    var database = await client.CreateDatabaseIfNotExistsAsync(configuration.GetValue<string>("DatabaseName"));
    await database.Database.CreateContainerIfNotExistsAsync(configuration.GetValue<string>("VehiclesContainer"), "/id");
}