// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Planning;
using Microsoft.SemanticKernel.Planning.Sequential;

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
{
    builder
        .SetMinimumLevel(LogLevel.Trace)
        .AddConsole()
        .AddDebug();
});

// Create kernel
IKernel kernel = new KernelBuilder()
    .WithCompletionService()
    .WithLoggerFactory(loggerFactory)
    .Build();

_ = kernel.ImportSkill(new Plugins.MotorPlugin(kernel.LoggerFactory.CreateLogger("MotorPlugin")), "MotorPlugin");

// Create a planner
var planner = new SequentialPlanner(kernel);

//var ask = "What are the steps the car has to perform to walk like a jellyfish?";
//var ask = "What are the steps the car has to perform to go like turn left forward turn right backward stop?";
//var ask = "What are the steps the car has to perform to go 10 steps in randomly selected direction like forward, backward, and turning left or right?";
//var ask = "What are the steps the car has to perform to avoid a tree?";
//var ask = "What are the steps the car has to perform to avoid the tree by going around?";
//var ask = "What are the steps the car has to perform to do an evasive maneuver?";
//var ask = "What are the steps the car has to perform to run away?";
//var ask = "What are the steps the car has to perform to rumba dance?";
//var ask = "What are the steps the car has to perform to do some ballerina moves?";
//var ask = "What are the steps the car has to perform to go on square path?";
//var ask = "What are the steps the car has to perform to go on square path? I don't like tuning left";
//var ask = "What are the steps the car has to perform to move and return in the same place where it started?";
//var ask = "What are the steps the car has to perform to move forward, turn left, forward and return in the same place where it started?";
var ask = "What are the steps the car has to perform to do a pretty complex evasive maneuver with a least 15 steps? Stop at every 5 steps.";
//var ask = "What are the steps the car has to perform to sway (semi-circles)?";
//var ask = "What are the steps the car has to perform to do the moonwalk dancing (maximum 10 steps)?";
//var ask = "What are the steps the car has to perform to go zigzag for maximum 6 steps and the stop?";
//var ask = "What are the steps the car has to perform to go on a circle?";
//var ask = "What are the steps the car has to perform to do a full circle?";
//var ask = "What are the steps the car has to perform to do a full circle by turning left and then to do a full circle by turning right?";

var plan = await planner.CreatePlanAsync(ask);

Console.WriteLine($"Ask: {ask}");
foreach (var step in plan.Steps)
{
    Console.WriteLine(step.Name);
}

var result = (await kernel.RunAsync(plan)).Result;
Console.WriteLine("\nPlan results:");
Console.WriteLine(result.Trim());

//Console.WriteLine("Plan:\n");
//Console.WriteLine(JsonSerializer.Serialize(plan, new JsonSerializerOptions { WriteIndented = true }));
