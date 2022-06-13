using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Containers;
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
            public string _login;

            public DraughtsCallbackServiceClient(string address, byte[] pass, string login)
            {
                var unsafeHandler = new HttpClientHandler();
                unsafeHandler.ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;

                var clientChannel = GrpcChannel.ForAddress($"http://{address}", new GrpcChannelOptions
                {
                    HttpHandler = unsafeHandler
                });

                _login = login;
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

            public async Task TakeMoveFromAnotherAsync(string move, string guid, string name, CancellationToken token = default)
            {
                var answer = await _client.TakeMoveFromAnotherAsync(new OthersMove
                {
                    OtherMovement = move,
                    YourGameGuid = guid,
                    OtherName = name
                });
            }
                    // TODO: add other requests...

        }

        class Pair
        {
            public DraughtsCallbackServiceClient one;
            public DraughtsCallbackServiceClient two;

            public Pair(DraughtsCallbackServiceClient o, DraughtsCallbackServiceClient t)
            {
                one = o;
                two = t;
            }
        }
       
        #endregion

        private readonly ILogger<DraughtsServiceImpl> _logger;
        private readonly Dictionary<string, DraughtsCallbackServiceClient> _activeUsers;
        private readonly Dictionary<string, Pair> _games;                                   //Value - one - first, two - second
        private readonly Dictionary<string, Consignment> _consignment;
        private readonly List<uint> _ports;
        public DraughtsServiceImpl(ILogger<DraughtsServiceImpl> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _activeUsers = new Dictionary<string, DraughtsCallbackServiceClient>();
            _ports = new List<uint>();
            _games = new Dictionary<string, Pair>();
            _consignment = new Dictionary<string, Consignment>();
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
            _activeUsers.Add(request.Name, new DraughtsCallbackServiceClient(request.Address, request.Password.ToByteArray(), request.Name));
            var strs = request.Address.Split(":");
            _ports.Add(Convert.ToUInt32(strs[1]));

            var succeeded = await _activeUsers[request.Name].IdentificationCallbackAsync($"Welcome, {request.Name}!");

            return new IdentificationResponse
            {
                Success = true  //TODO: Remove mock
            };
        }

        public async override Task<GameInfo> NewRandomGame(IdentName request, ServerCallContext context)
        {
            if (_activeUsers.Count == 0)
            {
                return new GameInfo
                {
                    Opponent = "-1"     //Nobody at server
                };
            }
            string anotherName = "";
            DraughtsCallbackServiceClient another = null;
            foreach (var client in _activeUsers)
            {
                if (client.Value.Status == State.Wait && client.Key != request.Name)
                {
                    another = client.Value;
                    anotherName = client.Key;
                }
            }

            if (another is null)
            {
                return new GameInfo
                {
                    Opponent = "-2" //All Plays
                };
            }
            var agree = await another.NewRandomGameAnotherAsync(request.Name);

            if (agree)
            {
                var id = Guid.NewGuid().ToString("N");
                _games[id] = new Pair(_activeUsers[request.Name], another);
                _consignment[id] = new Consignment();
                return new GameInfo
                {
                    Opponent = anotherName,
                    Guid = id
                };
            }
            else
            {
                return new GameInfo
                {
                    Opponent = "-3"         //Dissagree to play
                };
            }
        }
        public async override Task<IsCorrect> SendAMoveToAnother(Move request, ServerCallContext context)
        {
            if (!ValidMove(request.Movement))
            {
                return new IsCorrect { Correct = "0"};
            }

            if (_games[request.GameGuid].one == _activeUsers[request.Name])
            {
                if (_consignment[request.GameGuid]._firstMoves.Count != _consignment[request.GameGuid]._secondMoves.Count)
                    return new IsCorrect
                    {
                        Correct = "-2"      //not yor part
                    };
                _consignment[request.GameGuid].AddMoveFirst(request.Movement);
                await _games[request.GameGuid].two.TakeMoveFromAnotherAsync(request.Movement, request.GameGuid, request.Name);
            }
            else
            {
                if (_consignment[request.GameGuid]._firstMoves.Count - 1 != _consignment[request.GameGuid]._secondMoves.Count)
                    return new IsCorrect
                    {
                        Correct = "-2"      //not yor part
                    };
                _consignment[request.GameGuid].AddMoveSecond(request.Movement);
                await _games[request.GameGuid].one.TakeMoveFromAnotherAsync(request.Movement, request.GameGuid, request.Name);
            }

            return new IsCorrect
            {
                Correct = "1"
            };
        }

        private bool ValidMove(string move)
        {
            return true;
        }

    }
}
