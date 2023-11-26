using Commands;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Plugins;
using Serilog;

var loggerFactory = new LoggerFactory()
    .AddSerilog(new LoggerConfiguration()
        //.MinimumLevel.Debug()
        .MinimumLevel.Information()
        //.MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Debug)
        .MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Warning)
        .WriteTo.Console()
        .CreateLogger()
    );

var kernel = new KernelBuilder()
    .WithCompletionService()
    .WithLoggerFactory(loggerFactory)
    .Build();

var logger = kernel.LoggerFactory.CreateLogger(CommandExtensions.MotorPlugin);

var motorPlugin = new MotorPlugin(logger);

// 1. Load semantic functions and native functions into the kernel

// create semantic function inline (instead of importing it from MotorPlugin)
////var extractBasicMotorCommandsSemanticFunction = kernel.CreateSemanticFunction(CommandExtensions.ExtractBasicCommandsPromptTemplate, CommandExtensions.ExtractBasicCommandsPromptTemplateConfig, CommandExtensions.ExtractBasicCommands, CommandExtensions.MotorPlugin);

// import semantic functions from MotorPlugin
var semanticMotorPluginFunctions = kernel.ImportSemanticFunctionsFromDirectory(Path.Combine(Directory.GetCurrentDirectory(), CommandExtensions.PluginsFolder), CommandExtensions.MotorPlugin);
var extractBasicMotorCommandsSemanticFunction = semanticMotorPluginFunctions[CommandExtensions.ExtractBasicCommands];
var extractMostRelevantBasicMotorCommandSemanticFunction = semanticMotorPluginFunctions[CommandExtensions.ExtractMostRelevantBasicCommand];

_ = kernel.ImportFunctions(new Plugins.MotorPlugin(logger), CommandExtensions.MotorPlugin);


var asks = new List<string>
{
  "Go like forward forward turn right backward stop.",
  //"Go 10 steps where each step is a randomly selected step like: move forward, backward, and turning left or right.",
  //"You have a tree in front of the car. Avoid it.",
  //"Move forward, turn left, forward and return in the same place where it started.",
  //"Do a full circle by turning left followed by a full circle by turning right.",
  "Run away.",
  //"Do an evasive maneuver.",
  //"Do a pretty complex evasive maneuver with a least 15 steps. Stop at every 5 steps.",
  //"Do the moonwalk dancing.",
  //"Move like a jellyfish.",
  "Dance like a ballerina.",
  //"Go on square path.",
  //"Go on a full complete circle.",
  //"Go on a semi-circle.",
  //"Do a full 360 degrees rotation.",
};

// Create prompt renderers
////var promptRenderer = new BasicPromptTemplateFactory();
////var extractBasicCommandsRenderedPromptTemplate = promptRenderer.Create(CommandExtensions.ExtractBasicCommandsPromptTemplate, CommandExtensions.ExtractBasicCommandsPromptTemplateConfig);
////var extractMostRelevantBasicCommandRenderedPromptTemplate = promptRenderer.Create(CommandExtensions.ExtractMostRelevantBasicCommandPromptTemplate, CommandExtensions.ExtractMostRelevantBasicCommandPromptTemplateConfig);


// 2. Initialize context variables

var variables = new ContextVariables();
variables.Set("commands", CommandExtensions.BasicCommands);

foreach (var ask in asks)
{
    logger.LogInformation("----------------------------------------------------------------------------------------------------");
    logger.LogInformation("ASK: {ask}", ask);

    variables.Update(ask);

    // Rendered prompts
    ////var extractBasicCommandsRenderedPrompt = await extractBasicCommandsRenderedPromptTemplate.RenderAsync(kernel.CreateNewContext(variables));
    ////logger.LogDebug("RENDERED BASIC COMMANDS PROMPT: {renderedPrompt}", extractBasicCommandsRenderedPrompt);
    ////var extractMostRelevantBasicCommandRenderedPrompt = await extractMostRelevantBasicCommandRenderedPromptTemplate.RenderAsync(kernel.CreateNewContext(variables));
    ////logger.LogDebug("RENDERED MOST RELEVANT BASIC COMMAND PROMPT: {renderedPrompt}", extractMostRelevantBasicCommandRenderedPrompt);

    try
    {
        #region One or more actions using original ask. Choose desired execution.
        ////await kernel.CreateAndExecuteActionPlan(ask, variables, logger);
        ////await kernel.CreateAndExecuteSequentialPlan(ask, variables, logger);
        #endregion

        #region One action using refined ask (as most relevant basic command)
        ////var refinedAskMostRelevantResult = await kernel.RunAsync(extractMostRelevantBasicMotorCommandSemanticFunction, variables);
        ////var refinedAskMostRelevant = refinedAskMostRelevantResult.FunctionResults.First().GetValue<string>();
        ////logger.LogInformation("REFINED ASK (most relevant): {ask}", refinedAskMostRelevant);

        ////await kernel.CreateAndExecuteActionPlan(refinedAskMostRelevant!, variables, logger);
        #endregion

        #region More actions using refined ask (as list of basic commands). Choose desired execution.
        var refinedAskListResult = await kernel.RunAsync(extractBasicMotorCommandsSemanticFunction, variables);
        var refinedAskList = refinedAskListResult.FunctionResults.First().GetValue<string>();
        logger.LogInformation("REFINED ASK (list): {ask}", refinedAskList);

        ////await kernel.CreateAndExecuteFunctionsChain(new List<string> { nameof(motorPlugin.Forward), nameof(motorPlugin.Backward) }, variables, logger);
        ////await kernel.CreateAndExecuteFunctionsAsSequenceOfActionPlan(refinedAskList!, variables, logger);
        await kernel.CreateAndExecuteSequentialPlan(refinedAskList!, variables, logger);
        #endregion

    }
    catch (SKException ex)
    {
        logger.LogError("FAILED with exception: {message}", ex.Message);
        continue;
    }
}
