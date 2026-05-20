using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Mosaik.Core.Notification;
using Mosaik.Core.Workflow;
using Mosaik.Models;
using Mosaik.Models.Workflow;
using Mosaik.Services;
using Mosaik.Services.Workflow;

namespace Mosaik.Tests;

// Plan 36 W-09 + W-11 — WorkflowStepProcessor escalation unit tests.
public class WorkflowStepProcessorTests
{
    private static MosaikContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<MosaikContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        return new MosaikContext(options);
    }

    private static NotificationService NewNotificationService(MosaikContext ctx) =>
        new NotificationService(ctx, NullLogger<NotificationService>.Instance);

    private static WorkflowStepProcessor NewProcessor(MosaikContext ctx) =>
        new WorkflowStepProcessor(ctx, NewNotificationService(ctx), NullLogger<WorkflowStepProcessor>.Instance);

    private static string OneStepDefinition(int deadlineDays, object? escalateTo = null)
    {
        var props = new Dictionary<string, object?>
        {
            ["name"] = "İlk Adım",
            ["deadlineDays"] = deadlineDays
        };
        if (escalateTo is not null) props["escalateTo"] = escalateTo;
        return JsonSerializer.Serialize(new
        {
            steps = new[]
            {
                new { id = "s1", name = "İlk Adım", type = "approval", properties = props }
            }
        });
    }

    private static async Task<(WorkflowTemplate template, WorkflowInstance instance)> SeedActiveAsync(
        MosaikContext ctx, int deadlineDays, object? escalateTo, DateTime stepEnteredAt)
    {
        var template = new WorkflowTemplate
        {
            FirmaId = 1,
            Name = "Test",
            EntityType = "Contract",
            DefinitionJson = OneStepDefinition(deadlineDays, escalateTo),
            IsActive = true,
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };
        ctx.WorkflowTemplates.Add(template);
        await ctx.SaveChangesAsync();

        var instance = new WorkflowInstance
        {
            FirmaId = 1,
            TemplateId = template.Id,
            EntityType = "Contract",
            EntityId = 7,
            CurrentStepId = "s1",
            Status = WorkflowInstanceStatus.Active,
            StartedAt = stepEnteredAt,
            StartedBy = 10
        };
        ctx.WorkflowInstances.Add(instance);
        await ctx.SaveChangesAsync();

        ctx.WorkflowInstanceLogs.Add(new WorkflowInstanceLog
        {
            InstanceId = instance.Id,
            EventType = WorkflowEventType.InstanceStarted,
            ActorId = 10,
            OccurredAt = stepEnteredAt
        });
        ctx.WorkflowInstanceLogs.Add(new WorkflowInstanceLog
        {
            InstanceId = instance.Id,
            StepId = "s1",
            EventType = WorkflowEventType.StepEntered,
            ActorId = 10,
            OccurredAt = stepEnteredAt
        });
        await ctx.SaveChangesAsync();
        return (template, instance);
    }

    [Fact]
    public async Task ExecuteAsync_WithinDeadline_NoEscalation()
    {
        await using var ctx = NewContext(nameof(ExecuteAsync_WithinDeadline_NoEscalation));
        // Adım 3 gün önce açıldı, deadline 5 gün → henüz süre dolmadı
        await SeedActiveAsync(ctx, deadlineDays: 5, escalateTo: 99, stepEnteredAt: DateTime.UtcNow.AddDays(-3));
        var processor = NewProcessor(ctx);

        await processor.ExecuteAsync();

        Assert.DoesNotContain(ctx.WorkflowInstanceLogs, l => l.EventType == WorkflowEventType.EscalationFired);
    }

    [Fact]
    public async Task ExecuteAsync_DeadlinePassedPlusOneDay_FiresEscalation()
    {
        await using var ctx = NewContext(nameof(ExecuteAsync_DeadlinePassedPlusOneDay_FiresEscalation));
        // 5 gün deadline, 7 gün önce başladı → 7 - 5 = 2 gün gecikti → escalate
        await SeedActiveAsync(ctx, deadlineDays: 5, escalateTo: 99, stepEnteredAt: DateTime.UtcNow.AddDays(-7));
        var processor = NewProcessor(ctx);

        await processor.ExecuteAsync();

        Assert.Contains(ctx.WorkflowInstanceLogs, l =>
            l.EventType == WorkflowEventType.EscalationFired && l.StepId == "s1");
    }

    [Fact]
    public async Task ExecuteAsync_AlreadyEscalated_NoSecondLog()
    {
        await using var ctx = NewContext(nameof(ExecuteAsync_AlreadyEscalated_NoSecondLog));
        await SeedActiveAsync(ctx, deadlineDays: 5, escalateTo: 99, stepEnteredAt: DateTime.UtcNow.AddDays(-7));
        var processor = NewProcessor(ctx);

        await processor.ExecuteAsync();
        await processor.ExecuteAsync(); // ikinci çağrı

        var escalations = ctx.WorkflowInstanceLogs.Where(l => l.EventType == WorkflowEventType.EscalationFired).ToList();
        Assert.Single(escalations);
    }

    [Fact]
    public async Task ExecuteAsync_NoDeadline_SkipsInstance()
    {
        await using var ctx = NewContext(nameof(ExecuteAsync_NoDeadline_SkipsInstance));
        // deadlineDays=0 → skip
        await SeedActiveAsync(ctx, deadlineDays: 0, escalateTo: 99, stepEnteredAt: DateTime.UtcNow.AddDays(-100));
        var processor = NewProcessor(ctx);

        await processor.ExecuteAsync();

        Assert.DoesNotContain(ctx.WorkflowInstanceLogs, l => l.EventType == WorkflowEventType.EscalationFired);
    }

    [Fact]
    public async Task ExecuteAsync_EscalateToUserId_NotifiesThatUser()
    {
        await using var ctx = NewContext(nameof(ExecuteAsync_EscalateToUserId_NotifiesThatUser));
        await SeedActiveAsync(ctx, deadlineDays: 1, escalateTo: 99, stepEnteredAt: DateTime.UtcNow.AddDays(-5));
        var processor = NewProcessor(ctx);

        await processor.ExecuteAsync();

        var notifications = await ctx.Notifications
            .Where(n => n.NotificationType == "workflow_escalation")
            .ToListAsync();
        Assert.Contains(notifications, n => n.UserId == 99);
    }

    [Fact]
    public async Task ExecuteAsync_CancelledInstance_SkipsEscalation()
    {
        await using var ctx = NewContext(nameof(ExecuteAsync_CancelledInstance_SkipsEscalation));
        var (_, instance) = await SeedActiveAsync(ctx, deadlineDays: 1, escalateTo: 99, stepEnteredAt: DateTime.UtcNow.AddDays(-5));
        instance.Status = WorkflowInstanceStatus.Cancelled;
        instance.CompletedAt = DateTime.UtcNow;
        await ctx.SaveChangesAsync();
        var processor = NewProcessor(ctx);

        await processor.ExecuteAsync();

        Assert.DoesNotContain(ctx.WorkflowInstanceLogs, l => l.EventType == WorkflowEventType.EscalationFired);
    }
}
