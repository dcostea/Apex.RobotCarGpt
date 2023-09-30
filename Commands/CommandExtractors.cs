using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SemanticFunctions;
using Microsoft.SemanticKernel;

namespace Commands;

public static class CommandExtractors
{
    const string Plugins = nameof(Plugins);
    const string MotorPlugin = nameof(MotorPlugin);
    const string ExtractBasicCommands = nameof(ExtractBasicCommands);

    public async static Task<string> ExtractCommandsUsingInlineSemanticFunctionAsync(this IKernel kernel, string ask)
    {
        var extractMotorCommandsPrompt = """
You are a minimalistic car capable of performing some basic commands like go forward, go backward, turn left, turn right, and stop.
Initial state of the car is stopped.
Take this goal "{{$input}}" and create a list of basic commands, as enumerated above, to fulfill the goal. 
""";

        var extractMotorCommandsFunction = kernel.CreateSemanticFunction(extractMotorCommandsPrompt, maxTokens: 500);
        var extractedMotorCommands = await extractMotorCommandsFunction.InvokeAsync(ask);

        return extractedMotorCommands.Result;
    }

    public async static Task<string> ExtractCommandsUsingRegisteredSemanticFunctionAsync(this IKernel kernel, string ask)
    {
        var extractMotorCommandsPrompt = """
You are a minimalistic car capable of performing some basic commands like {{$commands}}.
Initial state of the car is stopped.
Take this goal "{{$input}}" and create a list of basic commands, as enumerated above, to fulfill the goal. 
""";

        var promptConfig = new PromptTemplateConfig
        {
            Schema = 1,
            Type = "completion",
            Description = "Create a list of basic commands.",
            Completion =
            {
                MaxTokens = 500,
                Temperature = 0.0,
                TopP = 0.0,
                PresencePenalty = 0.0,
                FrequencyPenalty = 0.0
            },
            Input =
            {
                Parameters = new List<PromptTemplateConfig.InputParameter>
                {
                    new PromptTemplateConfig.InputParameter
                    {
                        Name = "input",
                        Description = "The car action.",
                        DefaultValue = ""
                    },
                    new PromptTemplateConfig.InputParameter
                    {
                        Name = "commands",
                        Description = "The commands to choose from.",
                        DefaultValue = ""
                    },
                }
            }
        };

        var variables = new ContextVariables
        {
            ["input"] = ask,
            ["commands"] = "go forward, go backward, turn left, turn right, and stop"
        };

        var promptTemplate = new PromptTemplate(
            extractMotorCommandsPrompt,
            promptConfig,
            kernel
        );

        // register a semantic function
        var functionConfig = new SemanticFunctionConfig(promptConfig, promptTemplate);
        var extractMotorCommandsFunction = kernel.RegisterSemanticFunction(MotorPlugin, ExtractBasicCommands, functionConfig);

        var extractedMotorCommands = await kernel.RunAsync(extractMotorCommandsFunction, variables);

        return extractedMotorCommands.Result;
    }

    public async static Task<string> ExtractCommandsUsingPluginSemanticFunctionAsync(this IKernel kernel, string ask)
    {
        var variables = new ContextVariables
        {
            ["input"] = ask,
            ["commands"] = "go forward, go backward, turn left, turn right, and stop"
        };

        // import semantic functions from MotorPlugin
        var pluginsDirectory = Path.Combine(Directory.GetCurrentDirectory(), Plugins);
        var semanticMotorPlugin = kernel.ImportSemanticSkillFromDirectory(pluginsDirectory, MotorPlugin);

        var extractedMotorCommands = await kernel.RunAsync(variables, semanticMotorPlugin[ExtractBasicCommands]);

        return extractedMotorCommands.Result;
    }
}
