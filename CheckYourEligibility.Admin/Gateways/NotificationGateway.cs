﻿using CheckYourEligibility.Admin.Boundary.Requests;
using CheckYourEligibility.Admin.Boundary.Responses;
using CheckYourEligibility.Admin.Gateways.Interfaces;
using System.Net.Http;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CheckYourEligibility.Admin.Gateways
{
    public class NotificationGateway : BaseGateway, INotificationGateway
    {
        private readonly string _NotificationSendUrl;
        private readonly HttpClient _httpClient;
        private readonly ILogger _logger;
        protected readonly IHttpContextAccessor _httpContextAccessor;


        public NotificationGateway(ILoggerFactory logger, HttpClient httpClient, IConfiguration configuration, IHttpContextAccessor httpContextAccessor) : base("EcsService", logger, httpClient, configuration, httpContextAccessor)
        {
            _NotificationSendUrl = "Notification";
            _httpClient = httpClient;
            _logger = logger.CreateLogger("EcsService");
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<NotificationItemResponse> SendNotification(NotificationRequest notificationRequest)
        {

            try
            {
                var response = await ApiDataPostAsynch(_NotificationSendUrl, notificationRequest, new NotificationItemResponse());
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Send Notification failed. uri-{_httpClient.BaseAddress}{_NotificationSendUrl}");
                throw;
            }
        }
    }
}
