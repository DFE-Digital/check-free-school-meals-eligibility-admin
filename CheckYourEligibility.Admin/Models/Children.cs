namespace CheckYourEligibility.Admin.Models;

public class Children
{
    public List<Child> ChildList { get; set; }

    // Parent/guardian and outcome details, threaded through hidden form fields (not Session/TempData)
    // so concurrent checks in other tabs cannot overwrite them. See ELIG-3594.
    public string? ParentFirstName { get; set; }
    public string? ParentLastName { get; set; }
    public string? ParentDateOfBirth { get; set; }
    public string? ParentEmail { get; set; }
    public string? ParentNino { get; set; }
    public string? ParentNass { get; set; }
    public string? Status { get; set; }
    public string? Tier { get; set; }
    public string? EligibilityEndDate { get; set; }
}