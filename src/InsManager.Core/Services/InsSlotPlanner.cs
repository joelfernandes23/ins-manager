namespace InsManager.Core.Services;

public static class InsSlotPlanner
{
    public static IReadOnlyList<InsSlotAssignment> BuildRefillPlan(
        int fromSlot,
        int toSlot,
        int toRouteIndex,
        int routeCount)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(fromSlot, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(fromSlot, 9);
        ArgumentOutOfRangeException.ThrowIfLessThan(toSlot, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(toSlot, 9);
        ArgumentOutOfRangeException.ThrowIfLessThan(toRouteIndex, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(routeCount, 0);

        var assignments = new List<InsSlotAssignment>();
        var routeOffset = 1;

        for (var slotOffset = 1; slotOffset <= 8; slotOffset++)
        {
            var slotNumber = ((toSlot - 1 + slotOffset) % 9) + 1;
            if (slotNumber == fromSlot) continue;

            var routeIndex = toRouteIndex + routeOffset;
            if (routeIndex >= routeCount) break;

            assignments.Add(new InsSlotAssignment(slotNumber, routeIndex));
            routeOffset++;
        }

        return assignments;
    }
}

public sealed record InsSlotAssignment(int SlotNumber, int RouteIndex);
