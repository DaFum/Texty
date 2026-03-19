using Texty.Core.Models;
using Texty.Runtime.Clipboard;
using Texty.Runtime.Insertion;

namespace Texty.Tests;

public sealed class InsertionPipelineTests
{
    [Fact]
    public async Task Execute_ShouldRunAllDeterministicSteps()
    {
        var pipeline = new ClipboardInsertionPipeline(new InMemoryClipboardGateway(), new NoOpKeystrokeEmitter());
        var result = await pipeline.ExecuteAsync(
            new InsertionPayload("hello", "<p>hello</p>", ["CTRL+LEFT"]),
            new InsertionContext("notepad.exe", false, true, null));

        Assert.Contains(result, step => step.Step == InsertionStep.Prepare && step.Success);
        Assert.Contains(result, step => step.Step == InsertionStep.Insert && step.Success);
        Assert.Contains(result, step => step.Step == InsertionStep.Restore && step.Success);
        Assert.Contains(result, step => step.Step == InsertionStep.PostProcess && step.Success);
    }

    [Fact]
    public async Task Execute_ShouldBlockWhenTargetProcessDoesNotMatchForeground()
    {
        var pipeline = new ClipboardInsertionPipeline(
            new InMemoryClipboardGateway(),
            new NoOpKeystrokeEmitter(),
            new FakeForegroundProcessProvider("winword.exe"));

        var result = await pipeline.ExecuteAsync(
            new InsertionPayload("hello", "<p>hello</p>", []),
            new InsertionContext("notepad.exe", false, true, null));

        Assert.Contains(result, step => step.Step == InsertionStep.Prepare && !step.Success);
        Assert.DoesNotContain(result, step => step.Step == InsertionStep.Insert && step.Success);
    }

    [Fact]
    public async Task Execute_ShouldAllowTargetProcessMatchIgnoringExeSuffix()
    {
        var pipeline = new ClipboardInsertionPipeline(
            new InMemoryClipboardGateway(),
            new NoOpKeystrokeEmitter(),
            new FakeForegroundProcessProvider("notepad.exe"));

        var result = await pipeline.ExecuteAsync(
            new InsertionPayload("hello", "<p>hello</p>", []),
            new InsertionContext("NOTEPAD", false, true, null));

        Assert.Contains(result, step => step.Step == InsertionStep.Prepare && step.Success);
        Assert.Contains(result, step => step.Step == InsertionStep.Insert && step.Success);
    }

    private sealed class FakeForegroundProcessProvider : IForegroundProcessProvider
    {
        private readonly string _processName;

        public FakeForegroundProcessProvider(string processName)
        {
            _processName = processName;
        }

        public string? GetForegroundProcessName() => _processName;
    }
}
