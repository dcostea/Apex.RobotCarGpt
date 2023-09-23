namespace Apex.RobotCarGpt;

public static class StringExtensions
{
    public static string ToArrow(this string function)
    {
        Console.OutputEncoding = System.Text.Encoding.Unicode;
        var x = function switch
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
            _ => throw new Exception($"Function {function} does not exist.")
        };

        return x;
    }
}