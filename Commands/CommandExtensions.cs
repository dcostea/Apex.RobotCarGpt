using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Planning.Handlebars;

namespace Commands;

public static class CommandExtensions
{
    public const string PluginsFolder = "plugins";

    public const string CommandsPlugin = nameof(CommandsPlugin);

    public const string BasicCommands = "go forward, go backward, turn left, turn right, and stop";

    public const string ExtractBasicCommands = nameof(ExtractBasicCommands);
    public const string ExtractBasicCommandsPromptTemplate = """
You are a robot car capable of performing only the following allowed basic commands: {{ $commands }}.
Initial state of the car is stopped. The last state of the car is stopped.
You need to:
[START ACTION TO BE PERFORMED]
{{ $input }}
[END ACTION TO BE PERFORMED]
Extract a list of basic commands from the action to be performed to fulfill the goal.
Restrict the extracted list to the allowed basic commands enumerated above.
Give me the list as a comma separated list. Remove any introduction, ending or explanation from the response, show me only the list of allowed commands.
""";
    public static readonly PromptTemplateConfig ExtractBasicCommandsPromptTemplateConfig = new()
    {
        Description = "Extract basic motor commands.",
        ExecutionSettings = [
            new OpenAIPromptExecutionSettings
            {
                MaxTokens = 500,
                Temperature = 0.0
            }
        ],
        InputVariables =
        [
            new() { Name = "input", Description = "Action to be performed." },
            new() { Name = "commands", Description = "The commands to chose from." },
        ],
        Name = ExtractBasicCommands,
        Template = ExtractBasicCommandsPromptTemplate
    };

    public const string ExtractMostRelevantBasicCommand = nameof(ExtractMostRelevantBasicCommand);
    public const string ExtractMostRelevantBasicCommandPromptTemplate = """
You are a robot car capable of performing only the following allowed basic commands: {{ $commands }}.
Initial state of the car is stopped. The last state of the car is stopped.
You need to:
[START ACTION TO BE PERFORMED]
{{ $input }}
[END ACTION TO BE PERFORMED]
Extract the most relevant basic command, one command only, for the action to be performed to fulfill the goal.
Restrict the extracted basic command to one of the allowed basic commands enumerated above.
Remove any introduction, ending or explanation from the response, show me only the extracted basic command.
""";
    public static readonly PromptTemplateConfig ExtractMostRelevantBasicCommandPromptTemplateConfig = new()
    {
        Description = "Extract most relevant basic motor command.",
        ExecutionSettings = [
            new OpenAIPromptExecutionSettings
            {
                MaxTokens = 500,
                Temperature = 0.0
            }
        ],
        InputVariables =
        {
            new() { Name = "input", Description = "Action to be performed." },
            new() { Name = "commands", Description = "The commands to chose from." },
        },
        Name = ExtractMostRelevantBasicCommand,
        Template = ExtractMostRelevantBasicCommandPromptTemplate
    };

    public const string ExecuteBasicCommand = nameof(ExecuteBasicCommand);
    public const string ExecuteBasicCommandPromptTemplate = """
Echo the next text back to me:
Forward action is returning: {{ MotorPlugin.Forward $input }}
Backward action is returning: {{ MotorPlugin.Backward $input }}
Turn Left action is returning: {{ MotorPlugin.TurnLeft $input }}
Turn Right action is returning: {{ MotorPlugin.TurnRight $input }}
Stop action is returning: {{ MotorPlugin.Stop $input }}
""";
    public static readonly PromptTemplateConfig ExecuteBasicCommandPromptTemplateConfig = new()
    {
        Description = "Execute basic motor command.",
        ExecutionSettings = [
            new OpenAIPromptExecutionSettings
            {
                MaxTokens = 500,
                Temperature = 0.0
            }
        ],
        InputVariables =
        {
            new() { Name = "input", Description = "Action to be performed." },
        },
        Name = ExecuteBasicCommand,
        Template = ExecuteBasicCommandPromptTemplate
    };

    public static string ToArrow(this string function)
    {
        Console.OutputEncoding = System.Text.Encoding.Unicode;
        var x = function.ToUpper() switch
        {
            //"FORWARD" => "→",
            //"BACKWARD" => "←",
            //"TURNLEFT" => "↑",
            //"TURNRIGHT" => "↓",
            "STOP" => "·",
            "FORWARD" => "🡲",
            "BACKWARD" => "🡰",
            "TURNLEFT" => "🡵",
            "TURNRIGHT" => "🡶",
            _ => "?"
        };

        return x;
    }
}
