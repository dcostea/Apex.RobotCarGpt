using Commands;
using HandlebarsDotNet;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Serilog;
using System.Diagnostics;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    //.MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Debug)
    //.MinimumLevel.Override("Microsoft.SemanticKernel", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .CreateLogger();

var sw = new Stopwatch();

var builder = Kernel.CreateBuilder();

builder.Services.AddLogging(c => c.AddSerilog(Log.Logger));

builder.Services.AddAzureOpenAIChatCompletion(
    deploymentName: Env.Var("AzureOpenAI:ChatCompletionDeploymentName")!,
    modelId: Env.Var("AzureOpenAI:TextCompletionModelId")!,
    endpoint: Env.Var("AzureOpenAI:Endpoint")!,
    serviceId: Env.Var("AzureOpenAI:AzureOpenAIChat")!,
    apiKey: Env.Var("AzureOpenAI:ApiKey")!);


// 1a. LOAD plugins with kernel builder

builder.Plugins.AddFromType<Plugins.MotorPlugin>();
builder.Plugins.AddFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), CommandExtensions.PluginsFolder, CommandExtensions.CommandsPlugin), CommandExtensions.CommandsPlugin);

var kernel = builder.Build();


// 1b. LOAD prompt functions from CommandsPlugin

// CREATE prompt functions inline (instead of importing them from CommandsPlugin)
////var extractBasicCommandsPromptFunction = kernel.CreateFunctionFromPrompt(CommandExtensions.ExtractBasicCommandsPromptTemplate, new OpenAIPromptExecutionSettings { MaxTokens = 500, Temperature = 0.0 }, CommandExtensions.ExtractBasicCommands);
////var extractMostRelevantBasicCommandPromptFunction = kernel.CreateFunctionFromPrompt(CommandExtensions.ExtractBasicCommandsPromptTemplate, new OpenAIPromptExecutionSettings { MaxTokens = 500, Temperature = 0.0 }, CommandExtensions.ExtractMostRelevantBasicCommand);
////var executeBasicCommandPromptFunction = kernel.CreateFunctionFromPrompt(CommandExtensions.ExtractBasicCommandsPromptTemplate, new OpenAIPromptExecutionSettings { MaxTokens = 500, Temperature = 0.0 }, CommandExtensions.ExecuteBasicCommand);

// IMPORT prompt functions from CommandsPlugin
////var promptMotorPluginFunctions = kernel.ImportPluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), CommandExtensions.PluginsFolder, CommandExtensions.CommandsPlugin), CommandExtensions.CommandsPlugin);
////var extractBasicCommandsPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractBasicCommands];
////var extractMostRelevantBasicCommandPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractMostRelevantBasicCommand];
////var executeBasicCommandPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExecuteBasicCommand];


// 1c. LOAD native functions from MotorPlugin

////var motorPluginFunctions = kernel.CreatePluginFromType<Plugins.MotorPlugin>();


var asks = new List<string>
{
  //"Go like forward forward turn right backward stop.",
  //"Go 10 steps where each step is a randomly selected step like: move forward, backward, and turning left or right.",
  "You have a tree in front of the car. Avoid it.",
  "You have a tree in front of the car. Avoid it.",
  "You have a tree in front of the car. Avoid it.",
  "You have a tree in front of the car. Avoid it.",
  "You have a tree in front of the car. Avoid it.",
  //"Move forward, turn left, forward and return in the same place where it started.",
  //"Do a full circle by turning left followed by a full circle by turning right.",
  //"Run away.",
  //"Do an evasive maneuver.",
  //"Do a pretty complex evasive maneuver with a least 15 steps. Stop at every 5 steps.",
  //"Do the moonwalk dancing.",
  //"Move like a jellyfish.",
  //"Dance like a ballerina.",
  //"Go on square path.",
  //"Go on a full complete circle.",
  //"Go on a semi-circle.",
  //"Do a full 360 degrees rotation.",
};

// Create prompt renderers
////var promptRenderer = new KernelPromptTemplateFactory();
////var extractBasicCommandsRenderedPromptTemplate = promptRenderer.Create(CommandExtensions.ExtractBasicCommandsPromptTemplateConfig);
////var extractMostRelevantBasicCommandRenderedPromptTemplate = promptRenderer.Create(CommandExtensions.ExtractMostRelevantBasicCommandPromptTemplateConfig);


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
    ////var extractBasicCommandsRenderedPrompt = await extractBasicCommandsRenderedPromptTemplate.RenderAsync(kernel, variables);
    ////Log.Debug("RENDERED BASIC COMMANDS PROMPT: {renderedPrompt}", extractBasicCommandsRenderedPrompt);
    ////var extractMostRelevantBasicCommandRenderedPrompt = await extractMostRelevantBasicCommandRenderedPromptTemplate.RenderAsync(kernel, variables);
    ////Log.Debug("RENDERED MOST RELEVANT BASIC COMMAND PROMPT: {renderedPrompt}", extractMostRelevantBasicCommandRenderedPrompt);

    try
    {
        sw.Restart();

        // 3a. USING LLM FUNCTIONS
        ////await PlannersAndFunctions.AutoInvokeKernelFunctionsAsync(kernel, ask);
        //await PlannersAndFunctions.AutoInvokeKernelFunctionsWithRefinedAskAsync(kernel, variables);
        //await PlannersAndFunctions.KernelFunctionsWithRefinedAskAsync(kernel, variables);

        // 3b. USING PLANNERS
        ////await PlannersAndFunctions.HandlebarsPlannerAsync(kernel, variables, ask);
        //await PlannersAndFunctions.HandlebarsPlannerWithRefinedAskAsync(kernel, variables);
        //await PlannersAndFunctions.HandlebarsPlannerWithAugmentedAskAsync(kernel, variables, ask);
        await PlannersAndFunctions.FunctionCallingPlannerWithRefinedAskAsync(kernel, variables);
        //await PlannersAndFunctions.FunctionCallingPlannerWithAugmentedAskAsync(kernel, variables, ask);
    }
    catch (HandlebarsCompilerException ex)
    {
        //Log.Error("HANDLEBAR COMPILER PLAN FAILED with exception: {message}", ex.Message);
        continue;
    }
    catch (HandlebarsRuntimeException ex)
    {
        //Log.Error("HANDLEBAR RUNTIME PLAN FAILED with exception: {message}", ex.Message);
        continue;
    }
    catch (KernelException ex) when (
        ex.Message.Contains(nameof(HandlebarsPlannerErrorCodes.InsufficientFunctionsForGoal), StringComparison.CurrentCultureIgnoreCase) ||
        ex.Message.Contains(nameof(HandlebarsPlannerErrorCodes.HallucinatedHelpers), StringComparison.CurrentCultureIgnoreCase))
    {
        //Log.Error("KERNEL FAILED with exception: {message}", ex.Message);
        continue;
    }
    catch (Exception ex)
    {
        //Log.Error("FAILED with exception: {message}", ex.Message);
        continue;
    }
    finally
    {
        sw.Stop();
        Log.Debug("Total seconds per ask: {seconds}", sw.Elapsed.TotalSeconds);
    }
}
