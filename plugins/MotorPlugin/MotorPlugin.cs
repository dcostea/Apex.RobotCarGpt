using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Orchestration;

namespace Plugins;

public class MotorPlugin
{
    private readonly ILogger _logger;

    public MotorPlugin(ILogger logger)
    {
        _logger = logger;
    }

    [SKFunction, Description("Moves the car backward.")]
    public string Backward(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        var state = isStopped ? "stopped" : "moving";
        _logger.LogTrace("The car was {state} and now is moving backward.", state);

        return $"moving";
    }

    [SKFunction, Description("Moves the car forward.")]
    public string Forward(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        var state = isStopped ? "stopped" : "moving";
        _logger.LogTrace("The car was {state} and now is moving forward.", state);

        return $"moving";
    }

    [SKFunction, Description("Stops the car.")]
    public string Stop(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        var state = isStopped ? "stopped" : "moving";
        _logger.LogTrace("The car was {state} and now is stopping.", state);

        return $"stopped";
    }

    [SKFunction, Description("Turns the car anticlockwise.")]
    public string TurnLeft(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        var state = isStopped ? "stopped" : "moving";
        _logger.LogTrace("The car was {state} and now is turning left.", state);

        return $"moving";
    }

    [SKFunction, Description("Turns the car clockwise.")]
    public string TurnRight(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        var state = isStopped ? "stopped" : "moving";
        _logger.LogTrace("The car was {state} and now is turning right.", state);

        return $"moving";
    }
}
