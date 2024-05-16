using Microsoft.SemanticKernel;

namespace Apex.RobotCarGpt.Helpers;

public static class PrintHelper
{
    public static string ToArrow(this string function)
    {
        Console.OutputEncoding = System.Text.Encoding.Unicode;
        var arrow = function.ToUpper() switch
        {
            //"FORWARD" => "→",
            //"BACKWARD" => "←",
            //"TURNLEFT" => "↑",
            //"TURNRIGHT" => "↓",
            "STOP" => "·",
            "FORWARD" => "🡲",
            "BACKWARD" => "🡰",
            "TURNLEFT" => "🡵",
            "TURNRIGHT" => "🡶",
            _ => "?"
        };

        return arrow;
    }

    public static void PrintAllPluginsFunctions(this Kernel kernel)
    {
        var functions = kernel.Plugins.GetFunctionsMetadata();

        Console.WriteLine("\nREGISTERED PLUGINS / FUNCTIONS: ");

        foreach (KernelFunctionMetadata func in functions)
        {
            PrintPluginFunction(func);
        }
    }

    public static void PrintPluginFunction(KernelFunctionMetadata func)
    {
        Console.WriteLine($"{func.PluginName}  => {func.Name}: {func.Description}");

        if (func.Parameters.Count > 0)
        {
            Console.WriteLine("   Params:");
            foreach (var p in func.Parameters)
            {
                Console.WriteLine($"    {p.Name}: {p.Description} (default: '{p.DefaultValue}')");
            }
        }
    }

    public static async Task PrintRenderedPromptAsync(this Kernel kernel, string promptTemplate, KernelArguments kernelArguments)
    {
        var promptTemplateFactory = new KernelPromptTemplateFactory();
        var promptTemplateRenderer = promptTemplateFactory.Create(new PromptTemplateConfig(promptTemplate));
        var renderedPrompt = await promptTemplateRenderer.RenderAsync(kernel, kernelArguments);

        Console.WriteLine(renderedPrompt);
    }
}