using Apex.RobotCarGpt;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Memory;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Sequential;
using Microsoft.SemanticKernel.Orchestration;

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

var isPlanExcutedStepByStep = false;
var isExtractingMotorCommands = true;
var showStepsUsingArrows = true;

foreach (var ask in asks)
{
    Plan plan = null!;

    logger.LogInformation("ASK: {ask}", ask);

    try
    {
        if (isExtractingMotorCommands)
        {
            string extractMotorCommandsPrompt = """
    You are a minimalistic car capable of performing some basic commands like going forward, going backward, turn left, turn right and stop.
    Initial state of the car is stopped.
    Take this goal "{{$input}}" and create a list of basic commands, as enumerated above, to fulfil the goal. 
    """;

            var extractMotorCommandsFunction = kernel.CreateSemanticFunction(extractMotorCommandsPrompt, maxTokens: 500);
            var extractedMotorCommands = await extractMotorCommandsFunction.InvokeAsync(ask);
            logger.LogInformation("Extracted motor commands: {response}", extractedMotorCommands.Result);

            plan = await planner.CreatePlanAsync(extractedMotorCommands.Result);
        }
        else 
        {
            plan = await planner.CreatePlanAsync(ask);
        }

        if (showStepsUsingArrows)
        {
            // show steps by function name converted to arrows
            var planStepsArrows = string.Join(" ", plan.Steps.Select(s => s.Name.ToUpper().ToArrow()));
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console.WriteLine(planStepsArrows);
            Console.WriteLine();
        }
        else 
        {
            // show steps by function name
            var planSteps = string.Join(" => ", plan.Steps.Select(s => s.Name.ToUpper()));
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
            var result = await kernel.RunAsync(plan);
            logger.LogInformation("Plan result: {result}", result.Result);
        }
        catch (SKException ex)
        {
            logger.LogError("Plan execution failed: {message}", ex.Message);
        }
    }
}
