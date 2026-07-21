public class ApplicationConstants
{
    public static Dictionary<string, string> StatusFilters = new()
    {
        { "Entitled", "Eligible (2025-2026)" },
        { "Entitled.targeted", "Eligible targeted" },
        { "Entitled.expanded", "Eligible expanded" },
        { "EvidenceNeeded", "Evidence needed" },
        { "Receiving", "Receiving entitlement (2025-2026)" },
        { "Receiving.targeted", "Receiving targeted FSM" },
        { "Receiving.expanded", "Receiving expanded FSM" },
        { "SentForReview", "Sent for review" },
        { "ReviewedEntitled", "Reviewed entitled (2025-2026)" },
        { "ReviewedEntitled.targeted", "Reviewed entitled targeted" },
        { "ReviewedEntitled.expanded", "Reviewed entitled expanded" },
        { "ReviewedNotEntitled", "Reviewed not entitled" },
        { "Archived", "Archived" },
    };
}
