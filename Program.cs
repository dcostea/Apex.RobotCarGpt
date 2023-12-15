using Commands;
using HandlebarsDotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Plugins;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    //.MinimumLevel.Information()
    //.MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Debug)
    .MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

var builder = Kernel.CreateBuilder();

builder.Services.AddLogging(c => c.AddSerilog(Log.Logger));

builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!,
    modelId: Env.Var("AzureOpenAI:TextCompletionModelId")!,
    endpoint: Env.Var("AzureOpenAI:Endpoint")!,
    serviceId: Env.Var("AzureOpenAI:AzureOpenAIChat")!,
    apiKey: Env.Var("AzureOpenAI:ApiKey")!);

var motorPlugin = new MotorPlugin();
builder.Plugins.AddFromObject(motorPlugin, nameof(Plugins.MotorPlugin));
builder.Plugins.AddFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), CommandExtensions.PluginsFolder, CommandExtensions.CommandsPlugin), CommandExtensions.CommandsPlugin);

var kernel = builder.Build();


// 1. LOAD native and semantic functions from MotorPlugin

// CREATE semantic function inline (instead of importing it from MotorPlugin)
////var extractBasicCommandsSemanticFunction = kernel.CreateFunctionFromPrompt(new KernelPromptTemplate(CommandExtensions.ExtractBasicCommandsPromptTemplateConfig), CommandExtensions.ExtractBasicCommandsPromptTemplateConfig, CommandExtensions.ExtractBasicCommands);
////var extractMostRelevantBasicCommandSemanticFunction = kernel.CreateFunctionFromPrompt(new KernelPromptTemplate(CommandExtensions.ExtractMostRelevantBasicCommandPromptTemplateConfig), CommandExtensions.ExtractMostRelevantBasicCommandPromptTemplateConfig, CommandExtensions.ExtractMostRelevantBasicCommand);
////var executeBasicCommandSemanticFunction = kernel.CreateFunctionFromPrompt(new KernelPromptTemplate(CommandExtensions.ExecuteBasicCommandPromptTemplateConfig), CommandExtensions.ExecuteBasicCommandPromptTemplateConfig, CommandExtensions.ExecuteBasicCommand);

// IMPORT semantic functions from MotorPlugin
////var semanticMotorPluginFunctions = kernel.ImportPluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), CommandExtensions.PluginsFolder), CommandExtensions.CommandsPlugin);
var extractBasicCommandsSemanticFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractBasicCommands];
var extractMostRelevantBasicCommandSemanticFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractMostRelevantBasicCommand];
var executeBasicCommandSemanticFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExecuteBasicCommand];

////var motorPluginFunctions = kernel.ImportPluginFromType<Plugins.MotorPlugin>();


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
var promptRenderer = new KernelPromptTemplateFactory();
var extractBasicCommandsRenderedPromptTemplate = promptRenderer.Create(CommandExtensions.ExtractBasicCommandsPromptTemplateConfig);
var extractMostRelevantBasicCommandRenderedPromptTemplate = promptRenderer.Create(CommandExtensions.ExtractMostRelevantBasicCommandPromptTemplateConfig);


// 2. PREPARE CONTEXT VARIABLES

var variables = new KernelArguments()
{
    ["commands"] = CommandExtensions.BasicCommands
};

foreach (var ask in asks)
{
    Log.Information("----------------------------------------------------------------------------------------------------");
    Log.Information("ASK: {ask}", ask);

    variables["input"] = ask;

    // Rendered prompts
    var extractBasicCommandsRenderedPrompt = await extractBasicCommandsRenderedPromptTemplate.RenderAsync(kernel, variables);
    Log.Debug("RENDERED BASIC COMMANDS PROMPT: {renderedPrompt}", extractBasicCommandsRenderedPrompt);
    var extractMostRelevantBasicCommandRenderedPrompt = await extractMostRelevantBasicCommandRenderedPromptTemplate.RenderAsync(kernel, variables);
    Log.Debug("RENDERED MOST RELEVANT BASIC COMMAND PROMPT: {renderedPrompt}", extractMostRelevantBasicCommandRenderedPrompt);

    try
    {
        #region Native Function.

        //FunctionResult? result = kernel.Plugins[nameof(MotorPlugin)].TryGetFunction("Forward", out var func)
        //    ? (await kernel.InvokeAsync(func, variables))
        //    : default;
        //Log.Debug("  RESULT: {result}", result);

        #endregion


        #region Semantic Function calling Native Function.

        ////var result = await kernel.InvokeAsync(executeBasicCommandSemanticFunction, variables);
        ////Log.Debug("  RESULT: {result}", result.GetValue<string>());

        #endregion


        #region Sequential Plan => Multiple steps.

        ////#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        ////        var handlebarsPlannerConfig = new HandlebarsPlannerConfig()
        ////        {
        ////            // Change this if you want to test with loops regardless of model selection.
        ////            AllowLoops = true,
        ////        };
        ////        handlebarsPlannerConfig.ExcludedPlugins.Add(CommandExtensions.CommandsPlugin);
        ////        var planner = new HandlebarsPlanner(handlebarsPlannerConfig);

        ////        var plan = await planner.CreatePlanAsync(kernel, ask!);
        ////        var result = plan.Invoke(kernel, variables);
        ////        Log.Debug("  RESULT: {result}", result.Trim());

        ////#pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        #endregion


        #region Sequential Plan ('ask' is converted to list of basic command) => Multiple steps.

        var refinedAskListResult = await kernel.InvokeAsync(extractBasicCommandsSemanticFunction, variables);
        var refinedAskList = refinedAskListResult.GetValue<string>();
        Log.Information("REFINED ASK (list): {ask}", refinedAskList);

#pragma warning disable SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
        var handlebarsPlannerOptions = new HandlebarsPlannerOptions ()
        {
            // Change this if you want to test with loops regardless of model selection.
            AllowLoops = true,
        };
        handlebarsPlannerOptions.ExcludedPlugins.Add(CommandExtensions.CommandsPlugin);
        var planner = new HandlebarsPlanner(handlebarsPlannerOptions);
        var plan = await planner.CreatePlanAsync(kernel, refinedAskList!);
        //Log.Debug("  PLAN PROMPT: {prompt}", plan.Prompt);
        var result = await plan.InvokeAsync(kernel, variables);
        Log.Debug("  RESULT: {result}", result.Trim());

#pragma warning restore SKEXP0060 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.

        #endregion
    }
    catch (HandlebarsRuntimeException hex) 
    {
        Log.Error("HANDLEBAR PLAN FAILED with exception: {message}", hex.Message);
        continue;
    }
    catch (KernelException ex)
    {
        Log.Error("FAILED with exception: {message}", ex.Message);
        continue;
    }
}
