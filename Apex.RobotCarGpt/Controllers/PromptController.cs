using Apex.RobotCarGpt.Helpers;
using Azure.AI.OpenAI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Handlebars;
using Serilog;
using System.Text.Json;

namespace Apex.RobotCarGpt.Controllers;

[ApiController]
[Route("[controller]")]
public class PromptsController(Kernel kernel) : ControllerBase
{
    private readonly List<string> _actions = [
        "You have a tree in front of the car. Avoid it and then resume the initial direction.",
        "Do the moonwalk dancing.",
        "Do a full 360 degrees rotation using 90 degrees turns.",
    ];
    private readonly string MotorHelperPlugin = nameof(MotorHelperPlugin);
    private readonly string BreakdownComplexCommands = nameof(BreakdownComplexCommands);
    private readonly string Plugins = nameof(Plugins);

    [HttpGet("/prompt/chaining")]
    public async Task<IActionResult> GetPromptChaining()
    {
        Console.WriteLine("\n===========================================================================");
        Console.WriteLine("PROMPT CHAINING");

        kernel.ImportPluginFromType<Plugins.MotorCommandsPlugin.MotorCommandsPlugin>();
        var motorHelperPlugin = kernel.CreatePluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), Plugins, MotorHelperPlugin), MotorHelperPlugin);
        kernel.PrintAllPluginsFunctions();

        foreach (var action in _actions)
        {
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine($"COMPLEX ACTION: {action}");

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are a robot car able to perform basic motor commands.");

            try
            {
                var refinedAction = action;
                //var refinedAction = "advance a few steps, turn back and stop";

                var breakdownComplexCommandsFunction = motorHelperPlugin["BreakdownComplexCommands"];
                var refinedActionResult = await kernel.InvokeAsync(breakdownComplexCommandsFunction);
                refinedAction = refinedActionResult.GetValue<string>();
                Console.WriteLine($"REFINED ACTION: Call the next commands: {refinedAction}");

                chatHistory.AddUserMessage($"Call the next commands: {refinedAction}");

                var x = await kernel.InvokePromptAsync(refinedAction!);

                var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();
                var chatMessageContent = await chatCompletionService.GetChatMessageContentAsync(chatHistory, new OpenAIPromptExecutionSettings { }, kernel);

                var result = chatMessageContent.Content;
                Console.WriteLine("RESULT: {content}", result);
            }
            catch (Exception ex)
            {
                Log.Error("FAILED with exception: {message}", ex.Message);
                continue;
            }
        }

        return Ok();
    }

    [HttpGet("/function_calling/autoinvoked")]
    public async Task<IActionResult> GetFunctionCallingAutoinvoked()
    {
        Console.WriteLine("\n===========================================================================");
        Console.WriteLine("FUNCTION CALLING WITH AUTOINVOKE");

        kernel.ImportPluginFromType<Plugins.MotorCommandsPlugin.MotorCommandsPlugin>();
        kernel.ImportPluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), Plugins, MotorHelperPlugin), MotorHelperPlugin);
        kernel.PrintAllPluginsFunctions();

        foreach (var action in _actions)
        {
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine($"COMPLEX ACTION: {action}");

            var chat = kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are a robot car able to perform basic motor commands.");
            chatHistory.AddUserMessage(action);

            try
            {
                Console.WriteLine("AutoInvokeKernelFunctions HAS A LIMIT OF MAX 5 CALLS!");

                // In order to capture the autoinvoked tools we need to stream the responses

                var streamingResult = chat.GetStreamingChatMessageContentsAsync(chatHistory, new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions }, kernel);

                await foreach (var result in streamingResult)
                {
                    var openaiMessageContent = result as OpenAIStreamingChatMessageContent;
                    var toolCall = openaiMessageContent?.ToolCallUpdate as StreamingFunctionToolCallUpdate;

                    if (openaiMessageContent!.Role == AuthorRole.Assistant)
                    {
                        if (toolCall is not null)
                        {
                            Console.Write($"\nTOOL: {toolCall.Name}");
                        }
                        else
                        {
                            Console.WriteLine();
                        }

                        continue;
                    }

                    if (openaiMessageContent?.FinishReason is not null)
                    {
                        Console.WriteLine($"\nFINISH REASON: {openaiMessageContent?.FinishReason}");
                        continue;
                    }

                    Console.Write($"{openaiMessageContent?.Content}");
                }
            }
            catch (Exception ex)
            {
                Log.Error("FAILED with exception: {message}", ex.Message);
                continue;
            }
        }

        return Ok();
    }

    [HttpGet("/function_calling")]
    public async Task<IActionResult> GetFunctionCalling()
    {
        Console.WriteLine("\n===========================================================================");
        Console.WriteLine("FUNCTION CALLING");

        kernel.ImportPluginFromType<Plugins.MotorCommandsPlugin.MotorCommandsPlugin>();
        kernel.ImportPluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), Plugins, MotorHelperPlugin), MotorHelperPlugin);
        kernel.PrintAllPluginsFunctions();

        foreach (var action in _actions)
        {
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine($"COMPLEX ACTION: {action}");

            var chat = kernel.GetRequiredService<IChatCompletionService>();

            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage("You are a robot car able to perform basic motor commands.");
            chatHistory.AddUserMessage(action);

            while (true)
            {
                var result = await chat.GetChatMessageContentAsync(chatHistory, new OpenAIPromptExecutionSettings { ToolCallBehavior = ToolCallBehavior.EnableKernelFunctions }, kernel);

                if (result.Content is not null)
                {
                    Console.WriteLine(result.Content);
                }

                var toolCalls = (result as OpenAIChatMessageContent)!.ToolCalls.OfType<ChatCompletionsFunctionToolCall>();

                if (toolCalls.Any())
                {
                    chatHistory.Add(result);

                    foreach (var toolCall in toolCalls)
                    {
                        string content;
                        if (kernel.Plugins.TryGetFunctionAndArguments(toolCall, out KernelFunction? function, out KernelArguments? arguments))
                        {
                            Console.WriteLine($"\nTOOL: {toolCall.Name}");

                            // invoke the function manually
                            var response = await function!.InvokeAsync(kernel, arguments);
                            content = JsonSerializer.Serialize(response.GetValue<string>());
                        }
                        else
                        {
                            // instruct the model to try again
                            content = "Unable to find the function. Please try again!";
                            Console.WriteLine(content);
                        }

                        chatHistory.Add(new ChatMessageContent(
                            AuthorRole.Tool,
                            content,
                            metadata: new Dictionary<string, object?>(1) { { OpenAIChatMessageContent.ToolIdProperty, toolCall.Id } }));
                    }
                }
                else
                {
                    // stop if no more tools found
                    break;
                }
            }
        }

        return Ok();
    }

    [HttpGet("/planner/function_calling")]
    public async Task<IActionResult> GetFunctionCallingStepwisePlanner(bool showChatHistory = false)
    {
        Console.WriteLine("\n===========================================================================");
        Console.WriteLine("FUNCTION CALLING STEPWISE PLANNER");

        kernel.ImportPluginFromType<Plugins.MotorCommandsPlugin.MotorCommandsPlugin>();
        kernel.ImportPluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), Plugins, MotorHelperPlugin), MotorHelperPlugin);
        kernel.PrintAllPluginsFunctions();

        foreach (var action in _actions)
        {
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine($"COMPLEX ACTION: {action}");

            try
            {
                var planner = new FunctionCallingStepwisePlanner(new FunctionCallingStepwisePlannerOptions { MaxIterations = 10 });

                var chatHistory = new ChatHistory();
                chatHistory.AddSystemMessage("You are a robot car able to perform basic motor commands.");
                //If you are asked to perform complex actions, break down the complex action into basic motor commands.

                var plannerResult = await planner.ExecuteAsync(kernel, action);

                if (showChatHistory) 
                {
                    Console.WriteLine("\nCHAT HISTORY: ");

                    foreach (var chat in plannerResult.ChatHistory!)
                    {
                        var toolCalls = (chat as OpenAIChatMessageContent)?.ToolCalls.OfType<ChatCompletionsFunctionToolCall>();

                        if (chat.Role == AuthorRole.Assistant)
                        {
                            if (toolCalls is not null)
                            {
                                foreach (var toolCall in toolCalls)
                                {
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    Console.Write($"{chat.Role.Label} > ");
                                    Console.ResetColor();
                                    Console.WriteLine(toolCall.Name);
                                }
                            }
                            continue;
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write($"{chat.Role.Label} > ");
                        Console.ResetColor();
                        Console.WriteLine(chat.Content);
                    }
                }

                Console.WriteLine($"\nFINAL RESULT: {plannerResult.FinalAnswer}");
            }
            catch (Exception ex)
            {
                Log.Error("FAILED with exception: {message}", ex.Message);
                continue;
            }
        }

        return Ok();
    }

    [HttpGet("/planner/handlebars")]
    public async Task<IActionResult> GetHandlebarsPlanner(bool showPlan = false)
    {
        Console.WriteLine("\n===========================================================================");
        Console.WriteLine("HANDLEBARS PLANNER");

        kernel.ImportPluginFromType<Plugins.MotorCommandsPlugin.MotorCommandsPlugin>();
        kernel.ImportPluginFromPromptDirectory(Path.Combine(Directory.GetCurrentDirectory(), Plugins, MotorHelperPlugin), MotorHelperPlugin);
        kernel.PrintAllPluginsFunctions();

        foreach (var action in _actions)
        {
            Console.WriteLine("---------------------------------------------------------------------------");
            Console.WriteLine($"COMPLEX ACTION: {action}");

            try
            {
                var handlebarsPlannerOptions = new HandlebarsPlannerOptions { AllowLoops = true };
                var planner = new HandlebarsPlanner(handlebarsPlannerOptions);
                var plan = await planner.CreatePlanAsync(kernel, action);
                plan.Prompt = $"""
                    <system~>## Instructions
                    You are a robot car able to perform basic motor commands.
                    
                    {plan.Prompt}
                    """;

                if (showPlan)
                {
                    Console.WriteLine($"\nPLAN: {plan.Prompt}");
                }

                var result = await plan.InvokeAsync(kernel);
                Console.WriteLine($"\nRESULT: {result.Trim()}");
            }
            catch (Exception ex)
            {
                Log.Error("FAILED with exception: {message}", ex.Message);
                continue;
            }
        }

        return Ok();
    }
}
