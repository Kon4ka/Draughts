using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;

using Draughts.Grpc;
using System.Threading;
using System;
using Grpc.Net.Client;
using System.Net.Http;

namespace Draughts.Server.Services
{
    public class DraughtsServiceImpl : DraughtsService.DraughtsServiceBase
    {

        private int _unsucceededAuths = 0;

        #region Nested

        private sealed class DraughtsCallbackServiceClient
        {

            private readonly DraughtsCallbackService.DraughtsCallbackServiceClient _client;
            
            public DraughtsCallbackServiceClient(string address)
            {
                var unsafeHandler = new HttpClientHandler();
                unsafeHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                var clientChannel = GrpcChannel.ForAddress($"http://{address}", new GrpcChannelOptions
                {
                    HttpHandler = unsafeHandler
                });

                _client = new DraughtsCallbackService.DraughtsCallbackServiceClient(clientChannel);
            }

            public async Task<bool> IdentificationCallbackAsync(string message, CancellationToken token = default)
            {
                try
                {
                    _ = await _client.IdentificationCallbackAsync(new IdentificationRequestCallback
                    {
                        Message = message
                    }, cancellationToken: token);

                    return true;
                }
                catch (Exception ex)
                {
                    // TODO: handle an exception

                    return false;
                }
            }

            // TODO: add other requests...

        }

        #endregion

        private readonly ILogger<DraughtsServiceImpl> _logger;
        private readonly Dictionary<string, DraughtsCallbackServiceClient> _activeUsers;
        public DraughtsServiceImpl(ILogger<DraughtsServiceImpl> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeUsers = new Dictionary<string, DraughtsCallbackServiceClient>();
        }

        public override async Task<IdentificationResponse> Identification(IdentificationRequest request, ServerCallContext context)
        {
            if (_activeUsers.ContainsKey(request.Name))
            {
                await _activeUsers[request.Name].IdentificationCallbackAsync($"You was already authenticated!!1!1 Auth id: {++_unsucceededAuths}");

                return new IdentificationResponse
                {
                    Success = false  //TODO: Remove mock
                };
            }
            _activeUsers.Add(request.Name, new DraughtsCallbackServiceClient(request.Address));

            var succeeded = await _activeUsers[request.Name].IdentificationCallbackAsync($"Welcome, {request.Name}!");

            return new IdentificationResponse
            {
                Success = true  //TODO: Remove mock
            };
        }
    }
}
