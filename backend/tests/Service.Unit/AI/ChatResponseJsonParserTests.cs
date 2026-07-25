using Module.AI.Chat;
using Xunit;

namespace Service.Unit.AI;

public class ChatResponseJsonParserTests
{
    [Fact]
    public void TryDeserialize_ParsesPlainJson()
    {
        const string content =
            """{"threadId":"thread-1","agentId":"leo","userVision":"Build CRM","scopes":[{"supervisorId":"supervisor-sales","message":"Create sales strategy"}]}""";

        var success = ChatResponseJsonParser.TryDeserialize<Vision>(content, out var response);

        Assert.True(success);
        Assert.NotNull(response);
        Assert.Equal("thread-1", response.ThreadId);
        Assert.Single(response.Scopes);
    }

    [Fact]
    public void TryDeserialize_ParsesMarkdownFencedJson()
    {
        const string content =
            """
            ```json
            {"threadId":"thread-2","agentId":"leo","userVision":"Build ERP","scopes":[{"supervisorId":"supervisor-finance","message":"Plan accounting workflows"}]}
            ```
            """;

        var success = ChatResponseJsonParser.TryDeserialize<Vision>(content, out var response);

        Assert.True(success);
        Assert.NotNull(response);
        Assert.Equal("thread-2", response.ThreadId);
    }

    [Fact]
    public void TryDeserialize_ParsesJsonEmbeddedInProse()
    {
        const string content =
            """
            Here is the result you asked for:
            {"threadId":"thread-3","agentId":"leo","userVision":"Build HR suite","scopes":[{"supervisorId":"supervisor-hr","message":"Define HR processes"}]}
            Let me know if you need changes.
            """;

        var success = ChatResponseJsonParser.TryDeserialize<Vision>(content, out var response);

        Assert.True(success);
        Assert.NotNull(response);
        Assert.Equal("thread-3", response.ThreadId);
    }

    [Fact]
    public void TryDeserialize_UsesConfiguredEnumSerialization()
    {
        const string content =
            """{"threadId":"thread-4","status":"READY","agentToRecruit":{"agentId":"specialist-backend-engineer","jobTitle":"Backend Engineer","jobDescription":"Build APIs","department":"BACKEND","systemPrompt":"You are backend expert","guardrails":[],"supervisorId":"supervisor-platform"}}""";

        var success = ChatResponseJsonParser.TryDeserialize<Recruitment>(content, out var response);

        Assert.True(success);
        Assert.NotNull(response);
        Assert.Equal(RecruitingStatus.Ready, response.Status);
        Assert.Equal(Department.Backend, response.AgentToRecruit.Department);
    }

    [Fact]
    public void TryDeserialize_ReturnsFalseForInvalidJson()
    {
        const string content = "This is not json.";

        var success = ChatResponseJsonParser.TryDeserialize<Vision>(content, out var response);

        Assert.False(success);
        Assert.Null(response);
    }
}
