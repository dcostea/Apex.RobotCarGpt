using Apex.RobotCarGpt.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Serilog;
using System.Diagnostics;
using System.Text.Json;

namespace Apex.RobotCarGpt.Controllers;

[ApiController]
[Route("[controller]")]
public class PromptsController(Kernel kernel) : ControllerBase
{
    private readonly List<string> asks = [
        //"Go like forward forward turn right backward stop.",
        //"Go 10 steps where each step is a randomly selected step like: move forward, backward, and turning left or right.",
        "You have a tree in front of the car. Avoid it.",
        //"Move forward, turn left, forward and return at the same place where it started.",
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
    ];

    [HttpGet("autoinvoked_function_calling")]
    public async Task<IActionResult> GetAutoinvokedFunctionCalling(bool isRefinedAsk = true, bool isAugmented = false)
    {
        var result = string.Empty;

        var sw = new Stopwatch();

        Log.Information("===========================================================================");
        Log.Information("AUTOINVOKED FUNCTION CALLING{isRefinedAsk}{isAugmentedAsk}", isRefinedAsk ? " WITH REFINED ASK" : "", isAugmented ? " WITH AUGMENTED ASK" : "");

        var variables = new KernelArguments()
        {
            ["commands"] = CommandExtensions.BasicCommands
        };

        foreach (var ask in asks)
        {
            sw.Restart();

            Log.Information("---------------------------------------------------------------------------");
            Log.Information("ASK: {ask}", ask);

            variables["input"] = ask;

            try
            {
                if (isRefinedAsk)
                {
                    var extractBasicCommandsPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractBasicCommands];
                    var refinedAskListResult = await kernel.InvokeAsync(extractBasicCommandsPromptFunction, variables);

                    var refinedAsk = refinedAskListResult.GetValue<string>();
                    Log.Information("REFINED ASK (list of basic commands): {ask}", refinedAsk);

                    variables["input"] = refinedAsk;
                }

                if (isAugmented)
                {
                    //TODO review this
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

                    variables["input"] = augmentedAsk;
                }

                Log.Warning("AutoInvokeKernelFunctions HAS A LIMIT OF MAX 5 CALLS!");

                var settings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                    Seed = 42L
                };

                var clonedKernel = kernel.Clone();
                clonedKernel.Plugins.Remove(kernel.Plugins[CommandExtensions.CommandsPlugin]);
                var streamingResult = clonedKernel.InvokePromptStreamingAsync(variables["input"]!.ToString()!, new KernelArguments(settings));
                await foreach (var streamingResponse in streamingResult)
                {
                    result += streamingResponse;
                }
                Log.Debug("STREAMING RESULT: {result}", result);
            }
            catch (Exception ex)
            {
                Log.Error("FAILED with exception: {message}", ex.Message);
                continue;
            }
            finally
            {
                sw.Stop();
                Log.Debug("Total seconds per ask: {seconds}", sw.Elapsed.TotalSeconds);
            }
        }

        return Ok(result);
    }

    [HttpGet("function_calling")]
    public async Task<IActionResult> GetFunctionCalling(bool isRefinedAsk = true, bool isAugmented = false)
    {
        if (isRefinedAsk && isAugmented)
        {
            return BadRequest("We cannot use refined ask and augmented ask at the same time, choose only one.");
        }

        var result = string.Empty;

        var sw = new Stopwatch();

        Log.Information("===========================================================================");
        Log.Information("FUNCTION CALLING{isRefinedAsk}{isAugmentedAsk}", isRefinedAsk ? " WITH REFINED ASK" : "", isAugmented ? " WITH AUGMENTED ASK" : "");

        var variables = new KernelArguments()
        {
            ["commands"] = CommandExtensions.BasicCommands
        };

        foreach (var ask in asks)
        {
            sw.Restart();

            Log.Information("---------------------------------------------------------------------------");
            Log.Information("ASK: {ask}", ask);

            variables["input"] = ask;

            try
            {
                if (isRefinedAsk) 
                {
                    var extractBasicCommandsPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractBasicCommands];
                    var refinedAskResult = await kernel.InvokeAsync(extractBasicCommandsPromptFunction, variables);
                    var refinedAsk = refinedAskResult.GetValue<string>();
                    Log.Information("REFINED ASK (list of basic commands): {ask}", refinedAsk);

                    variables["input"] = refinedAsk;
                }

                if (isAugmented)
                {
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

                    variables["input"] = augmentedAsk;
                }

                var chatHistory = new ChatHistory();
                chatHistory.AddMessage(AuthorRole.User, variables["input"]!.ToString()!);

                var settings = new OpenAIPromptExecutionSettings
                {
                    ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions,
                    Seed = 42L
                };

                var clonedKernel = kernel.Clone();
                clonedKernel.Plugins.Remove(kernel.Plugins[CommandExtensions.CommandsPlugin]);

                var chatCompletionService = clonedKernel.GetRequiredService<IChatCompletionService>();
                var chatMessageContent = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, clonedKernel);

                var functionCalls = ((OpenAIChatMessageContent)chatMessageContent).GetOpenAIFunctionToolCalls();
                foreach (var functionCall in functionCalls)
                {
                    clonedKernel.Plugins.TryGetFunctionAndArguments(functionCall, out var pluginFunction, out var arguments);
                    var functionResult = await clonedKernel.InvokeAsync(pluginFunction!, arguments!);
                    var jsonResponse = functionResult.GetValue<object>();
                    var json = JsonSerializer.Serialize(jsonResponse);
                    chatHistory.AddMessage(AuthorRole.Tool, json);
                }
                chatMessageContent = await chatCompletionService.GetChatMessageContentAsync(chatHistory, settings, clonedKernel);
                result = chatMessageContent.Content;
                Log.Debug("RESULT: {content}", result);
            }
            catch (Exception ex)
            {
                Log.Error("FAILED with exception: {message}", ex.Message);
                continue;
            }
            finally
            {
                sw.Stop();
                Log.Debug("Total seconds per ask: {seconds}", sw.Elapsed.TotalSeconds);
            }
        }

        return Ok(result);
    }

    [HttpGet("handlebars_planner")]
    public async Task<IActionResult> GetHandlebarsPlanner(bool isRefinedAsk = true, bool isAugmented = false)
    {
        if (isRefinedAsk && isAugmented)
        {
            return BadRequest("We cannot use refined ask and augmented ask at the same time, choose only one.");
        }

        var result = string.Empty;

        var sw = new Stopwatch();

        Log.Information("===========================================================================");
        Log.Information("HANDLEBARS PLANNER{isRefinedAsk}{isAugmentedAsk}", isRefinedAsk ? " WITH REFINED ASK" : "", isAugmented ? " WITH AUGMENTED ASK" : "");

        var variables = new KernelArguments()
        {
            ["commands"] = CommandExtensions.BasicCommands
        };

        foreach (var ask in asks)
        {
            sw.Restart();

            Log.Information("---------------------------------------------------------------------------");
            Log.Information("ASK: {ask}", ask);

            variables["input"] = ask;

            try
            {
                if (isRefinedAsk) 
                {
                    var extractBasicCommandsPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractBasicCommands];
                    var refinedAskResult = await kernel.InvokeAsync(extractBasicCommandsPromptFunction, variables);
                    var refinedAsk = refinedAskResult.GetValue<string>();
                    Log.Information("REFINED ASK (list of basic commands): {ask}", refinedAsk);

                    variables["input"] = refinedAsk;
                }

                if (isAugmented)
                {
                    var augmentedAsk = $"""
                    You get this action: {ask}

                    Extract the basic commands from the action and execute them.
                    """;

                    variables["input"] = augmentedAsk;
                }

                var handlebarsPlannerOptions = new HandlebarsPlannerOptions { AllowLoops = true };
                handlebarsPlannerOptions.ExcludedPlugins.Add(CommandExtensions.CommandsPlugin);
                var planner = new HandlebarsPlanner(handlebarsPlannerOptions);
                var plan = await planner.CreatePlanAsync(kernel, variables["input"]!.ToString()!);
                ////Log.Debug("  PLAN PROMPT: {prompt}", plan.Prompt);
                result = await plan.InvokeAsync(kernel, variables);
                Log.Information("  RESULT: {result}", result.Trim());
            }
            catch (Exception ex)
            {
                Log.Error("FAILED with exception: {message}", ex.Message);
                continue;
            }
            finally
            {
                sw.Stop();
                Log.Debug("Total seconds per ask: {seconds}", sw.Elapsed.TotalSeconds);
            }
        }

        return Ok(result);
    }

    [HttpGet("function_calling_planner")]
    public async Task<IActionResult> GetFunctionCallingPlanner(bool isRefinedAsk = true, bool isAugmented = false)
    {
        if (isRefinedAsk && isAugmented)
        {
            return BadRequest("We cannot use refined ask and augmented ask at the same time, choose only one.");
        }

        var result = string.Empty;

        var sw = new Stopwatch();

        Log.Information("===========================================================================");
        Log.Information("FUNCTION CALLING PLANNER{isRefinedAsk}{isAugmentedAsk}", isRefinedAsk ? " WITH REFINED ASK" : "", isAugmented ? " WITH AUGMENTED ASK" : "");

        var variables = new KernelArguments()
        {
            ["commands"] = CommandExtensions.BasicCommands
        };

        foreach (var ask in asks)
        {
            sw.Restart();

            Log.Information("---------------------------------------------------------------------------");
            Log.Information("ASK: {ask}", ask);

            variables["input"] = ask;

            try
            {
                if (isRefinedAsk)
                {
                    var extractBasicCommandsPromptFunction = kernel.Plugins[CommandExtensions.CommandsPlugin][CommandExtensions.ExtractBasicCommands];
                    var refinedAskResult = await kernel.InvokeAsync(extractBasicCommandsPromptFunction, variables);
                    var refinedAsk = refinedAskResult.GetValue<string>();
                    Log.Information("REFINED ASK (list of basic commands): {ask}", refinedAsk);

                    variables["input"] = refinedAsk;
                }

                if (isAugmented)
                {
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

                    variables["input"] = augmentedAsk;
                }

                var functionCallingStepwisePlannerOptions = new FunctionCallingStepwisePlannerOptions
                {
                    MaxIterations = 30,
                    ExecutionSettings = new OpenAIPromptExecutionSettings { Seed = 42L }
                };
                functionCallingStepwisePlannerOptions.ExcludedPlugins.Add(CommandExtensions.CommandsPlugin);
                var planner = new FunctionCallingStepwisePlanner(functionCallingStepwisePlannerOptions);
                var plannerResult = await planner.ExecuteAsync(kernel, variables["input"]!.ToString()!);

                //foreach (var item in result.ChatHistory!)
                //{
                //    Log.Debug("  CHAT: {item}", item.Content);
                //}

                result = plannerResult.FinalAnswer;
                Log.Debug("  ANSWER: {answer}", result);
            }
            catch (Exception ex)
            {
                Log.Error("FAILED with exception: {message}", ex.Message);
                continue;
            }
            finally
            {
                sw.Stop();
                Log.Debug("Total seconds per ask: {seconds}", sw.Elapsed.TotalSeconds);
            }
        }

        return Ok(result);
    }
}
