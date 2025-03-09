using ApiGateway.Extensions;
using ApiGateway.Models;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Microsoft.AspNetCore.CookiePolicy;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policyBuilder =>
    {
        policyBuilder
            .AllowAnyOrigin()
            .AllowAnyOrigin()
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

app.UseCookiePolicy(
    new CookiePolicyOptions
    {
	MinimumSameSitePolicy = SameSiteMode.None,
	HttpOnly = HttpOnlyPolicy.None,
	Secure = CookieSecurePolicy.None,
    });

app.UseAuthentication();
app.UseAuthorization();

app.UseOcelot().Wait();

// app.UseHttpsRedirection();

app.Run();
