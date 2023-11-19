using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Plugins;

public class MotorPlugin(ILogger _logger)
{
    [SKFunction, Description("Moves the car backward.")]
    public string Backward([Description("The current state of the car.")] string input)
    {
        _logger.LogDebug("COMMAND: The car was {state} and now is moving backward.", input);

        return "moving";
    }

    [SKFunction, Description("Moves the car forward.")]
    public string Forward([Description("The current state of the car.")] string input)
    {
        _logger.LogDebug("COMMAND: The car was {state} and now is moving forward.", input);

        return "moving";
    }

    [SKFunction, Description("Stops the car.")]
    public string Stop([Description("The current state of the car.")] string input)
    {
        _logger.LogDebug("COMMAND: The car was {state} and now is stopping.", input);

        return "stopped";
    }

    [SKFunction, Description("Turns the car anticlockwise.")]
    public string TurnLeft([Description("The current state of the car.")] string input)
    {
        _logger.LogDebug("COMMAND: The car was {state} and now is turning left.", input);

        return "moving";
    }

    [SKFunction, Description("Turns the car clockwise.")]
    public string TurnRight([Description("The current state of the car.")] string input)
    {
        _logger.LogDebug("COMMAND: The car was {state} and now is turning right.", input);

        return "moving";
    }
}
