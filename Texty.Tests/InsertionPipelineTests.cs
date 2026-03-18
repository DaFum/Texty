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
}
