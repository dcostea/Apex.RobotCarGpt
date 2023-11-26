namespace Commands;

public static class CommandExtensions
{
    public static string ToArrow(this string function)
    {
        Console.OutputEncoding = System.Text.Encoding.Unicode;
        var x = function.ToUpper() switch
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

        return x;
    }
}