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
            //.MinimumLevel.Debug()
            //.MinimumLevel.Information()
            .MinimumLevel.Warning()
            .MinimumLevel.Override("Microsoft", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System", Serilog.Events.LogEventLevel.Warning)
            .MinimumLevel.Override("System.Net.Http", Serilog.Events.LogEventLevel.Warning)
            .WriteTo.Console()
            .CreateLogger();

        // Add services to the container.
        builder.Services.AddLogging(c => c.AddSerilog(Log.Logger));
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        var kernel = builder.Services.AddKernel();

        builder.Services.AddAzureOpenAIChatCompletion(
            deploymentName: Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!,
            endpoint: Env.Var("AzureOpenAI:Endpoint")!,
            apiKey: Env.Var("AzureOpenAI:ApiKey")!);

        //builder.Services.ConfigureHttpClientDefaults(c =>
        //{
        //    c.AddStandardResilienceHandler().Configure(o =>
        //    {
        //        var timeSpan = TimeSpan.FromSeconds(40);
        //        o.Retry.MaxRetryAttempts = 5;
        //        o.AttemptTimeout.Timeout = timeSpan;
        //        o.CircuitBreaker.SamplingDuration = 2 * timeSpan;
        //        o.TotalRequestTimeout.Timeout = 3 * timeSpan;
        //    });
        //});

        var app = builder.Build();

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
