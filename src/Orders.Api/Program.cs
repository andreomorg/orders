using System.Text.Json;
using System.Text.Json.Serialization;
using Orders.Api.ErrorHandling;
using Orders.Application;
using Orders.Infrastructure;

const string OpenApiDocumentUrl = "/openapi/v1.json";
const string SwaggerRoute = "/swagger";

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => ConfigureJson(options.JsonSerializerOptions));

// The OpenAPI document is generated from these options, so they must match the controllers' ones
builder.Services.ConfigureHttpJsonOptions(options => ConfigureJson(options.SerializerOptions));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ApiExceptionHandler>();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

await app.Services.InitializeDatabaseAsync();

app.UseExceptionHandler();
// Empty error responses (e.g. 404 for an unknown route) also get a ProblemDetails body
app.UseStatusCodePages();
app.UseHttpsRedirection();

// Swagger is always available, so the API can be explored however it is started
app.MapOpenApi();
app.UseSwaggerUI(options => options.SwaggerEndpoint(OpenApiDocumentUrl, "Orders API"));
app.MapGet("/", () => Results.Redirect(SwaggerRoute)).ExcludeFromDescription();

app.MapControllers();

app.Run();

// Enums travel as text ("Open", "Closed") instead of numbers
static void ConfigureJson(JsonSerializerOptions options) =>
    options.Converters.Add(new JsonStringEnumConverter());
