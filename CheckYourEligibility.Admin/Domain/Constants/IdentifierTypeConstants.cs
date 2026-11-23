namespace CheckYourEligibility.Admin.Domain.Constants
{
    public class IdentifierTypeConstants
    {
        public const string Nass = "nass";

        public const string Nino = "nino";

        internal static bool ShouldValidateNass(string checkIdentifierType)
        {
            if (string.Equals(checkIdentifierType, Nass, StringComparison.OrdinalIgnoreCase)) return true;

            return false;
        }
    }
}