using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Module.AI.Chat;
using Module.AI.Persistence;
using Xunit;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace Service.Integration;

public class SupervisorWorkflows : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly SupervisorChatService _supervisorChatService;

    public SupervisorWorkflows(WebApplicationFactory<Program> factory)
    {
        var serviceProvider = factory.Services.CreateScope().ServiceProvider;
        _supervisorChatService = serviceProvider.GetRequiredService<SupervisorChatService>();
    }

    [Fact]
    public async Task DefineEmployees_VisionProvided_RecruitSupervisor()
    {
        var onboarding = GetSupervisorOnboarding();
        var supervisor = onboarding.Agent;
        var analyzeTask = new AnalyzeTask(onboarding.ThreadId, supervisor.SupervisorId, supervisor.AgentId,
            supervisor.SystemPrompt);
        var chatHistory = await _supervisorChatService.DefineEmployees(analyzeTask);

        Assert.True(_supervisorChatService.TryGetResponse(chatHistory, out var response));
        Assert.InRange(response.Length, 1, 10);
    }

    private Onboarding GetSupervisorOnboarding()
    {
        const string supervisor =
            """
            {
              "threadId" : "019f8f0291f3",
              "status" : "READY",
              "agentToRecruit" : {
                "agentId" : "supervisor-entertainment",
                "jobTitle" : "Entertainment Supervisor",
                "jobDescription" : "Leads the development of tools and systems for immersive entertainment experiences, focusing on dynamic storytelling, character creation, and game management. Responsible for enhancing user engagement and ensuring smooth integration with existing platforms.",
                "department" : "OPERATIONS",
                "systemPrompt" : "You are the Entertainment Supervisor, specializing in the creation and integration of dynamic storytelling and game management tools for Dungeons & Dragons. Your leadership in the development of innovative features enhances the immersive experience for game masters and players alike.",
                "guardrails" : [ ],
                "supervisorId" : "leo"
              }
            }
            """;
        var options = new JsonSerializerOptions().ConfigureJsonSerialization();
        var recruitment = JsonSerializer.Deserialize<Recruitment>(supervisor, options);
        Assert.NotNull(recruitment);
        var toRecruit = recruitment.AgentToRecruit;
        var agent = new Agent
        {
            AgentId = toRecruit.AgentId,
            Name = "",
            PublicKeyHex = "",
            JobTitle = toRecruit.JobTitle,
            JobDescription = toRecruit.JobDescription,
            SystemPrompt = toRecruit.SystemPrompt,
            PrivateKeyHex = "",
            Department = toRecruit.Department.ToString(),
            SupervisorId = toRecruit.SupervisorId,
            Guardrails = toRecruit.Guardrails,
            Status = AgentStatus.Onboarding
        };

        return new Onboarding(recruitment.ThreadId, agent);
    }
}