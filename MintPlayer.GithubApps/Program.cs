using Microsoft.Extensions.DependencyInjection;
using MintPlayer.GithubApps;
using Smee.IO.Client;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddHostedService<SmeeService>();
builder.Services.AddSingleton<ISmeeClient>((provider) =>
{
    return new SmeeClient(new Uri(builder.Configuration["GithubApp:WebhookProxyUrl"] ?? string.Empty));
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();

app.Run();
