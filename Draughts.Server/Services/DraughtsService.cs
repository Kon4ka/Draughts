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

        private enum State
        {
            Play,
            Wait
        }
        private sealed class DraughtsCallbackServiceClient
        {

            private readonly DraughtsCallbackService.DraughtsCallbackServiceClient _client;
            private byte[] _password;
            public State Status;

            public DraughtsCallbackServiceClient(string address, byte[] pass)
            {
                var unsafeHandler = new HttpClientHandler();
                unsafeHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                var clientChannel = GrpcChannel.ForAddress($"http://{address}", new GrpcChannelOptions
                {
                    HttpHandler = unsafeHandler
                });

                _client = new DraughtsCallbackService.DraughtsCallbackServiceClient(clientChannel);
                _password = pass;
                Status = State.Wait;
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
            public async Task<bool> NewRandomGameAnotherAsync(string message, CancellationToken token = default)
            {
                var answer = await _client.NewRandomGameAnotherAsync(new IdentificationRequestCallback
                {
                    Message = message
                });

                return answer.Yesorno;

            }
            // TODO: add other requests...

        }

        class Pair
        {
            DraughtsCallbackServiceClient one;
            DraughtsCallbackServiceClient two;

            public Pair(DraughtsCallbackServiceClient o, DraughtsCallbackServiceClient t)
            {
                one = o;
                two = t;
            }
        }

        #endregion

        private readonly ILogger<DraughtsServiceImpl> _logger;
        private readonly Dictionary<string, DraughtsCallbackServiceClient> _activeUsers;
        private readonly Dictionary<string, Pair> _games;
        private readonly List<uint> _ports;
        public DraughtsServiceImpl(ILogger<DraughtsServiceImpl> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeUsers = new Dictionary<string, DraughtsCallbackServiceClient>();
            _ports = new List<uint>();
            _games = new Dictionary<string, Pair>();
        }

        public override Task<IdentificationResponse> IsPortFree(IsFree request, ServerCallContext context)
        {
            if (_ports.Count == 0 || !_ports.Contains(request.Port))
            {
                return Task.FromResult(new IdentificationResponse
                {
                    Success = true
                });
            }
            else
            {
                return Task.FromResult(new IdentificationResponse
                {
                    Success = false
                });
            }
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
            _activeUsers.Add(request.Name, new DraughtsCallbackServiceClient(request.Address, request.Password.ToByteArray()));
            var strs = request.Address.Split(":");
            _ports.Add(Convert.ToUInt32(strs[1]));

            var succeeded = await _activeUsers[request.Name].IdentificationCallbackAsync($"Welcome, {request.Name}!");

            return new IdentificationResponse
            {
                Success = true  //TODO: Remove mock
            };
        }

        public async override Task<IdentName> NewRandomGame(IdentName request, ServerCallContext context)
        {
            if (_activeUsers.Count == 0)
            {
                return new IdentName
                {
                    Name = "-1"     //Nobody at server
                };
            }
            string anotherName = "";
            DraughtsCallbackServiceClient another = null;
            foreach (var client in _activeUsers)
            {
                if (client.Value.Status == State.Wait && client.Key == request.Name)
                {
                    another = client.Value;
                    anotherName = client.Key;
                }
            }

            if (another is null)
            {
                return new IdentName
                {
                    Name = "-2" //All Plays
                };
            }
            var agree = await another.NewRandomGameAnotherAsync(request.Name);

            if (agree)
            {
                _games[Guid.NewGuid().ToString("N")] = new Pair(_activeUsers[request.Name], another);
                return new IdentName
                {
                    Name = anotherName
                };
            }
            else
            {
                return new IdentName
                {
                    Name = "-3"         //Dissagree to play
                };
            }

/*            return new IdentName
            {
            };*/

        }
    }
}
