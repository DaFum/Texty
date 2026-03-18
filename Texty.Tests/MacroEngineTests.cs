using Texty.Core.Models;
using Texty.Runtime.Macros;

namespace Texty.Tests;

public sealed class MacroEngineTests
{
    [Fact]
    public void Execute_ShouldApplySetAppendAndFunction()
    {
        var engine = new MacroEngine(new BasicDslFunctionLibrary());
        var script = string.Join(
            Environment.NewLine,
            "set name andre",
            "func upperName upper $name",
            "append Hello $upperName");

        var result = engine.Execute(
            script,
            new MacroExecutionContext(new Dictionary<string, string>(), new InsertionContext(null, false, false, null)));

        Assert.Equal("Hello ANDRE", result.Output);
        Assert.True(result.Variables.ContainsKey("upperName"));
        Assert.Contains("func:upper", result.AuditTrail);
    }
}
