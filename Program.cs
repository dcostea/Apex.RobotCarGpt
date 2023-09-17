using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;

using var loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Information)
        .AddConsole()
        .AddDebug();
});

var kernel = new KernelBuilder()
    .WithCompletionService()
    .WithLoggerFactory(loggerFactory)
    .Build();

var logger = kernel.LoggerFactory.CreateLogger("MotorPlugin");

_ = kernel.ImportSkill(new Plugins.MotorPlugin(logger), "MotorPlugin");

// Create a planner
var planner = new SequentialPlanner(kernel);
var asks = new List<string> 
{
  "What are the steps the car has to perform to walk like a jellyfish?",
  "What are the steps the car has to perform to go like turn left forward turn right backward stop?",
  "What are the steps the car has to perform to go 10 steps in randomly selected direction like forward, backward, and turning left or right?",
  "What are the steps the car has to perform to avoid a tree?",
  "What are the steps the car has to perform to avoid the tree by going around?",
  "What are the steps the car has to perform to do an evasive maneuver?",
  "What are the steps the car has to perform to run away?",
  "What are the steps the car has to perform to rumba dance?",
  "What are the steps the car has to perform to do some ballerina moves?",
  "What are the steps the car has to perform to go on square path?",
  "What are the steps the car has to perform to go on square path? I don't like tuning left",
  "What are the steps the car has to perform to move and return in the same place where it started?",
  "What are the steps the car has to perform to move forward, turn left, forward and return in the same place where it started?",
  "What are the steps the car has to perform to do a pretty complex evasive maneuver with a least 15 steps? Stop at every 5 steps.",
  "What are the steps the car has to perform to sway (semi-circles)?",
  "What are the steps the car has to perform to do the moonwalk dancing (maximum 10 steps)?",
  "What are the steps the car has to perform to go zigzag for maximum 6 steps and the stop?",
  "What are the steps the car has to perform to go on a circle?",
  "What are the steps the car has to perform to do a full circle?",
  "What are the steps the car has to perform to do a full circle by turning left and then to do a full circle by turning right?",
};

foreach (var ask in asks)
{
    try
    {
        var plan = await planner.CreatePlanAsync(ask);

        logger.LogInformation("Ask: {ask}", ask);
        foreach (var step in plan.Steps)
        {
            logger.LogInformation(step.Name);
        }

        var planResult = await kernel.RunAsync(plan);
        logger.LogDebug("Plan results:");
        logger.LogDebug(planResult.Result);
        //logger.LogDebug(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));
    }
    catch (Exception ex)
    {
        logger.LogError(ex.Message);
        throw;
    }
}
