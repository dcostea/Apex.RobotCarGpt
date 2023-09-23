using Apex.RobotCarGpt;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Sequential;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .AddFilter("Microsoft", LogLevel.Warning)
        .AddFilter("System", LogLevel.Warning)
        .AddFilter("Plugins.MotorPlugin", LogLevel.Warning)
        .AddFilter("Microsoft.SemanticKernel", LogLevel.Debug)
        .AddFilter("Microsoft.SemanticKernel.SkillDefinition", LogLevel.Warning)
        .SetMinimumLevel(LogLevel.Debug)
        .AddConsole()
        .AddDebug();
});
/*
var loggerFactory = new LoggerFactory(new[] { new ConsoleLoggerProvider(optionsMonitor) }, 
new LoggerFilterOptions { MinLevel = LogLevel.Information });  
 */

var kernel = new KernelBuilder()
    .WithCompletionService()
    //.WithMemoryStorage(new VolatileMemoryStore())
    .WithLoggerFactory(loggerFactory)
    .Build();

var logger = kernel.LoggerFactory.CreateLogger("MotorPlugin");

_ = kernel.ImportSkill(new Plugins.MotorPlugin(logger));

// Create a planner
var config = new SequentialPlannerConfig
{
    RelevancyThreshold = 0.6,
    AllowMissingFunctions = true,
    //Memory = textMemoryProvider
};
var planner = new SequentialPlanner(kernel, config);

var asks = new List<string>
{
  "What are the steps the car has to perform to walk like a jellyfish?",
  "What are the steps the car has to perform to go like turn left forward turn right backward stop?",
  "What are the steps the car has to perform to go on square path?",
  "What are the steps the car has to perform to go on square path? I prefer to start turning right",
  "What are the steps the car has to perform to go on square path? I prefer to start turning left",
  "What are the steps the car has to perform to go 10 steps in randomly selected direction like forward, backward, and turning left or right?",
  "What are the steps the car has to perform to avoid a tree?",
  "What are the steps the car has to perform to do an evasive maneuver?",
  "What are the steps the car has to perform to run away?",
  "What are the steps the car has to perform to dance rumba?",
  "What are the steps the car has to perform to do some ballerina moves?",
  "What are the steps the car has to perform to move forward, turn left, forward and return in the same place where it started?",
  "What are the steps the car has to perform to do a pretty complex evasive maneuver with a least 15 steps? Stop at every 5 steps.",
  "What are the steps the car has to perform to sway (semi-circles)?",
  "What are the steps the car has to perform to do the moonwalk dancing (maximum 10 steps)?",
  "What are the steps the car has to perform to go zigzag for maximum 6 steps and the stop?",
  "What are the steps the car has to perform to go on a circle?",
  "What are the steps the car has to perform to do a semi-circle?",
  "What are the steps the car has to perform to do a semi-circle? A semi-circle is turning 180 degrees",
  "What are the steps the car has to perform to do a full circle?",
  "What are the steps the car has to perform to do a full circle by turning left and then to do a full circle by turning right?",
};

foreach (var ask in asks)
{
    var cancellationTokenSource = new CancellationTokenSource();
    cancellationTokenSource ??= new CancellationTokenSource();
    var cancellationToken = cancellationTokenSource.Token;

    Plan plan = null!;

    try
    {
        logger.LogInformation("\nAsk: {ask}", ask);

        plan = await planner.CreatePlanAsync(ask, cancellationToken);
        //plan.UseCompletionSettings(new CompleteRequestSettings
        //{
        //    Temperature = 1,
        //});

        var planSteps = string.Join(" => ", plan.Steps.Select(s => s.Name));
        var planStepsArrows = string.Join(" ", plan.Steps.Select(s => s.Name.ToArrow()));
        //logger.LogInformation(planStepsArrows);
        //logger.LogInformation(planSteps);

        Console.OutputEncoding = System.Text.Encoding.Unicode;
        Console.WriteLine(planStepsArrows);
    }
    catch (SKException exc)
    {
        logger.LogError("Plan failed with exception: {message}", exc.Message);
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
        continue;
    }

    ////var planResult = await kernel.RunAsync(plan, cancellationToken: cancellationToken);
    ////logger.LogDebug("Plan results:");
    ////logger.LogDebug(planResult.Result);

    // loop until complete or at most N steps
    var maxSteps = 10;
    var input = string.Empty;
    try
    {
        for (int step = 1; plan.HasNextStep && step < maxSteps; step++)
        {
            if (string.IsNullOrEmpty(ask))
            {
                await kernel.StepAsync(plan);
            }
            else
            {
                plan = await kernel.StepAsync(input, plan);
                input = string.Empty;
            }

            if (!plan.HasNextStep)
            {
                logger.LogTrace($"Step {step} - Results SO FAR: {plan.State} - COMPLETE!");
                break;
            }

            logger.LogTrace($"Step {step} - Results SO FAR: {plan.State}");
        }

        if (plan.HasNextStep)
        {
            logger.LogTrace($"There are {plan.Steps.Count - maxSteps} MORE STEPS out of {plan.Steps.Count} but we hit maximum steps limit.");
        }
    }
    catch (SKException ex)
    {
        logger.LogError("Step - Execution failed: {message}", ex.Message);
        cancellationTokenSource.Cancel();
        cancellationTokenSource.Dispose();
    }
}
