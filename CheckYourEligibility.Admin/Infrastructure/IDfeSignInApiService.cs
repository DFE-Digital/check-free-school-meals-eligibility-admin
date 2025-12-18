using CheckYourEligibility.Admin.Domain.DfeSignIn;

namespace CheckYourEligibility.Admin.Infrastructure;

/// <summary>
///     Service for interacting with the DfE Sign-in public API.
/// </summary>
public interface IDfeSignInApiService
{
    /// <summary>
    ///     Gets the roles for a user in a specific organisation.
    /// </summary>
    /// <param name="userId">The user's ID from DfE Sign-in.</param>
    /// <param name="organisationId">The organisation's ID from DfE Sign-in.</param>
    /// <returns>A list of roles assigned to the user for this service.</returns>
    Task<IList<Role>> GetUserRolesAsync(string userId, Guid organisationId);
}
