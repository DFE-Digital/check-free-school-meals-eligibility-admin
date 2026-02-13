using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Gateways.Interfaces;

namespace CheckYourEligibility.Admin.Usecases
{
    public interface IDeleteBulkCheckFileUseCase_FsmBasic
    {
        Task<CheckEligiblityBulkDeleteResponse> Execute(string bulkCheckId);
    }

    public class DeleteBulkCheckFileUseCase_FsmBasic : IDeleteBulkCheckFileUseCase_FsmBasic
    {
        private readonly ICheckGateway _checkGateway;
        private readonly ILogger<DeleteBulkCheckFileUseCase_FsmBasic> _logger;

        public DeleteBulkCheckFileUseCase_FsmBasic(
            ILogger<DeleteBulkCheckFileUseCase_FsmBasic> logger,
            ICheckGateway checkGateway)
        {
            _logger = logger;
            _checkGateway = checkGateway;
        }

        public async Task<CheckEligiblityBulkDeleteResponse> Execute(string bulkCheckId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(bulkCheckId))
                {
                    _logger.LogWarning("Attempted to delete bulk check with empty ID");
                    return new CheckEligiblityBulkDeleteResponse
                    {
                        Success = false,
                        Message = "Invalid bulk check ID"
                    };
                }

                var deleteUrl = $"bulk-check/{bulkCheckId}";
                var response = await _checkGateway.DeleteBulkChecksFor_FsmBasic(deleteUrl);

                if (response.Success)
                {
                    _logger.LogInformation("Successfully deleted bulk check: {BulkCheckId}", bulkCheckId);
                }
                else
                {
                    _logger.LogWarning("Failed to delete bulk check: {BulkCheckId}. Message: {Message}", 
                        bulkCheckId, response.Message);
                }

                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting bulk check: {BulkCheckId}", bulkCheckId);
                return new CheckEligiblityBulkDeleteResponse
                {
                    Success = false,
                    Message = $"Error deleting bulk check: {ex.Message}"
                };
            }
        }
    }
}
