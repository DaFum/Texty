using Texty.Core.Models;
using Texty.Runtime.Templating;
using Texty.Runtime.Triggering;

namespace Texty.Tests;

public sealed class TriggerAndTemplateTests
{
    [Fact]
    public async Task TriggerEvaluator_ShouldMatchAutotextRule()
    {
        var evaluator = new TriggerEvaluator();
        var rule = new TriggerRule(
            Guid.NewGuid(),
            TriggerType.Autotext,
            "txsig",
            false,
            TriggerScope.Any,
            null,
            true);

        var signal = new TriggerSignal(TriggerType.Autotext, "my txsig", "WINWORD.EXE", TriggerScope.Any);
        var matches = await evaluator.EvaluateAsync([rule], signal);

        Assert.Single(matches);
        Assert.Equal(rule.Id, matches[0].Rule.Id);
    }

    [Fact]
    public async Task TemplateRenderer_ShouldReplacePlaceholders()
    {
        var renderer = new TemplateRenderer();
        var now = DateTimeOffset.UtcNow;
        var snippet = new Snippet(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Welcome",
            "txwelcome",
            "Hello {{name}}",
            "<p>Hello {{name}}</p>",
            [],
            [],
            [],
            new SnippetTemplate("<p>Hello {{name}}</p>", [new TemplateField("name", "Name", FormFieldType.Text, true, null, null, null, null, null)]),
            SnippetHighlightMode.None,
            null,
            false,
            now,
            now,
            "test");

        var rendered = await renderer.RenderAsync(
            snippet,
            new RenderContext(new Dictionary<string, object?> { ["name"] = "Alex" }));

        Assert.Equal("Hello Alex", rendered.PlainText);
        Assert.Contains("Alex", rendered.HtmlText, StringComparison.Ordinal);
    }
}
