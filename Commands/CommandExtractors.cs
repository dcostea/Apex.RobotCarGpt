using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TemplateEngine.Basic;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.Extensions.Logging;

namespace Commands;

public static class CommandExtractors
{
    const string Plugins = nameof(Plugins);
    const string MotorPlugin = nameof(MotorPlugin);
    const string ExtractBasicMotorCommands = nameof(ExtractBasicMotorCommands);

    public async static Task<string?> ExtractBasicCommandsUsingInlineSemanticFunctionAsync(this IKernel kernel, string ask, string commands, ILogger logger)
    {
        const string ExtractBasicMotorPromptTemplate = """
You are a minimalistic car capable of performing some basic commands like {{$commands}}.
Initial state of the car is stopped.
Take this goal "{{$input}}" and create a list of basic commands, as enumerated above, to fulfill the goal. 
""";

        // render prompt for debugging purposes
        var promptRenderer = new BasicPromptTemplateFactory();
        var promptTemplateConfig = new PromptTemplateConfig 
        {
            Input = new PromptTemplateConfig.InputConfig
            {
                Parameters = new List<PromptTemplateConfig.InputParameter>
                {
                    new()
                    {
                        Name = "input",
                        Description = "The car action."
                    },
                    new()
                    {
                        Name = "commands",
                        Description = "The commands to chose from."
                    },
                }
            }   
        };

        var variables = new ContextVariables(ask);
        variables.Set("commands", commands);

        var renderedPromptTemplate = promptRenderer.Create(ExtractBasicMotorPromptTemplate, promptTemplateConfig);
        var renderedPrompt = await renderedPromptTemplate.RenderAsync(kernel.CreateNewContext(variables));
        logger.LogDebug("[START RENDERED PROMPT]\n{renderedPrompt}\n[END RENDERED PROMPT]", renderedPrompt);

        var openAIRequestSettings = new OpenAIRequestSettings
        {
            MaxTokens = 500,
            Temperature = 0.0,
            TopP = 0.0,
            PresencePenalty = 0.0,
            FrequencyPenalty = 0.0,
        };

        if (!kernel.Functions.TryGetFunction(MotorPlugin, ExtractBasicMotorCommands, out var extractBasicMotorCommandsFunction))
        {
            extractBasicMotorCommandsFunction = kernel.CreateSemanticFunction(ExtractBasicMotorPromptTemplate, requestSettings: openAIRequestSettings);
        }

        var extractedBasicMotorCommands = await kernel.RunAsync(extractBasicMotorCommandsFunction, variables);

        return extractedBasicMotorCommands.FunctionResults.First().GetValue<string>();
    }

    public async static Task<string?> ExtractBasicCommandsUsingRegisteredSemanticFunctionAsync(this IKernel kernel, string ask, string commands)
    {
        const string ExtractBasicMotorPromptTemplate = """
You are a minimalistic car capable of performing some basic commands like {{$commands}}.
Initial state of the car is stopped.
Take this goal "{{$input}}" and create a list of basic commands, as enumerated above, to fulfill the goal. 
""";

        var promptConfig = new PromptTemplateConfig
        {
            Description = "Create a list of basic commands.",
            ModelSettings = new List<AIRequestSettings>
            {
                new OpenAIRequestSettings
                {
                    MaxTokens = 500,
                    Temperature = 0.0,
                    TopP = 0.0,
                    PresencePenalty = 0.0,
                    FrequencyPenalty = 0.0
                }
            },
            Input = new PromptTemplateConfig.InputConfig
            {
                Parameters = new List<PromptTemplateConfig.InputParameter>
                {
                    new() {
                        Name = "input",
                        Description = "The car action."
                    },
                    new() {
                        Name = "commands",
                        Description = "The commands to chose from."
                    },
                }
            }
        };

        var variables = new ContextVariables(ask);
        variables.Set("commands", commands);
        
        if (!kernel.Functions.TryGetFunction(MotorPlugin, ExtractBasicMotorCommands, out var extractBasicMotorCommandsFunction))
        {
            extractBasicMotorCommandsFunction = kernel.CreateSemanticFunction(ExtractBasicMotorPromptTemplate, promptConfig, ExtractBasicMotorCommands, MotorPlugin);
        }

        var extractedBasicMotorCommands = await kernel.RunAsync(extractBasicMotorCommandsFunction, variables);

        return extractedBasicMotorCommands.FunctionResults.First().GetValue<string>();
    }

    public async static Task<string?> ExtractBasicCommandsUsingPluginSemanticFunctionAsync(this IKernel kernel, string ask, string commands)
    {
        var variables = new ContextVariables(ask);
        variables.Set("commands", commands);

        // import semantic functions from MotorPlugin
        var pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), Plugins);

        if (!kernel.Functions.TryGetFunction(MotorPlugin, ExtractBasicMotorCommands, out var extractBasicMotorCommandsFunction))
        {
            var semanticMotorPluginFunctions = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, MotorPlugin);
            extractBasicMotorCommandsFunction = semanticMotorPluginFunctions[ExtractBasicMotorCommands];
        }

        var extractedBasicMotorCommands = await kernel.RunAsync(extractBasicMotorCommandsFunction, variables);

        return extractedBasicMotorCommands.FunctionResults.First().GetValue<string>();
    }
}
