using System.ComponentModel;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Plugins;

public class MotorPlugin(ILogger _logger)
{
    [SKFunction, Description("Moves the car backward.")]
    public string Backward([Description("The current input of the car.")] string input)
    {
        _logger.LogDebug("COMMAND: The car was {input} and now is moving backward.", input);
        // TODO call car motor API, backward endpoint

        return "backward";
    }

    [SKFunction, Description("Moves the car forward.")]
    public string Forward([Description("The current input of the car.")] string input)
    {
        _logger.LogDebug("COMMAND: The car was {input} and now is moving forward.", input);
        // TODO call car motor API, forward endpoint

        return "forward";
    }

    [SKFunction, Description("Stops the car.")]
    public string Stop([Description("The current input of the car.")] string input)
    {
        _logger.LogDebug("COMMAND: The car was {input} and now is stopping.", input);
        // TODO call car motor API, stop endpoint

        return "stopped";
    }

    [SKFunction, Description("Turns the car anticlockwise.")]
    public string TurnLeft([Description("The current input of the car.")] string input)
    {
        _logger.LogDebug("COMMAND: The car was {input} and now is turning left.", input);
        // TODO call car motor API, turn left endpoint

        return "turned left";
    }

    [SKFunction, Description("Turns the car clockwise.")]
    public string TurnRight([Description("The current input of the car.")] string input)
    {
        _logger.LogDebug("COMMAND: The car was {input} and now is turning right.", input);
        // TODO call car motor API, turn right endpoint

        return "turned right";
    }
}
