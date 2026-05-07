using Microsoft.EntityFrameworkCore;
using Mosaik.Core.Domain;
using Mosaik.Core.Workflow;
using Mosaik.Models;
using Mosaik.Services;

namespace Mosaik.Tests;

// Plan 16.5 Faz A — ApprovalService (generic onay isteği + sıralı adım + karar uygulama).
public class ApprovalServiceTests
{
    private static MosaikContext NewContext(string name)
    {
        var options = new DbContextOptionsBuilder<MosaikContext>()
            .UseInMemoryDatabase(databaseName: name + "_" + Guid.NewGuid())
            .Options;
        return new MosaikContext(options);
    }

    private static IEnumerable<(int order, int? userId, string? role)> TwoSteps() =>
        new[]
        {
            (1, (int?)10, (string?)null),
            (2, (int?)20, (string?)null)
        };

    [Fact]
    public async Task CreateAsync_TwoSteps_PendingRequestWithSortedSteps()
    {
        await using var ctx = NewContext(nameof(CreateAsync_TwoSteps_PendingRequestWithSortedSteps));
        var svc = new ApprovalService(ctx);

        var result = await svc.CreateAsync("Tamim", 5, "Onay isteği", TwoSteps(), "admin");

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Data);
        Assert.Equal(ApprovalStatus.Pending, result.Data!.Status);
        Assert.Equal(2, result.Data.Steps.Count);
        Assert.Equal(1, result.Data.Steps[0].StepOrder);
        Assert.Equal(2, result.Data.Steps[1].StepOrder);
    }

    [Fact]
    public async Task CreateAsync_EmptySteps_ReturnsFailure()
    {
        await using var ctx = NewContext(nameof(CreateAsync_EmptySteps_ReturnsFailure));
        var svc = new ApprovalService(ctx);

        var result = await svc.CreateAsync("Tamim", 5, "Subject",
            Array.Empty<(int, int?, string?)>(), "admin");

        Assert.False(result.IsSuccess);
        Assert.Contains("onay adımı", result.Message);
    }

    [Fact]
    public async Task GetCurrentStepAsync_ReturnsMinPendingStepOrder()
    {
        await using var ctx = NewContext(nameof(GetCurrentStepAsync_ReturnsMinPendingStepOrder));
        var svc = new ApprovalService(ctx);

        var create = await svc.CreateAsync("Tamim", 1, null, TwoSteps(), "admin");
        var current = await svc.GetCurrentStepAsync(create.Data!.Id);

        Assert.NotNull(current);
        Assert.Equal(1, current!.StepOrder);
    }

    [Fact]
    public async Task DecideAsync_FirstStepApproved_RequestStillPending_SecondStepCurrent()
    {
        await using var ctx = NewContext(nameof(DecideAsync_FirstStepApproved_RequestStillPending_SecondStepCurrent));
        var svc = new ApprovalService(ctx);

        var create = await svc.CreateAsync("Tamim", 1, null, TwoSteps(), "admin");
        var first = await svc.GetCurrentStepAsync(create.Data!.Id);

        var decision = await svc.DecideAsync(first!.Id, ApprovalStatus.Approved, 10, "OK");
        Assert.True(decision.IsSuccess);

        var request = await ctx.ApprovalRequests.FindAsync(create.Data.Id);
        Assert.Equal(ApprovalStatus.Pending, request!.Status); // henüz 2. adım var

        var nextStep = await svc.GetCurrentStepAsync(create.Data.Id);
        Assert.Equal(2, nextStep!.StepOrder);
    }

    [Fact]
    public async Task DecideAsync_LastStepApproved_RequestApproved()
    {
        await using var ctx = NewContext(nameof(DecideAsync_LastStepApproved_RequestApproved));
        var svc = new ApprovalService(ctx);

        var create = await svc.CreateAsync("Tamim", 1, null, TwoSteps(), "admin");
        var first = await svc.GetCurrentStepAsync(create.Data!.Id);
        await svc.DecideAsync(first!.Id, ApprovalStatus.Approved, 10);

        var second = await svc.GetCurrentStepAsync(create.Data.Id);
        await svc.DecideAsync(second!.Id, ApprovalStatus.Approved, 20);

        var request = await ctx.ApprovalRequests.FindAsync(create.Data.Id);
        Assert.Equal(ApprovalStatus.Approved, request!.Status);
        Assert.NotNull(request.CompletedAt);
    }

    [Fact]
    public async Task DecideAsync_FirstStepRejected_RequestRejected_RemainingSkipped()
    {
        await using var ctx = NewContext(nameof(DecideAsync_FirstStepRejected_RequestRejected_RemainingSkipped));
        var svc = new ApprovalService(ctx);

        var create = await svc.CreateAsync("Tamim", 1, null, TwoSteps(), "admin");
        var first = await svc.GetCurrentStepAsync(create.Data!.Id);

        await svc.DecideAsync(first!.Id, ApprovalStatus.Rejected, 10, "uygun değil");

        var request = await ctx.ApprovalRequests.FindAsync(create.Data.Id);
        Assert.Equal(ApprovalStatus.Rejected, request!.Status);
        Assert.NotNull(request.CompletedAt);
    }

    [Fact]
    public async Task DecideAsync_AlreadyDecidedStep_ReturnsFailure()
    {
        await using var ctx = NewContext(nameof(DecideAsync_AlreadyDecidedStep_ReturnsFailure));
        var svc = new ApprovalService(ctx);

        var create = await svc.CreateAsync("Tamim", 1, null, TwoSteps(), "admin");
        var first = await svc.GetCurrentStepAsync(create.Data!.Id);
        await svc.DecideAsync(first!.Id, ApprovalStatus.Approved, 10);

        var retry = await svc.DecideAsync(first.Id, ApprovalStatus.Approved, 11);
        Assert.False(retry.IsSuccess);
        Assert.Contains("karara", retry.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetPendingForUserAsync_ReturnsOnlyCurrentStepForUser()
    {
        await using var ctx = NewContext(nameof(GetPendingForUserAsync_ReturnsOnlyCurrentStepForUser));
        var svc = new ApprovalService(ctx);

        // 2 ayrı request, 1. user 10 hem ilk adım, 2. user 20 ikinci adım
        await svc.CreateAsync("Tamim", 1, null, TwoSteps(), "admin");
        await svc.CreateAsync("Tamim", 2, null, TwoSteps(), "admin");

        var pendingFor10 = await svc.GetPendingForUserAsync(10, Array.Empty<string>());
        var pendingFor20 = await svc.GetPendingForUserAsync(20, Array.Empty<string>());

        // user 10 ilk adımda — 2 request'te de görünmeli
        Assert.Equal(2, pendingFor10.Count);
        Assert.All(pendingFor10, s => Assert.Equal(1, s.StepOrder));

        // user 20 ikinci adımda — henüz sıra gelmedi (1. step pending)
        Assert.Empty(pendingFor20);
    }
}
