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
        .AddFilter("Microsoft.SemanticKernel.Connectors", LogLevel.Warning)
        .SetMinimumLevel(LogLevel.Debug)
        .AddConsole()
        .AddDebug();
});

var kernel = new KernelBuilder()
    .WithCompletionService()
    //.WithMemoryStorage(new VolatileMemoryStore())
    .WithLoggerFactory(loggerFactory)
    .Build();

var logger = kernel.LoggerFactory.CreateLogger("MotorPlugin");

_ = kernel.ImportSkill(new Plugins.MotorPlugin(logger));

var config = new SequentialPlannerConfig
{
    RelevancyThreshold = 0.6,
    AllowMissingFunctions = false,
    //Memory = textMemoryProvider
};
var planner = new SequentialPlanner(kernel, config);

var asks = new List<string>
{
  "go like turn left forward turn right backward stop?",
  "go 10 steps where each step is a randomly selected step like forward, backward, and turning left or right?",

  "avoid the tree in front of the car?",
  "do the moonwalk dancing?",
  "move like a jellyfish?",
  "dance like a ballerina?",

  "do a full complete turn?",
  "go on square path?",
  "go on a circle?",
  "do a full circle?",
  "do a full rotation?",
  "do a semi-circle?",
  "do a full circle by turning left followed by a full circle by turning right?",

  "run away?",
  "do an evasive maneuver?",
  "do a pretty complex evasive maneuver with a least 15 steps? Stop at every 5 steps.",
  "move forward, turn left, forward and return in the same place where it started?",
};

var isPlanExcutedStepByStep = false;
var showStepsUsingArrows = true;

foreach (var ask in asks)
{
    Plan plan = null!;

    logger.LogInformation("Ask: {ask}", ask);

    string extractPrompt = """
    You are a minimalistic car capable of some basic commands like forward, backward, turn left, turn right and stop.
    Initial state of the car is stopped.
    Take this action "{{$input}}" and extract a list basic commands, as described above, to fulfil the action.
    """;

    var extractFunction = kernel.CreateSemanticFunction(extractPrompt, maxTokens: 500);
    var response = await extractFunction.InvokeAsync(ask);
    logger.LogInformation("Summarized ask: {response}", response.Result);

    try
    {
        plan = await planner.CreatePlanAsync(response.Result);

        if (showStepsUsingArrows)
        {
            // show steps by function name converted to arrows
            var planStepsArrows = string.Join(" ", plan.Steps.Select(s => s.Name.ToArrow()));
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console.WriteLine(planStepsArrows);
            Console.WriteLine();
        }
        else 
        {
            // show steps by function name
            var planSteps = string.Join(" => ", plan.Steps.Select(s => s.Name));
            logger.LogInformation(planSteps);
        }
    }
    catch (SKException exc)
    {
        logger.LogError("Plan creation failed with exception: {message}", exc.Message);
        continue;
    }

    if (isPlanExcutedStepByStep) 
    {
        // execute plan step by step until complete or at most N steps
        var maxSteps = 10;
        var input = string.Empty;
        try
        {
            for (int step = 1; plan.HasNextStep && step < maxSteps; step++)
            {
                plan = string.IsNullOrEmpty(ask)
                    ? await kernel.StepAsync(plan)
                    : await kernel.StepAsync(input, plan);

                input = string.Empty;

                if (!plan.HasNextStep)
                {
                    logger.LogTrace("Step {step} - Results SO FAR: {plan.State} - COMPLETE!", step, plan.State);
                    break;
                }

                logger.LogTrace("Step {step} - Results SO FAR: {plan.State}", step, plan.State);
            }
        }
        catch (SKException ex)
        {
            logger.LogError("Step - Execution failed: {message}", ex.Message);
        }
    }
    else 
    {
        // execute plan in one go
        try
        {
            _ = await kernel.RunAsync(plan);
        }
        catch (SKException ex)
        {
            logger.LogError("Plan execution failed: {message}", ex.Message);
        }
    }
}
