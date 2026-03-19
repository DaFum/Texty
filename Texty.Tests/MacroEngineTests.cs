using Texty.Core.Models;
using Texty.Core.Interfaces;
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

        Assert.True(result.Success);
        Assert.Equal("Hello ANDRE", result.Output);
        Assert.True(result.Variables.ContainsKey("upperName"));
        Assert.Contains("func:upper", result.AuditTrail);
    }

    [Fact]
    public void Execute_ShouldSupportIfElseBlocks()
    {
        var engine = new MacroEngine(new BasicDslFunctionLibrary());
        var script = string.Join(
            Environment.NewLine,
            "set role admin",
            "if $role eq admin",
            "append allowed",
            "else",
            "append denied",
            "end");

        var result = engine.Execute(
            script,
            new MacroExecutionContext(new Dictionary<string, string>(), new InsertionContext(null, false, false, null)));

        Assert.True(result.Success);
        Assert.Equal("allowed", result.Output);
    }

    [Fact]
    public void Execute_ShouldInvokeMacroActions()
    {
        var fakeActions = new FakeActionExecutor();
        var engine = new MacroEngine(new BasicDslFunctionLibrary(), fakeActions);
        var script = string.Join(
            Environment.NewLine,
            "set correlationId 42",
            "action notify hello world");

        var result = engine.Execute(
            script,
            new MacroExecutionContext(new Dictionary<string, string>(), new InsertionContext(null, false, false, null)));

        Assert.True(result.Success);
        Assert.Equal("notify", fakeActions.LastRequest?.Name);
        Assert.Equal("42", fakeActions.LastRequest?.CorrelationId);
    }

    [Fact]
    public void Execute_ShouldPassActionPolicyFromContext()
    {
        var fakeActions = new FakeActionExecutor();
        var engine = new MacroEngine(new BasicDslFunctionLibrary(), fakeActions);
        var script = "action powershell Get-Date";

        _ = engine.Execute(
            script,
            new MacroExecutionContext(
                new Dictionary<string, string>(),
                new InsertionContext(null, false, false, null),
                new MacroActionPolicy(
                    AllowProcessStart: true,
                    AllowFileSystemWrite: true,
                    AllowExternalOpen: true,
                    AllowNotifications: true,
                    AllowPowerShell: false)));

        Assert.NotNull(fakeActions.LastRequest);
        Assert.False(fakeActions.LastRequest!.Policy.AllowPowerShell);
    }

    private sealed class FakeActionExecutor : IMacroActionExecutor
    {
        public MacroActionRequest? LastRequest { get; private set; }

        public Task<MacroActionResult> ExecuteAsync(MacroActionRequest request, CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Task.FromResult(new MacroActionResult(true, false, "ok"));
        }
    }
}
