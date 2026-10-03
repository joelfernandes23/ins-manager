using InsManager.Core.Services;

namespace InsManager.Tests;

public sealed class InsSlotPlannerTests
{
    [Fact]
    public void RefillPlanWrapsSlotsAfterEightToNine()
    {
        var plan = InsSlotPlanner.BuildRefillPlan(8, 9, 8, 20);

        Assert.Equal(
            Enumerable.Range(1, 7),
            plan.Select(assignment => assignment.SlotNumber));
        Assert.Equal(
            Enumerable.Range(9, 7),
            plan.Select(assignment => assignment.RouteIndex));
    }

    [Fact]
    public void RefillPlanContinuesAfterNineToOne()
    {
        var plan = InsSlotPlanner.BuildRefillPlan(9, 1, 9, 20);

        Assert.Equal(
            Enumerable.Range(2, 7),
            plan.Select(assignment => assignment.SlotNumber));
        Assert.Equal(
            Enumerable.Range(10, 7),
            plan.Select(assignment => assignment.RouteIndex));
    }

    [Fact]
    public void RefillPlanStopsAtEndOfRoute()
    {
        var plan = InsSlotPlanner.BuildRefillPlan(8, 9, 8, 11);

        Assert.Collection(
            plan,
            assignment => Assert.Equal(new InsSlotAssignment(1, 9), assignment),
            assignment => Assert.Equal(new InsSlotAssignment(2, 10), assignment));
    }
}
