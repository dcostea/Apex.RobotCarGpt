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
        .AddFilter("Microsoft.SemanticKernel", LogLevel.Warning)
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

var asks = new List<string>
{
  "Go like turn left forward turn right backward stop.",
  "Go 10 steps where each step is a randomly selected step like forward, backward, and turning left or right.",
  "Avoid the tree in front of the car.",
  "Move forward, turn left, forward and return in the same place where it started.",
  "Do a full circle by turning left followed by a full circle by turning right.",
  "Run away.",
  "Do an evasive maneuver.",
  "Do a pretty complex evasive maneuver with a least 15 steps. Stop at every 5 steps.",
  "Do the moonwalk dancing.",
  "Move like a jellyfish.",
  "Dance like a ballerina.",
  "Go on square path.",
  "Go on a full complete circle.",
  "Go on a semi-circle.",
  "Do a full 360 degrees rotation.",
};

var isTransformingGoalIntoBasicMotorCommands = true;

foreach (var ask in asks)
{
    Plan plan = null!;

    logger.LogInformation(new string('-', 80));
    logger.LogInformation("ASK: {ask}", ask);

    try
    {
        if (isTransformingGoalIntoBasicMotorCommands)
        {
            const string Commands = "go forward, go backward, turn left, turn right, and stop";

            // 1. Extract basic motor commands by creating and invoking an inline semantic function (naive approach, default arguments)
            //var extractedBasicMotorCommandsFromAsk = await kernel.ExtractBasicCommandsUsingInlineSemanticFunctionAsync(ask, Commands, logger);

            // 2. Extract basic motor commands by registering and running a semantic function (SK-like approach)
            //var extractedBasicMotorCommandsFromAsk = await kernel.ExtractBasicCommandsUsingRegisteredSemanticFunctionAsync(ask, Commands);

            // 3. Extract basic motor commands by importing a semantic function defined in a plugin (in our case, same as MotorPlugin)
            var extractedBasicMotorCommandsFromAsk = await kernel.ExtractBasicCommandsUsingPluginSemanticFunctionAsync(ask, Commands);

            logger.LogInformation("[START EXTRACTED BASIC MOTOR COMMANDS]\n{extracted}\n[END EXTRACTED BASIC MOTOR COMMANDS]", extractedBasicMotorCommandsFromAsk);

            // import native functions from MotorPlugin
            _ = kernel.ImportFunctions(new Plugins.MotorPlugin(logger));
            var planner = new SequentialPlanner(kernel);
            plan = await planner.CreatePlanAsync(extractedBasicMotorCommandsFromAsk!);
        }
        else
        {
            // import native functions from MotorPlugin
            _ = kernel.ImportFunctions(new Plugins.MotorPlugin(logger));
            var planner = new SequentialPlanner(kernel);
            plan = await planner.CreatePlanAsync(ask);
        }


        if (plan.Steps.Any())
        {
            // show steps as function name converted to arrows
            var planStepsArrows = string.Join(" ", plan.Steps.Select(s => s.Name.ToUpper().ToArrow()));

            // I'm using Console.WriteLine instead of logging because Unicode arrows are not supported by the logger
            Console.OutputEncoding = System.Text.Encoding.Unicode;
            Console.WriteLine(planStepsArrows);
            Console.WriteLine();
        }
        else 
        {
            logger.LogInformation("No steps!");
        }
    }
    catch (SKException exc)
    {
        logger.LogError("PLAN CREATION FAILED with exception: {message}", exc.Message);
        continue;
    }

    try
    {
        var result = await kernel.RunAsync(plan);
        logger.LogInformation("PLAN RESULT: {result}", result.FunctionResults.First().GetValue<string>());
    }
    catch (SKException ex)
    {
        logger.LogError("PLAN EXECUTION FAILED: {message}", ex.Message);
    }
}
