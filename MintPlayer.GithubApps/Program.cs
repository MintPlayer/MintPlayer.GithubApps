using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MintPlayer.AspNetCore.BotFramework;
using MintPlayer.AspNetCore.BotFramework.Extensions;
using MintPlayer.GithubApps;
using Octokit.Webhooks.AspNetCore;
using Smee.IO.Client;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BotOptions>(options =>
{
    options.AppId = builder.Configuration["GithubApp:AppId"];
    options.WebhookUrl = builder.Configuration["GithubApp:WebhookUrl"];
    options.WebhookSecret = builder.Configuration["GithubApp:WebhookSecret"];
    options.PrivateKeyPath = builder.Configuration["GithubApp:PrivateKeyPath"];
});

// Add services to the container.
builder.Services.AddScoped<IGithubService, GithubService>();
builder.AddBotFramework<GithubProcessor>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
app.MapBotFramework();

app.Run();
