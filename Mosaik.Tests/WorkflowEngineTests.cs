using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.Workflow;
using Mosaik.Models;
using Mosaik.Models.Workflow;
using Mosaik.Services.Workflow;

namespace Mosaik.Tests;

// Plan 36 Faz A — WorkflowEngine event sourcing unit tests.
public class WorkflowEngineTests
{
    private static MosaikContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<MosaikContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        return new MosaikContext(options);
    }

    private static string ThreeStepDefinition() => JsonSerializer.Serialize(new
    {
        steps = new[]
        {
            new { id = "s1", name = "Yönetici Onayı", type = "task" },
            new { id = "s2", name = "Mali İşler", type = "task" },
            new { id = "s3", name = "Genel Müdür", type = "task" }
        }
    });

    private static async Task<WorkflowTemplate> SeedTemplate(MosaikContext ctx, string? definition = null)
    {
        var template = new WorkflowTemplate
        {
            FirmaId = 1,
            Name = "Sözleşme Onay Zinciri",
            EntityType = "Contract",
            DefinitionJson = definition ?? ThreeStepDefinition(),
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        ctx.WorkflowTemplates.Add(template);
        await ctx.SaveChangesAsync();
        return template;
    }

    private static WorkflowStartInput SampleStart(int templateId) => new(
        FirmaId: 1,
        TemplateId: templateId,
        EntityType: "Contract",
        EntityId: 99,
        StartedBy: 10);

    [Fact]
    public async Task StartAsync_NewInstance_CreatesInstanceStartedAndStepEnteredLogs()
    {
        await using var ctx = NewContext(nameof(StartAsync_NewInstance_CreatesInstanceStartedAndStepEnteredLogs));
        var engine = new WorkflowEngine(ctx, NullLogger<WorkflowEngine>.Instance);
        var template = await SeedTemplate(ctx);

        var result = await engine.StartAsync(SampleStart(template.Id));

        Assert.True(result.IsSuccess);
        var instance = ctx.WorkflowInstances.Single();
        Assert.Equal("s1", instance.CurrentStepId);
        Assert.Equal(WorkflowInstanceStatus.Active, instance.Status);

        var logs = await ctx.WorkflowInstanceLogs.OrderBy(l => l.Id).ToListAsync();
        Assert.Equal(2, logs.Count);
        Assert.Equal(WorkflowEventType.InstanceStarted, logs[0].EventType);
        Assert.Equal(WorkflowEventType.StepEntered, logs[1].EventType);
        Assert.Equal("s1", logs[1].StepId);
    }

    [Fact]
    public async Task StartAsync_InactiveTemplate_ReturnsFailure()
    {
        await using var ctx = NewContext(nameof(StartAsync_InactiveTemplate_ReturnsFailure));
        var engine = new WorkflowEngine(ctx, NullLogger<WorkflowEngine>.Instance);
        var template = await SeedTemplate(ctx);
        template.IsActive = false;
        await ctx.SaveChangesAsync();

        var result = await engine.StartAsync(SampleStart(template.Id));

        Assert.False(result.IsSuccess);
        Assert.Equal("TEMPLATE_NOT_FOUND", result.ErrorCode);
    }

    [Fact]
    public async Task AdvanceAsync_Approve_MovesToNextStep()
    {
        await using var ctx = NewContext(nameof(AdvanceAsync_Approve_MovesToNextStep));
        var engine = new WorkflowEngine(ctx, NullLogger<WorkflowEngine>.Instance);
        var template = await SeedTemplate(ctx);
        var start = await engine.StartAsync(SampleStart(template.Id));

        var advance = await engine.AdvanceAsync(start.Data, new WorkflowAdvanceInput(ActorId: 20, Approved: true, Comment: "ok"));

        Assert.True(advance.IsSuccess);
        var instance = ctx.WorkflowInstances.Single();
        Assert.Equal("s2", instance.CurrentStepId);
        Assert.Equal(WorkflowInstanceStatus.Active, instance.Status);

        var logs = await ctx.WorkflowInstanceLogs.OrderBy(l => l.Id).ToListAsync();
        Assert.Contains(logs, l => l.EventType == WorkflowEventType.StepCompleted && l.StepId == "s1");
        Assert.Contains(logs, l => l.EventType == WorkflowEventType.StepEntered && l.StepId == "s2");
    }

    [Fact]
    public async Task AdvanceAsync_ApproveLastStep_CompletesInstance()
    {
        await using var ctx = NewContext(nameof(AdvanceAsync_ApproveLastStep_CompletesInstance));
        var engine = new WorkflowEngine(ctx, NullLogger<WorkflowEngine>.Instance);
        var template = await SeedTemplate(ctx);
        var start = await engine.StartAsync(SampleStart(template.Id));

        await engine.AdvanceAsync(start.Data, new WorkflowAdvanceInput(20, true));
        await engine.AdvanceAsync(start.Data, new WorkflowAdvanceInput(30, true));
        var finalAdvance = await engine.AdvanceAsync(start.Data, new WorkflowAdvanceInput(40, true));

        Assert.True(finalAdvance.IsSuccess);
        var instance = ctx.WorkflowInstances.Single();
        Assert.Equal(WorkflowInstanceStatus.Completed, instance.Status);
        Assert.Null(instance.CurrentStepId);
        Assert.NotNull(instance.CompletedAt);

        var logs = await ctx.WorkflowInstanceLogs.OrderBy(l => l.Id).ToListAsync();
        Assert.Contains(logs, l => l.EventType == WorkflowEventType.InstanceCompleted);
    }

    [Fact]
    public async Task AdvanceAsync_Reject_CancelsInstance()
    {
        await using var ctx = NewContext(nameof(AdvanceAsync_Reject_CancelsInstance));
        var engine = new WorkflowEngine(ctx, NullLogger<WorkflowEngine>.Instance);
        var template = await SeedTemplate(ctx);
        var start = await engine.StartAsync(SampleStart(template.Id));

        var advance = await engine.AdvanceAsync(start.Data,
            new WorkflowAdvanceInput(ActorId: 20, Approved: false, Comment: "uygun değil"));

        Assert.True(advance.IsSuccess);
        var instance = ctx.WorkflowInstances.Single();
        Assert.Equal(WorkflowInstanceStatus.Cancelled, instance.Status);
        Assert.NotNull(instance.CompletedAt);

        var logs = await ctx.WorkflowInstanceLogs.OrderBy(l => l.Id).ToListAsync();
        Assert.Contains(logs, l => l.EventType == WorkflowEventType.StepRejected);
        Assert.Contains(logs, l => l.EventType == WorkflowEventType.InstanceCancelled);
    }

    [Fact]
    public async Task AdvanceAsync_AlreadyCompleted_ReturnsFailure()
    {
        await using var ctx = NewContext(nameof(AdvanceAsync_AlreadyCompleted_ReturnsFailure));
        var engine = new WorkflowEngine(ctx, NullLogger<WorkflowEngine>.Instance);
        var template = await SeedTemplate(ctx);
        var start = await engine.StartAsync(SampleStart(template.Id));

        await engine.AdvanceAsync(start.Data, new WorkflowAdvanceInput(20, false));
        var second = await engine.AdvanceAsync(start.Data, new WorkflowAdvanceInput(30, true));

        Assert.False(second.IsSuccess);
        Assert.Equal("NOT_ACTIVE", second.ErrorCode);
    }

    [Fact]
    public async Task CancelAsync_ActiveInstance_LogsCancellation()
    {
        await using var ctx = NewContext(nameof(CancelAsync_ActiveInstance_LogsCancellation));
        var engine = new WorkflowEngine(ctx, NullLogger<WorkflowEngine>.Instance);
        var template = await SeedTemplate(ctx);
        var start = await engine.StartAsync(SampleStart(template.Id));

        var cancel = await engine.CancelAsync(start.Data, actorId: 50, reason: "iptal");

        Assert.True(cancel.IsSuccess);
        Assert.Equal(WorkflowInstanceStatus.Cancelled, ctx.WorkflowInstances.Single().Status);
        Assert.Contains(ctx.WorkflowInstanceLogs, l => l.EventType == WorkflowEventType.InstanceCancelled);
    }

    [Fact]
    public async Task GetLogsAsync_ReturnsLogsInChronologicalOrder()
    {
        await using var ctx = NewContext(nameof(GetLogsAsync_ReturnsLogsInChronologicalOrder));
        var engine = new WorkflowEngine(ctx, NullLogger<WorkflowEngine>.Instance);
        var template = await SeedTemplate(ctx);
        var start = await engine.StartAsync(SampleStart(template.Id));
        await engine.AdvanceAsync(start.Data, new WorkflowAdvanceInput(20, true));

        var logs = await engine.GetLogsAsync(start.Data);

        Assert.True(logs.Count >= 3);
        for (int i = 1; i < logs.Count; i++)
            Assert.True(logs[i].OccurredAt >= logs[i - 1].OccurredAt);
    }
}
