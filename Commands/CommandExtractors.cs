using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TemplateEngine.Basic;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.Extensions.Logging;

namespace Commands;

public static class CommandExtractors
{
    const string Plugins = "plugins";                       // plugins folder
    const string MotorPlugin = nameof(MotorPlugin);         // plugin name
    const string ExtractBasicMotorCommandsSemanticFunction = nameof(ExtractBasicMotorCommandsSemanticFunction);

    public async static Task<string?> ExtractBasicCommandsUsingInlineSemanticFunctionAsync(this IKernel kernel, string ask, string commands, ILogger logger)
    {
        const string ExtractBasicMotorPromptTemplate = """
You are a robot car capable of performing only the following allowed basic commands: {{ $commands }}.
Initial state of the car is stopped.
The last state of the car is stopped.
Your goal is "{{ $input }}".
Create a comma separated list of basic commands, as enumerated above, to fulfill the goal.
Remove any introduction, ending or explanation from the response, show me only the list of allowed commands.
""";

        var variables = new ContextVariables(ask);
        variables.Set("commands", commands);

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
                    new() { Name = "input", Description = "The car state." },
                    new() { Name = "commands", Description = "The commands to chose from." },
                ]
            }
        };

        // Render prompt for debugging purposes
        var renderedPromptTemplate = promptRenderer.Create(ExtractBasicMotorPromptTemplate, promptTemplateConfig);
        var renderedPrompt = await renderedPromptTemplate.RenderAsync(kernel.CreateNewContext(variables));
        logger.LogDebug("RENDERED PROMPT: {renderedPrompt}", renderedPrompt);

        if (!kernel.Functions.TryGetFunction(MotorPlugin, ExtractBasicMotorCommandsSemanticFunction, out var extractBasicMotorCommandsSemanticFunction))
        {
            extractBasicMotorCommandsSemanticFunction = kernel.CreateSemanticFunction(ExtractBasicMotorPromptTemplate, promptTemplateConfig);
        }

        var extractedBasicMotorCommands = await kernel.RunAsync(extractBasicMotorCommandsSemanticFunction, variables);

        return extractedBasicMotorCommands.FunctionResults.First().GetValue<string>();
    }

    public async static Task<string?> ExtractBasicCommandsUsingPluginSemanticFunctionAsync(this IKernel kernel, string ask, string commands)
    {
        var variables = new ContextVariables(ask);
        variables.Set("commands", commands);

        var pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), Plugins);

        if (!kernel.Functions.TryGetFunction(MotorPlugin, ExtractBasicMotorCommandsSemanticFunction, out var extractBasicMotorCommandsSemanticFunction))
        {
            var semanticMotorPluginFunctions = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, MotorPlugin);
            extractBasicMotorCommandsSemanticFunction = semanticMotorPluginFunctions[ExtractBasicMotorCommandsSemanticFunction];
        }

        var extractedBasicMotorCommands = await kernel.RunAsync(extractBasicMotorCommandsSemanticFunction, variables);

        return extractedBasicMotorCommands.FunctionResults.First().GetValue<string>();
    }
}
