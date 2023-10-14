using Commands;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Planners;
using Microsoft.SemanticKernel.Planning;

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
    .WithLoggerFactory(loggerFactory)
    .Build();

var logger = kernel.LoggerFactory.CreateLogger(nameof(Plugins.MotorPlugin));

// import native functions from MotorPlugin
_ = kernel.ImportFunctions(new Plugins.MotorPlugin(logger));

var planner = new SequentialPlanner(kernel);

var asks = new List<string>
{
  "go like turn left forward turn right backward stop?",
  "go 10 steps where each step is a randomly selected step like forward, backward, and turning left or right?",
  "avoid the tree in front of the car?",
  "move forward, turn left, forward and return in the same place where it started?",
  "do the moonwalk dancing?",
  "move like a jellyfish?",
  "dance like a ballerina?",

  "go on square path?",
  "go on a full complete circle?",
  "go on a semi-circle?",
  "do a full 360 degrees rotation?",
  "do a full circle by turning left followed by a full circle by turning right?",

  "run away?",
  "do an evasive maneuver?",
  "do a pretty complex evasive maneuver with a least 15 steps? Stop at every 5 steps.",
};

var isPlanExecutedStepByStep = false;
var isTransformingGoalIntoBasicCommands = true;

foreach (var ask in asks)
{
    Plan plan = null!;

    logger.LogInformation("ASK: {ask}", ask);

    try
    {
        if (isTransformingGoalIntoBasicCommands)
        {
            // 1. Extract commands by creating and invoking an inline semantic function (naive approach, default arguments)
            //var extractedMotorCommandsFromAsk = await kernel.ExtractCommandsUsingInlineSemanticFunctionAsync(ask);

            // 2. Extract commands by registering and running a semantic function (SK-like approach)
            //var extractedMotorCommandsFromAsk = await kernel.ExtractCommandsUsingRegisteredSemanticFunctionAsync(ask);

            // 3. Extract commands by importing a semantic function defined in a plugin (in our case, same as MotorPlugin)
            var extractedMotorCommandsFromAsk = await kernel.ExtractCommandsUsingPluginSemanticFunctionAsync(ask);

            logger.LogInformation("Extracted motor commands: {response}", extractedMotorCommandsFromAsk);

            plan = await planner.CreatePlanAsync(extractedMotorCommandsFromAsk!);
        }
        else
        {
            plan = await planner.CreatePlanAsync(ask);
        }

        // show steps as function name converted to arrows
        var planStepsArrows = string.Join(" ", plan.Steps.Select(s => s.Name.ToUpper().ToArrow()));
        Console.OutputEncoding = System.Text.Encoding.Unicode;
        Console.WriteLine(planStepsArrows);
        Console.WriteLine();
    }
    catch (SKException exc)
    {
        logger.LogError("Plan creation failed with exception: {message}", exc.Message);
        continue;
    }

    if (isPlanExecutedStepByStep)
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
            var result = await kernel.RunAsync(plan);
            logger.LogInformation("Plan result: {result}", result.FunctionResults);
        }
        catch (SKException ex)
        {
            logger.LogError("Plan execution failed: {message}", ex.Message);
        }
    }
}
