using System.ComponentModel;
using Commands;
using Microsoft.SemanticKernel;
using Serilog;

namespace Plugins;

public class MotorPlugin
{
    [KernelFunction, Description("Moves the car backward.")]
    public string Backward([Description("The current input of the car.")] string input)
    {
        Log.Information("Backward".ToArrow());
        // TODO call car motor API, backward endpoint

        return "backward";
    }

    [KernelFunction, Description("Moves the car forward.")]
    public string Forward([Description("The current input of the car.")] string input)
    {
        Log.Information("Forward".ToArrow());
        // TODO call car motor API, forward endpoint

        return "forward";
    }

    [KernelFunction, Description("Stops the car.")]
    public string Stop([Description("The current input of the car.")] string input)
    {
        Log.Information("Stop".ToArrow());
        // TODO call car motor API, stop endpoint

        return "stopped";
    }

    [KernelFunction, Description("Turns the car anticlockwise.")]
    public string TurnLeft([Description("The current input of the car.")] string input)
    {
        Log.Information("TurnLeft".ToArrow());
        // TODO call car motor API, turn left endpoint

        return "turned left";
    }

    [KernelFunction, Description("Turns the car clockwise.")]
    public string TurnRight([Description("The current input of the car.")] string input)
    {
        Log.Information("TurnRight".ToArrow());
        // TODO call car motor API, turn right endpoint

        return "turned right";
    }
}
