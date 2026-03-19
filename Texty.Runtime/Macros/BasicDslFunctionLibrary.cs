using System.Text.RegularExpressions;
using Texty.Core.Interfaces;
using Texty.Core.Models;

namespace Texty.Runtime.Macros;

public sealed class BasicDslFunctionLibrary : IDslFunctionLibrary
{
    private const int MaxRegexPatternLength = 512;
    private const int MaxRegexInputLength = 16_384;
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(1);

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
                    if (start < 0 || length < 0 || start >= value.Length)
                    {
                        result = string.Empty;
                    }
                    else
                    {
                        var safeLength = Math.Max(0, Math.Min(length, value.Length - start));
                        result = value.Substring(start, safeLength);
                    }

                    return true;
                }

                return false;
            case "regex":
                if (args.Count >= 2)
                {
                    if (!CanRunRegex(args[0], args[1]))
                    {
                        result = string.Empty;
                        return true;
                    }

                    try
                    {
                        var match = Regex.Match(args[0], args[1], RegexOptions.None, RegexTimeout);
                        result = match.Success ? match.Value : string.Empty;
                    }
                    catch (ArgumentException)
                    {
                        result = string.Empty;
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        result = string.Empty;
                    }

                    return true;
                }

                return false;
            case "replace":
                if (args.Count >= 3)
                {
                    result = args[0].Replace(args[1], args[2], StringComparison.OrdinalIgnoreCase);
                    return true;
                }

                return false;
            case "regexreplace":
                if (args.Count >= 3)
                {
                    if (!CanRunRegex(args[0], args[1]))
                    {
                        result = string.Empty;
                        return true;
                    }

                    try
                    {
                        result = Regex.Replace(args[0], args[1], args[2], RegexOptions.None, RegexTimeout);
                    }
                    catch (ArgumentException)
                    {
                        result = string.Empty;
                    }
                    catch (RegexMatchTimeoutException)
                    {
                        result = string.Empty;
                    }

                    return true;
                }

                return false;
            case "concat":
                if (args.Count >= 1)
                {
                    result = string.Concat(args);
                    return true;
                }

                return false;
            case "adddays":
                if (args.Count >= 2 &&
                    DateTimeOffset.TryParse(args[0], out var baseDate) &&
                    int.TryParse(args[1], out var days))
                {
                    result = baseDate.AddDays(days).ToString("yyyy-MM-dd");
                    return true;
                }

                return false;
            case "formatdate":
                if (args.Count >= 2 &&
                    DateTimeOffset.TryParse(args[0], out var dateValue))
                {
                    result = dateValue.ToString(args[1]);
                    return true;
                }

                return false;
            default:
                return false;
        }
    }

    private static bool CanRunRegex(string input, string pattern)
    {
        return input.Length <= MaxRegexInputLength &&
               pattern.Length is > 0 and <= MaxRegexPatternLength;
    }
}
