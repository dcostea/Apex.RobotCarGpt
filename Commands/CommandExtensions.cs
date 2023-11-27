using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Planners;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.TemplateEngine;
using Microsoft.SemanticKernel.Connectors.AI.OpenAI;
using Microsoft.SemanticKernel.Planning;

namespace Commands;

public static class CommandExtensions
{
    public const string PluginsFolder = "plugins";
    public const string MotorPlugin = nameof(MotorPlugin);
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
        ModelSettings =
        [
            new OpenAIRequestSettings
            {
                MaxTokens = 500,
                Temperature = 0.0
            }
        ],
        Input = new ()
        {
            Parameters =
            [
                new() { Name = "input", Description = "Action to be performed." },
                new() { Name = "commands", Description = "The commands to chose from." },
            ]
        }
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
        ModelSettings =
        [
            new OpenAIRequestSettings
            {
                MaxTokens = 500,
                Temperature = 0.0
            }
        ],
        Input = new ()
        {
            Parameters =
            [
                new() { Name = "input", Description = "Action to be performed." },
                new() { Name = "commands", Description = "The commands to chose from." },
            ]
        }
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
        ModelSettings =
        [
            new OpenAIRequestSettings
            {
                MaxTokens = 500,
                Temperature = 0.0
            }
        ],
        Input = new()
        {
            Parameters =
            [
                new() { Name = "input", Description = "Action to be performed." },
            ]
        }
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

    public static async Task<Plan> CreateSequentialPlan(this IKernel kernel, string ask, ILogger logger)
    {
        // Create sequential plan

        var config = new SequentialPlannerConfig();
        config.ExcludedFunctions.Add(ExtractBasicCommands);             // we don't want to allow this semantic function in the plan!
        config.ExcludedFunctions.Add(ExtractMostRelevantBasicCommand);  // we don't want to allow this semantic function in the plan!
        var planner = new SequentialPlanner(kernel, config);

        var plan = await planner.CreatePlanAsync(ask);

        // show steps as arrows (function names converted to arrows)
        var planStepsArrows = plan.Steps.Any()
            ? string.Join(" ", plan.Steps.Select(s => s.Name.ToArrow()))
            : "No steps!";

        logger.LogInformation("  PLAN STEPS: {arrows}", planStepsArrows);

        return plan;
    }

    public static async Task<Plan> CreateActionPlan(this IKernel kernel, string ask, ILogger logger)
    {
        // Create action plan

        var config = new ActionPlannerConfig();
        config.ExcludedFunctions.Add(ExtractBasicCommands);             // we don't want to allow this semantic function in the plan!
        config.ExcludedFunctions.Add(ExtractMostRelevantBasicCommand);  // we don't want to allow this semantic function in the plan!
        var planner = new ActionPlanner(kernel, config);

        var plan = await planner.CreatePlanAsync(ask);

        // show steps as arrows (function names converted to arrows)
        var planStepsArrows = plan.Steps.Any()
            ? string.Join(" ", plan.Steps.Select(s => s.Name.ToArrow()))
            : "No steps!";

        logger.LogInformation("  PLAN STEP: {arrows}", planStepsArrows);

        return plan;
    }
}
