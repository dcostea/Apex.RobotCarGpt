using Apex.RobotCarGpt.Helpers;
using Microsoft.SemanticKernel;
using System.ComponentModel;

namespace Apex.RobotCarGpt.Plugins.MotorCommandsPlugin;

[Description("Car motor plugin.")]
public class MotorCommandsPlugin
{
    [KernelFunction, Description("Moves the car backward.")]
    public async Task<string> Backward()
    {
        // TODO call car motor API, backward endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Backward".ToArrow());
        Console.ResetColor();

        return await Task.FromResult("moving backward...");
    }

    [KernelFunction, Description("Moves the car forward.")]
    public async Task<string> Forward()
    {
        // TODO call car motor API, forward endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Forward".ToArrow());
        Console.ResetColor();

        return await Task.FromResult("moving forward...");
    }

    [KernelFunction, Description("Stops the car.")]
    public async Task<string> Stop()
    {
        // TODO call car motor API, stop endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Stop".ToArrow());
        Console.ResetColor();

        return await Task.FromResult("stopping...");
    }

    [KernelFunction, Description("Turns the car anticlockwise.")]
    public async Task<string> TurnLeft()
    {
        // TODO call car motor API, turn left endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("TurnLeft".ToArrow());
        Console.ResetColor();

        return await Task.FromResult("turning anticlockwise...");
    }

    [KernelFunction, Description("Turns the car clockwise.")]
    public async Task<string> TurnRight()
    {
        // TODO call car motor API, turn right endpoint
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("TurnRight".ToArrow());
        Console.ResetColor();

        return await Task.FromResult("turning clockwise...");
    }
}
