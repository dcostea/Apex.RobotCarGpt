using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.Orchestration;
using Microsoft.SemanticKernel.SkillDefinition;

namespace Plugins;

public class MotorPlugin
{
    private readonly ILogger _logger;

    public MotorPlugin(ILogger logger)
    {
        _logger = logger;
    }

    [SKFunction, Description("Moves the car backward.")]
    [SKParameter("input", "Car movement status")]
    public string Backward(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        var state = isStopped ? "stopped" : "moving";
        _logger.LogTrace("The car was {state} and now is moving backward.", state);

        return $"moving";
    }

    [SKFunction, Description("Moves the car forward.")]
    [SKParameter("input", "Car movement status")]
    public string Forward(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        var state = isStopped ? "stopped" : "moving";
        _logger.LogTrace("The car was {state} and now is moving forward.", state);

        return $"moving";
    }

    [SKFunction, Description("Stops the car.")]
    [SKParameter("input", "Car movement status")]
    public string Stop(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        var state = isStopped ? "stopped" : "moving";
        _logger.LogTrace("The car was {state} and now is stopping.", state);

        return $"stopped";
    }

    [SKFunction, Description("Turns the car anticlockwise.")]
    [SKParameter("input", "Car movement status")]
    public string TurnLeft(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        var state = isStopped ? "stopped" : "moving";
        _logger.LogTrace("The car was {state} and now is turning left.", state);

        return $"moving";
    }

    [SKFunction, Description("Turns the car clockwise.")]
    [SKParameter("input", "Car movement status")]
    public string TurnRight(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        var state = isStopped ? "stopped" : "moving";
        _logger.LogTrace("The car was {state} and now is turning right.", state);

        return $"moving";
    }
}
