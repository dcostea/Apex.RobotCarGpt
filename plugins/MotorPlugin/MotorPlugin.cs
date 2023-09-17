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

    [SKName("BACKWARD")]
    [SKFunction, Description("The car moves backward. This command moves the car backward until another command is called. This command terminates any other command and keeps the car moving backward.")]
    public string Backward(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.Low);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.Low);
        _logger.LogDebug("The car is moving backward.");
        return $"moving";
    }

    [SKName("FORWARD")]
    [SKFunction, Description("The car moves forward. This command moves the car forward until another command is called. This command terminates any other command and keeps the car moving forward.")]
    public string Forward(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.Low);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.Low);
        _logger.LogDebug("The car is moving forward.");
        return $"moving";
    }

    [SKName("STOP")]
    [SKFunction, Description("The car stops moving.")]
    public string Stop()
    {
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.Low);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.Low);
        _logger.LogDebug("The car is stopped.");
        return $"stopped";
    }

    [SKName("TURNLEFT")]
    [SKFunction, Description("The car turns anti-clockwise 45 degrees. A complete turn of 360 degrees brings the car in the initial position.")]
    [SKParameter("input", "Car movement status")]
    public string TurnLeft(SKContext context)
    {
        //_gpioController.Write(_settings.RightForwardPin, PinValue.High);
        var isStopped = context.Variables["input"].Contains("stopped");
        // if status is stopped, start the car
        var milliseconds = 500;
        System.Threading.Thread.Sleep(milliseconds); // 45 degrees turn
        //_gpioController.Write(_settings.RightForwardPin, PinValue.Low);
        _logger.LogDebug("The car turned left.");
        return $"moving";
    }

    [SKName("TURNRIGHT")]
    [SKFunction, Description("The car turns clockwise 45 degrees. A complete turn of 360 degrees brings the car in the initial position.")]
    [SKParameter("input", "Car movement status")]
    public string TurnRight(SKContext context)
    {
        //_gpioController.Write(_settings.RightForwardPin, PinValue.High);
        var isStopped = context.Variables["input"].Contains("stopped");
        // if status is stopped, start the car
        var milliseconds = 500;
        System.Threading.Thread.Sleep(milliseconds); // 45 degrees turn
        //_gpioController.Write(_settings.RightForwardPin, PinValue.Low);
        _logger.LogDebug("The car turned right.");
        return $"moving";
    }

}
