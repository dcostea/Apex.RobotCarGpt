using Commands;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planners;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.TemplateEngine.Basic;
using Microsoft.SemanticKernel.TemplateEngine;
using Serilog;

var loggerFactory = new LoggerFactory()
    .AddSerilog(new LoggerConfiguration()
        .MinimumLevel.Debug()
        //.MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Debug)
        //.MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console()
        .CreateLogger()
    );

var kernel = new KernelBuilder()
    .WithCompletionService()
    .WithLoggerFactory(loggerFactory)
    .Build();

const string Commands = "go forward, go backward, turn left, turn right, and stop";
const string MotorPlugin = nameof(MotorPlugin);
const string ExtractBasicMotorCommands = nameof(ExtractBasicMotorCommands);

const string ExtractBasicMotorPromptTemplate = """
You are a robot car capable of performing only the following allowed basic commands: {{ $commands }}.
Initial state of the car is stopped. The last state of the car is stopped.
You need to:
[START ACTION TO BE PERFORMED]
{{ $input }}
[END ACTION TO BE PERFORMED]
Create a comma separated list of basic commands, as enumerated above, to fulfill the goal.
Remove any introduction, ending or explanation from the response, show me only the list of allowed commands.
""";

var logger = kernel.LoggerFactory.CreateLogger(MotorPlugin);

var asks = new List<string>
{
  "Go like forward forward turn right backward stop.",
  "Go 10 steps where each step is a randomly selected step like: move forward, backward, and turning left or right.",
  "You have a tree in front of the car. Avoid it.",
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

var promptRenderer = new BasicPromptTemplateFactory();
var promptTemplateConfig = new PromptTemplateConfig
{
    Description = "Extract basic motor commands.",
    ModelSettings =
    [
        new OpenAIRequestSettings
        {
            MaxTokens = 500,
            Temperature = 0.0
        }
    ],
    Input = new PromptTemplateConfig.InputConfig
    {
        Parameters =
        [
            new() { Name = "input", Description = "Action to be performed." },
            new() { Name = "commands", Description = "The commands to chose from." },
        ]
    }
};
var renderedPromptTemplate = promptRenderer.Create(ExtractBasicMotorPromptTemplate, promptTemplateConfig);


// 0. Configure how to run

const bool CreatePlanWithRefinedAsk = true;
const bool CreateInlineSemanticFunction = true;


// 1. Load semantic functions and native functions into the kernel

ISKFunction extractBasicMotorCommandsSemanticFunction = null!;
if (CreateInlineSemanticFunction)
{
    // create semantic function inline (instead of importing it from MotorPlugin)
    extractBasicMotorCommandsSemanticFunction = kernel.CreateSemanticFunction(ExtractBasicMotorPromptTemplate, promptTemplateConfig, ExtractBasicMotorCommands, MotorPlugin);
}
else 
{
    // import semantic function from MotorPlugin
    var semanticMotorPluginFunctions = kernel.ImportSemanticFunctionsFromDirectory(Path.Combine(Directory.GetCurrentDirectory(), "plugins"), MotorPlugin);
    extractBasicMotorCommandsSemanticFunction = semanticMotorPluginFunctions[ExtractBasicMotorCommands];
}

// import native functions from MotorPlugin
_ = kernel.ImportFunctions(new Plugins.MotorPlugin(logger), MotorPlugin);

var config = new SequentialPlannerConfig();
config.ExcludedFunctions.Add(ExtractBasicMotorCommands);    // we don't want to use this function in the plan
var planner = new SequentialPlanner(kernel, config);

foreach (var ask in asks)
{
    Plan plan = null!;


    // 2. Initialize context variables

    logger.LogInformation("----------------------------------------------------------------------------------------------------");
    logger.LogInformation("ASK: {ask}", ask);

    var variables = new ContextVariables(ask);
    variables.Set("commands", Commands);

    var renderedPrompt = await renderedPromptTemplate.RenderAsync(kernel.CreateNewContext(variables));
    logger.LogDebug("RENDERED PROMPT: {renderedPrompt}", renderedPrompt);


    // 3. Create plan

    try
    {
        if (!CreatePlanWithRefinedAsk)
        {
            plan = await planner.CreatePlanAsync(ask);
        }
        else
        {
            // refine ask
            var refinedAskResult = await kernel.RunAsync(extractBasicMotorCommandsSemanticFunction, variables);
            var refinedAsk = refinedAskResult.FunctionResults.First().GetValue<string>();
            logger.LogInformation("EXTRACTED COMMANDS: {extracted}", refinedAsk);

            plan = await planner.CreatePlanAsync(refinedAsk!);
        }

        // show steps as arrows (function names converted to arrows)
        var planStepsArrows = plan.Steps.Any()
            ? string.Join(" ", plan.Steps.Select(s => s.Name.ToArrow()))
            : "No steps!";
        logger.LogInformation("PLAN STEPS: {planStepsArrows}", planStepsArrows);
    }
    catch (SKException ex)
    {
        logger.LogError("PLAN CREATION FAILED with exception: {message}", ex.Message);
        continue;
    }


    // 4. Execute plan

    try
    {
        var result = await kernel.RunAsync(plan, variables);
        logger.LogInformation("PLAN RESULT: {result}", result.FunctionResults.First().GetValue<string>());
    }
    catch (SKException ex)
    {
        logger.LogError("PLAN EXECUTION FAILED: {message}", ex.Message);
    }
}
