using Commands;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Diagnostics;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.Planners;
using Microsoft.SemanticKernel.Planning;
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

// 1. LOAD native and semantic functions from MotorPlugin

// CREATE semantic function inline (instead of importing it from MotorPlugin)
var extractBasicCommandsSemanticFunction = kernel.CreateSemanticFunction(CommandExtensions.ExtractBasicCommandsPromptTemplate, CommandExtensions.ExtractBasicCommandsPromptTemplateConfig, CommandExtensions.ExtractBasicCommands, CommandExtensions.MotorPlugin);
var extractMostRelevantBasicCommandSemanticFunction = kernel.CreateSemanticFunction(CommandExtensions.ExtractMostRelevantBasicCommandPromptTemplate, CommandExtensions.ExtractMostRelevantBasicCommandPromptTemplateConfig, CommandExtensions.ExtractMostRelevantBasicCommand, CommandExtensions.MotorPlugin);
var executeBasicCommandSemanticFunction = kernel.CreateSemanticFunction(CommandExtensions.ExecuteBasicCommandPromptTemplate, CommandExtensions.ExecuteBasicCommandPromptTemplateConfig, CommandExtensions.ExecuteBasicCommand, CommandExtensions.MotorPlugin);

// IMPORT semantic functions from MotorPlugin
////var semanticMotorPluginFunctions = kernel.ImportSemanticFunctionsFromDirectory(Path.Combine(Directory.GetCurrentDirectory(), CommandExtensions.PluginsFolder), CommandExtensions.MotorPlugin);
////var extractBasicCommandsSemanticFunction = semanticMotorPluginFunctions[CommandExtensions.ExtractBasicCommands];
////var extractMostRelevantBasicCommandSemanticFunction = semanticMotorPluginFunctions[CommandExtensions.ExtractMostRelevantBasicCommand];
////var executeBasicCommandSemanticFunction = semanticMotorPluginFunctions[CommandExtensions.ExecuteBasicCommand];

// IMPORT native functions from MotorPlugin
var motorPluginFunctions = kernel.ImportFunctions(new Plugins.MotorPlugin(logger), CommandExtensions.MotorPlugin);


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

// Create prompt renderers
////var promptRenderer = new BasicPromptTemplateFactory();
////var extractBasicCommandsRenderedPromptTemplate = promptRenderer.Create(CommandExtensions.ExtractBasicCommandsPromptTemplate, CommandExtensions.ExtractBasicCommandsPromptTemplateConfig);
////var extractMostRelevantBasicCommandRenderedPromptTemplate = promptRenderer.Create(CommandExtensions.ExtractMostRelevantBasicCommandPromptTemplate, CommandExtensions.ExtractMostRelevantBasicCommandPromptTemplateConfig);


// 2. PREPARE CONTEXT VARIABLES

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
        #region Native Function.

        ////var result = await kernel.RunAsync(motorPluginFunctions[nameof(motorPlugin.Backward)], variables);

        #endregion


        #region Semantic Function calling Native Function.

        ////var result = await kernel.RunAsync(executeBasicCommandSemanticFunction, variables);

        #endregion


        // 3. CREATE AND EXECUTE PLANS

        #region Action Plan => One step.

        ////var plan = await kernel.CreateActionPlan(ask, logger);
        ////var result = await kernel.RunAsync(variables, plan);

        #endregion


        #region Sequential Plan => Multiple steps.

        ////var plan = await kernel.CreateSequentialPlan(ask, logger);
        ////var result = await kernel.RunAsync(variables, plan);

        #endregion


        #region Action Plan ('ask' is converted to most relevant basic command) => One step.

        ////var refinedAskMostRelevantResult = await kernel.RunAsync(extractMostRelevantBasicCommandSemanticFunction, variables);
        ////var refinedAskMostRelevant = refinedAskMostRelevantResult.FunctionResults.First().GetValue<string>();
        ////logger.LogInformation("REFINED ASK (most relevant): {ask}", refinedAskMostRelevant);
        ////var plan = await kernel.CreateActionPlan(refinedAskMostRelevant!, logger);
        ////var result = await kernel.RunAsync(variables, plan);

        #endregion


        #region More Action Plans ('ask' is converted to list of basic command) => Multiple steps.

        ////var refinedAskListResult = await kernel.RunAsync(extractBasicCommandsSemanticFunction, variables);
        ////var refinedAskList = refinedAskListResult.FunctionResults.First().GetValue<string>();
        ////logger.LogInformation("REFINED ASK (list): {ask}", refinedAskList);

        ////var plans = new List<ISKFunction>();
        ////foreach (var refinedAsk in refinedAskList!.Split(','))
        ////{
        ////    var plan = await kernel.CreateActionPlan(refinedAsk, logger);
        ////    plans.Add(plan);
        ////}
        ////var result = await kernel.RunAsync(variables, plans.ToArray());

        #endregion


        #region Sequential Plan ('ask' is converted to list of basic command) => Multiple steps.

        var refinedAskListResult = await kernel.RunAsync(extractBasicCommandsSemanticFunction, variables);
        var refinedAskList = refinedAskListResult.FunctionResults.First().GetValue<string>();
        logger.LogInformation("REFINED ASK (list): {ask}", refinedAskList);
        var plan = await kernel.CreateSequentialPlan(refinedAskList!, logger);
        var result = await kernel.RunAsync(variables, plan);

        #endregion


        logger.LogDebug("  RESULT: {result}", result.FunctionResults.First().GetValue<string>());
    }
    catch (SKException ex)
    {
        logger.LogError("FAILED with exception: {message}", ex.Message);
        continue;
    }
}
