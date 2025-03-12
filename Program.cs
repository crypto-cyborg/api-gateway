using System.Net;
using ApiGateway.Extensions;
using ApiGateway.Models;
using ApiGateway.Middlewares;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.WebHost.ConfigureKestrel((context, options) =>
{
    options.Listen(IPAddress.Any, 5001, listenOptions =>
    {
        listenOptions.UseHttps(
            "/home/tng/certs/cert.pfx",
            "ihopethiswillworksomeday");
    });
});

const string frontendPolicy = "frontendPolicy";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: frontendPolicy, policyBuilder =>
    {
        policyBuilder
            .WithOrigins("http://192.168.0.1:3000", "http://172.27.192.1:5173", "http://10.1.16.9:3000")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(nameof(JwtOptions)));

builder.Services.AddGatewayIdentity(builder.Configuration);

builder.Configuration.AddJsonFile("ocelot.json");
builder.Services.AddOcelot();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors(frontendPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/healthcheck", () => "Hello World!").RequireAuthorization();

app.UseOcelot().Wait();

// app.UseHttpsRedirection();

app.Run();