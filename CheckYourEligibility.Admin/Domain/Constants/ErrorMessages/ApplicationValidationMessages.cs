// Ignore Spelling: FSM

namespace CheckYourEligibility.Admin.Domain.Constants.ErrorMessages;

public static class ValidationMessages
{
    public const string FirstName = "Parent First Name is required";

    public const string LastName = "Parent Last Name is required";

    public const string DOB = "Date of birth is required:- (yyyy-mm-dd)";

    public const string InvalidNI = "Enter a National Insurance number in the correct format";

    public const string InvalidNASS = "Enter an asylum support reference number in the correct format";

    public const string NI_and_NASS = "Enter either a National Insurance number or asylum support reference number, not both";

    public const string NI_or_NASS = "National Insurance number or asylum support reference number is required";

    public const string ChildSchoolUrn = "Invalid Child School URN";

    public const string ChildFirstName = "Child First Name is required";

    public const string ChildLastName = "Child Last Name is required";

    public const string ChildDOB = "Child Date of birth is required:- (yyyy-mm-dd)";

    public const string InvalidSchoolUrnForLA = "School URN must be for a school in your local authority area";

    public const string InvalidSchoolUrnForMAT = "School URN must be for a school in your MAT";
}