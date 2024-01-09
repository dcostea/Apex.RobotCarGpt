using System.ComponentModel;
using Commands;
using Microsoft.SemanticKernel;
using Serilog;

namespace Plugins;

public class MotorPlugin
{
    [KernelFunction, Description("Moves the car backward.")]
    public void Backward()
    {
        //Log.Information("{arrow} ({input} => backward)", "Backward".ToArrow(), input);
        Log.Information("Backward".ToArrow());

        // TODO call car motor API, backward endpoint
    }

    [KernelFunction, Description("Moves the car forward.")]
    public void Forward()
    {
        //Log.Information("{arrow} ({input} => forward)", "Forward".ToArrow(), input);
        Log.Information("Forward".ToArrow());

        // TODO call car motor API, forward endpoint
    }

    [KernelFunction, Description("Stops the car.")]
    public void Stop()
    {
        //Log.Information("{arrow} ({input} => stop)", "Stop".ToArrow(), input);
        Log.Information("Stop".ToArrow());

        // TODO call car motor API, stop endpoint
    }

    [KernelFunction, Description("Turns the car anticlockwise.")]
    public void TurnLeft()
    {
        //Log.Information("{arrow} ({input} => turn left)", "TurnLeft".ToArrow(), input);
        Log.Information("TurnLeft".ToArrow());

        // TODO call car motor API, turn left endpoint
    }

    [KernelFunction, Description("Turns the car clockwise.")]
    public void TurnRight()
    {
        //Log.Information("{arrow} ({input} => turn right)", "TurnRight".ToArrow(), input);
        Log.Information("TurnRight".ToArrow());
        // TODO call car motor API, turn right endpoint
    }
}
