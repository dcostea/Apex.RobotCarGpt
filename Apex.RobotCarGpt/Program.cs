using Apex.RobotCarGpt.Commands;
using Microsoft.SemanticKernel;
using Serilog;

namespace Apex.RobotCarGpt;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Logging.ClearProviders();

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            //.MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Debug)
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http", Serilog.Events.LogEventLevel.Warning)
            //.MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console()
            .CreateLogger();

        // Add services to the container.
        builder.Services.AddLogging(c => c.AddSerilog(Log.Logger));
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var kernel = builder.Services.AddKernel();
        kernel.Plugins.AddFromType<Plugins.MotorPlugin.MotorPlugin>();
        kernel.Plugins.AddFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), CommandExtensions.PluginsFolder, CommandExtensions.CommandsPlugin), CommandExtensions.CommandsPlugin);

        builder.Services.AddAzureOpenAIChatCompletion(
            deploymentName: Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!,
            endpoint: Env.Var("AzureOpenAI:Endpoint")!,
            apiKey: Env.Var("AzureOpenAI:ApiKey")!);

        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseHttpsRedirection();

        app.MapControllers();

        app.Run();
    }
}
