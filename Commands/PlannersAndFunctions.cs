using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel;
using Serilog;
using System.Text.Json;

namespace Commands;

public static class PlannersAndFunctions
{

    public static async Task AutoInvokeKernelFunctionsAsync(Kernel kernel, string ask)
    {
        Log.Information("KERNEL FUNCTIONS (WITH AUTOINVOKE)");

        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Seed = 42L
        };

        var streamingResult = kernel.InvokePromptStreamingAsync(ask, new KernelArguments(settings));
        await foreach (var streamingResponse in streamingResult)
        {
            Log.Debug("STREAMING: {streamingResponse}", streamingResponse);
        }
    }

    public static async Task AutoInvokeKernelFunctionsWithRefinedAskAsync(Kernel kernel, KernelArguments variables)
    {
        Log.Information("KERNEL FUNCTIONS (WITH AUTOINVOKE) WITH REFINED ASK (EXTRA PROMPT)");

        var extractBasicCommandsPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractBasicCommands];
        var refinedAskListResult = await kernel.InvokeAsync(extractBasicCommandsPromptFunction, variables);
        var refinedAskList = refinedAskListResult.GetValue<string>();
        Log.Information("REFINED ASK (list of basic commands): {ask}", refinedAskList);

        Log.Warning("AutoInvokeKernelFunctions HAS A LIMIT OF MAX 5 CALLS!", refinedAskList);

        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
            Seed = 42L
        };

        var clonedKernel = kernel.Clone();
        clonedKernel.Plugins.Remove(kernel.Plugins[CommandExtensions.CommandsPlugin]);
        var streamingResult = clonedKernel.InvokePromptStreamingAsync(refinedAskList!, new KernelArguments(settings));
        await foreach (var streamingResponse in streamingResult)
        {
            Log.Debug("STREAMING: {streamingResponse}", streamingResponse);
        }
    }

    public static async Task KernelFunctionsWithRefinedAskAsync(Kernel kernel, KernelArguments variables)
    {
        Log.Information("KERNEL FUNCTIONS WITH REFINED ASK (EXTRA PROMPT)");

        var extractBasicCommandsPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractBasicCommands];
        var refinedAskListResult = await kernel.InvokeAsync(extractBasicCommandsPromptFunction, variables);
        var refinedAskList = refinedAskListResult.GetValue<string>();
        Log.Information("REFINED ASK (list of basic commands): {ask}", refinedAskList);

        var chatHistory = new ChatHistory();
        chatHistory.AddMessage(AuthorRole.User, refinedAskList!);
        var settings = new OpenAIPromptExecutionSettings
        {
            ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions,
            Seed = 42L
        };

        var clonedKernel = kernel.Clone();
        clonedKernel.Plugins.Remove(kernel.Plugins[CommandExtensions.CommandsPlugin]);

        var chatCompletionService = clonedKernel.GetRequiredService<IChatCompletionService>();
        var result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, clonedKernel);

        var functionCalls = ((OpenAIChatMessageContent)result).GetOpenAIFunctionToolCalls();
        foreach (var functionCall in functionCalls)
        {
            clonedKernel.Plugins.TryGetFunctionAndArguments(functionCall, out var pluginFunction, out var arguments);
            var functionResult = await clonedKernel.InvokeAsync(pluginFunction!, arguments!);
            var jsonResponse = functionResult.GetValue<object>();
            var json = JsonSerializer.Serialize(jsonResponse);
            chatHistory.AddMessage(AuthorRole.Tool, json);
        }
        result = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, clonedKernel);
        Log.Debug("RESULT: {content}", result.Content);
    }

    public static async Task HandlebarsPlannerAsync(Kernel kernel, KernelArguments variables, string ask)
    {
        Log.Information("HANDLEBARS PLANNER WITH PLAIN ASK");

        var handlebarsPlannerOptions = new HandlebarsPlannerOptions { AllowLoops = true };
        handlebarsPlannerOptions.ExcludedPlugins.Add(CommandExtensions.CommandsPlugin);
        var planner = new HandlebarsPlanner(handlebarsPlannerOptions);
        var plan = await planner.CreatePlanAsync(kernel, ask!);
        Log.Debug("  PLAN PROMPT: {prompt}", plan.Prompt);
        var result = await plan.InvokeAsync(kernel, variables);
        Log.Information("  RESULT: {result}", result.Trim());
    }

    public static async Task HandlebarsPlannerWithRefinedAskAsync(Kernel kernel, KernelArguments variables)
    {
        Log.Information("HANDLEBARS PLANNER WITH REFINED ASK (EXTRA PROMPT)");

        var extractBasicCommandsPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractBasicCommands];
        var refinedAskListResult = await kernel.InvokeAsync(extractBasicCommandsPromptFunction, variables);
        var refinedAskList = refinedAskListResult.GetValue<string>();
        Log.Information("REFINED ASK (list of basic commands): {ask}", refinedAskList);

        var handlebarsPlannerOptions = new HandlebarsPlannerOptions { AllowLoops = true };
        handlebarsPlannerOptions.ExcludedPlugins.Add(CommandExtensions.CommandsPlugin);
        var planner = new HandlebarsPlanner(handlebarsPlannerOptions);
        var plan = await planner.CreatePlanAsync(kernel, refinedAskList!);
        ////Log.Debug("  PLAN PROMPT: {prompt}", plan.Prompt);
        var result = await plan.InvokeAsync(kernel, variables);
        Log.Information("  RESULT: {result}", result.Trim());
    }

    public static async Task HandlebarsPlannerWithAugmentedAskAsync(Kernel kernel, KernelArguments variables, string ask)
    {
        Log.Information("Handlebars Planner with augmented ask (same prompt)");

        var augmentedAsk = $"""
        You are a robot car capable of performing basic commands.
        Initial state of the car is stopped.
        The last state of the car is stopped.
        Action:
        [START ACTION]
        {ask}
        [END ACTION]
        Extract the list of basic commands from the action and execute the commands.
        """;

        var handlebarsPlannerOptions = new HandlebarsPlannerOptions()
        {
            AllowLoops = true
        };
        handlebarsPlannerOptions.ExcludedFunctions.Add(CommandExtensions.ExecuteBasicCommand);
        handlebarsPlannerOptions.ExcludedFunctions.Add(CommandExtensions.ExtractMostRelevantBasicCommand);
        var planner = new HandlebarsPlanner(handlebarsPlannerOptions);

        var plan = await planner.CreatePlanAsync(kernel, augmentedAsk!);
        //Log.Debug("  PLAN PROMPT: {prompt}", plan.Prompt);
        var result = await plan.InvokeAsync(kernel, variables);
        Log.Information("  RESULT: {result}", result.Trim());
    }

    public static async Task FunctionCallingPlannerWithRefinedAskAsync(Kernel kernel, KernelArguments variables)
    {
        Log.Information("FunctionCalling Planner with refined ask (extra prompt)");

        var extractBasicCommandsPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractBasicCommands];
        var refinedAskListResult = await kernel.InvokeAsync(extractBasicCommandsPromptFunction, variables);
        var refinedAskList = refinedAskListResult.GetValue<string>();
        Log.Information("REFINED ASK (list of basic commands): {ask}", refinedAskList);

        var functionCallingStepwisePlannerConfig = new FunctionCallingStepwisePlannerConfig
        {
            MaxIterations = 30,
            ExecutionSettings = new OpenAIPromptExecutionSettings { Seed = 42L }
        };
        functionCallingStepwisePlannerConfig.ExcludedPlugins.Add(CommandExtensions.CommandsPlugin);
        var planner = new FunctionCallingStepwisePlanner(functionCallingStepwisePlannerConfig);
        var result = await planner.ExecuteAsync(kernel, refinedAskList!);
        Log.Debug("  ANSWER: {answer}", result.FinalAnswer);
    }

    public static async Task FunctionCallingPlannerWithAugmentedAskAsync(Kernel kernel, KernelArguments variables, string ask)
    {
        Log.Information("FunctionCalling Planner with augmented ask (same prompt)");

        var augmentedAsk = $"""
        You are a robot car capable of performing basic commands.
        Initial state of the car is stopped.
        The last state of the car is stopped.
        Action:
        [START ACTION]
        {ask}
        [END ACTION]
        Extract the list of basic commands from the action and show me the list. Execute the commands.
        """;

        var functionCallingStepwisePlannerConfig = new FunctionCallingStepwisePlannerConfig
        {
            MaxIterations = 20,
            ExecutionSettings = new OpenAIPromptExecutionSettings { Seed = 42L }
        };
        functionCallingStepwisePlannerConfig.ExcludedFunctions.Add(CommandExtensions.ExecuteBasicCommand);
        functionCallingStepwisePlannerConfig.ExcludedFunctions.Add(CommandExtensions.ExtractMostRelevantBasicCommand);
        var planner = new FunctionCallingStepwisePlanner(functionCallingStepwisePlannerConfig);

        var result = await planner.ExecuteAsync(kernel, augmentedAsk!);
        Log.Debug("  ANSWER: {answer}", result.FinalAnswer);
    }
}
