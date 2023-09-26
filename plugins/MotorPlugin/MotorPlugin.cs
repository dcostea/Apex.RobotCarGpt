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
    public string Backward(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.Low);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.Low);
        _logger.LogTrace("The car is moving backward.");
        return $"moving";
    }

    [SKFunction, Description("Moves the car forward.")]
    public string Forward(SKContext context)
    {
        var isStopped = context.Variables["input"].Contains("stopped");
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.Low);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.Low);
        _logger.LogTrace("The car is moving forward.");
        return $"moving";
    }

    [SKFunction, Description("Stops the car.")]
    public string Stop()
    {
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.High);
        //_gpioController.Write(_settings.LeftBackwardPin, PinValue.Low);
        //_gpioController.Write(_settings.RightBackwardPin, PinValue.Low);
        _logger.LogTrace("The car is stopped.");
        return $"stopped";
    }

    [SKFunction, Description("Turns the car anticlockwise.")]
    [SKParameter("input", "Car movement status")]
    public string TurnLeft(SKContext context)
    {
        //_gpioController.Write(_settings.LeftForwardPin, PinValue.High);
        var isStopped = context.Variables["input"].Contains("stopped");
        // if status is stopped, start the car
        var milliseconds = 500;
        System.Threading.Thread.Sleep(milliseconds);
        //_gpioController.Write(_settings.LeftForwardPin, PinValue.Low);
        _logger.LogTrace("The car turned left.");
        return $"moving";
    }

    [SKFunction, Description("Turns the car clockwise.")]
    [SKParameter("input", "Car movement status")]
    public string TurnRight(SKContext context)
    {
        //_gpioController.Write(_settings.RightForwardPin, PinValue.High);
        var isStopped = context.Variables["input"].Contains("stopped");
        // if status is stopped, start the car
        var milliseconds = 500;
        System.Threading.Thread.Sleep(milliseconds);
        //_gpioController.Write(_settings.RightForwardPin, PinValue.Low);
        _logger.LogTrace("The car turned right.");
        return $"moving";
    }
}
