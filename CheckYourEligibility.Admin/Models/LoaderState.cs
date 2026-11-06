namespace CheckYourEligibility.Admin.Models;

// Carries the check reference and parent details for the client's next poll, instead of relying on
// TempData/Session (shared across all tabs of a browser). See ELIG-3594.
public class LoaderState
{
    public string ResponseJson { get; set; }
    public string ParentGuardianJson { get; set; }
}
