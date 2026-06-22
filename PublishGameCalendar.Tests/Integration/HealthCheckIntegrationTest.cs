using System.Net;
using System.Net.Http.Json;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.Model;
using AquaOs.Calendar.Data.DynamoDb;
using AquaOs.Calendar.DTOs;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace AquaOs.Calendar.Tests.Integration;

public class CalendarIntegrationTest : IAsyncLifetime
{
    private readonly IContainer _dynamoDbContainer = new ContainerBuilder("amazon/dynamodb-local:latest")
        .WithPortBinding(8000, true)
        .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(8000))
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _dynamoDbContainer.StartAsync();

        string serviceUrl = $"http://{_dynamoDbContainer.Hostname}:{_dynamoDbContainer.GetMappedPublicPort(8000)}";

        // Create required DynamoDB tables
        AmazonDynamoDBClient dynamoClient = new(new AmazonDynamoDBConfig { ServiceURL = serviceUrl });
        await CreateTableAsync(dynamoClient, "series", "id");
        await CreateTableAsync(dynamoClient, "polling_config", "series_id");
        await CreateTableAsync(dynamoClient, "manual_events", "SeriesId", "Uid");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the background polling service to avoid interference
                    services.RemoveAll<IHostedService>();

                    // Replace DynamoDB client with one pointing to the container
                    services.RemoveAll<IAmazonDynamoDB>();
                    services.RemoveAll<DynamoDBContext>();
                    services.RemoveAll<IDynamoDbContext>();

                    services.AddSingleton<IAmazonDynamoDB>(_ => new AmazonDynamoDBClient(
                        "fakeKey", "fakeSecret",
                        new AmazonDynamoDBConfig { ServiceURL = serviceUrl }));
                    services.AddSingleton<DynamoDBContext>(sp =>
                        new DynamoDBContext(sp.GetRequiredService<IAmazonDynamoDB>()));
                    services.AddSingleton<IDynamoDbContext>(sp =>
                        new DynamoDbContextAdapter(sp.GetRequiredService<DynamoDBContext>()));
                });
            });

        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _dynamoDbContainer.DisposeAsync();
    }

    [Fact]
    public async Task GetCalendar_ReturnsOkWithEmptyList()
    {
        HttpResponseMessage response = await _client.GetAsync("/api/calendar");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        List<SeriesDto>? series = await response.Content.ReadFromJsonAsync<List<SeriesDto>>();
        Assert.NotNull(series);
        Assert.Empty(series);
    }

    private static async Task CreateTableAsync(IAmazonDynamoDB client, string tableName, string hashKey, string? rangeKey = null)
    {
        List<KeySchemaElement> keySchema = [new KeySchemaElement(hashKey, KeyType.HASH)];
        List<AttributeDefinition> attributes = [new AttributeDefinition(hashKey, ScalarAttributeType.S)];

        if (rangeKey is not null)
        {
            keySchema.Add(new KeySchemaElement(rangeKey, KeyType.RANGE));
            attributes.Add(new AttributeDefinition(rangeKey, ScalarAttributeType.S));
        }

        await client.CreateTableAsync(new CreateTableRequest
        {
            TableName = tableName,
            KeySchema = keySchema,
            AttributeDefinitions = attributes,
            BillingMode = BillingMode.PAY_PER_REQUEST
        });
    }
}
