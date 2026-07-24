using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.Chat;
using Xunit;

namespace Service.Integration;

public class HelgaWorkflows : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HelgaChatService _helgaChatService;

    public HelgaWorkflows(WebApplicationFactory<Program> factory)
    {
        var serviceProvider = factory.Services.CreateScope().ServiceProvider;
        _helgaChatService = serviceProvider.GetRequiredService<HelgaChatService>();
    }

    [Fact]
    public async Task Recruit_VisionProvided_RecruitSupervisor()
    {
        var response = GetVision();

        foreach (var scope in response.Scopes)
        {
            var request = new JobApplication(response.ThreadId, response.AgentId, scope.SupervisorId, scope.Message);
            var history = await _helgaChatService.Recruit(request);

            Assert.True(_helgaChatService.TryGetResponse(history, out var recruitment));
            Assert.Equal(RecruitingStatus.Ready, recruitment.Status);
            Assert.StartsWith("supervisor-", recruitment.AgentToRecruit.AgentId);
            Assert.StartsWith(response.AgentId, recruitment.AgentToRecruit.SupervisorId);
        }
    }

    private static Vision GetVision()
    {
        const string vision =
            """
            {
              "threadId" : "019f8f0291f3",
              "agentId" : "leo",
              "userVision" : "Create a new Dungeons & Dragons storytelling tool that assists game masters by providing dynamic plot development, character generation, and real-time game management features.",
              "scopes" : [ {
                "supervisorId" : "supervisor-entertainment",
                "message" : "Develop a Dungeons & Dragons storytelling tool focusing on dynamic plot development, character generation, and real-time game management capabilities. Ensure it enhances the experience for game masters and integrates smoothly into existing gameplay."
              } ]
            }
            """;
        var options = new JsonSerializerOptions().ConfigureJsonSerialization();
        var response = JsonSerializer.Deserialize<Vision>(vision, options);
        Assert.NotNull(response);
        return response;
    }
}