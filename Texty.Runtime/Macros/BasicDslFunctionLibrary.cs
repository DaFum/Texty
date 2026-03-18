using System.Text.RegularExpressions;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Macros;

public sealed class BasicDslFunctionLibrary : IDslFunctionLibrary
{
    public bool TryInvoke(string functionName, IReadOnlyList<string> args, MacroExecutionContext context, out string? result)
    {
        _ = context;
        result = null;
        switch (functionName.ToLowerInvariant())
        {
            case "upper":
                if (args.Count >= 1)
                {
                    result = args[0].ToUpperInvariant();
                    return true;
                }

                return false;
            case "lower":
                if (args.Count >= 1)
                {
                    result = args[0].ToLowerInvariant();
                    return true;
                }

                return false;
            case "substr":
                if (args.Count >= 3 &&
                    int.TryParse(args[1], out var start) &&
                    int.TryParse(args[2], out var length))
                {
                    var value = args[0];
                    if (start < 0 || start >= value.Length)
                    {
                        result = string.Empty;
                    }
                    else
                    {
                        result = value.Substring(start, Math.Min(length, value.Length - start));
                    }

                    return true;
                }

                return false;
            case "regex":
                if (args.Count >= 2)
                {
                    var match = Regex.Match(args[0], args[1]);
                    result = match.Success ? match.Value : string.Empty;
                    return true;
                }

                return false;
            default:
                return false;
        }
    }
}
