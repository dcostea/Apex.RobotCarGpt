using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TemplateEngine.Basic;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel.AI;

namespace Commands;

public static class CommandExtractors
{
    const string Plugins = nameof(Plugins);
    const string MotorPlugin = nameof(MotorPlugin);
    const string ExtractBasicCommands = nameof(ExtractBasicCommands);

    public async static Task<string?> ExtractCommandsUsingInlineSemanticFunctionAsync(this IKernel kernel, string ask)
    {
        const string ExtractMotorPromptTemplate = """
You are a minimalistic car capable of performing some basic commands like {{$commands}}.
Initial state of the car is stopped.
Take this goal "{{$input}}" and create a list of basic commands, as enumerated above, to fulfill the goal. 
""";

        var variables = new ContextVariables(ask);
        variables.Set("commands", "go forward, go backward, turn left, turn right, and stop");

        // render prompt for debugging purposes
        Console.WriteLine("--- Rendered Prompt");
        var promptRenderer = new BasicPromptTemplateEngine();
        var renderedPrompt = await promptRenderer.RenderAsync(ExtractMotorPromptTemplate, kernel.CreateNewContext(variables));
        Console.WriteLine(renderedPrompt);

        var openAIRequestSettings = new OpenAIRequestSettings
        {
            MaxTokens = 500,
            Temperature = 0.0,
            TopP = 0.0,
            PresencePenalty = 0.0,
            FrequencyPenalty = 0.0,
        };
        var extractMotorCommandsFunction = kernel.CreateSemanticFunction(ExtractMotorPromptTemplate, requestSettings: openAIRequestSettings);

        var extractedMotorCommands = await kernel.RunAsync(extractMotorCommandsFunction, variables);

        return extractedMotorCommands.FunctionResults.First().GetValue<string>();
    }

    public async static Task<string?> ExtractCommandsUsingRegisteredSemanticFunctionAsync(this IKernel kernel, string ask)
    {
        const string ExtractMotorPromptTemplate = """
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
                        Description = "The car action.",
                        DefaultValue = ask
                    },
                    new() {
                        Name = "commands",
                        Description = "The commands to chose from.",
                        DefaultValue = "go forward, go backward, turn left, turn right, and stop"
                    },
                }
            }
        };

        var extractMotorCommandsFunction = kernel.CreateSemanticFunction(ExtractMotorPromptTemplate, promptConfig, "ExtractMotorCommands", "MotorPlugin");

        var extractedMotorCommands = await kernel.RunAsync(extractMotorCommandsFunction);

        return extractedMotorCommands.FunctionResults.First().GetValue<string>();
    }

    public async static Task<string?> ExtractCommandsUsingPluginSemanticFunctionAsync(this IKernel kernel, string ask)
    {
        var variables = new ContextVariables(ask);
        variables.Set("commands", "go forward, go backward, turn left, turn right, and stop");

        // import semantic functions from MotorPlugin
        var pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), Plugins);
        var semanticMotorPlugin = kernel.ImportSemanticFunctionsFromDirectory(pluginsDirectory, MotorPlugin);
        var extractedMotorCommands = await kernel.RunAsync(variables, semanticMotorPlugin[ExtractBasicCommands]);

        return extractedMotorCommands.FunctionResults.First().GetValue<string>();
    }
}
