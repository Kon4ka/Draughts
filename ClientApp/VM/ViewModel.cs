using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using RelayNS;
using System.Net.Http;
using Grpc.Net.Client;
using Google.Protobuf;
using Draughts.Grpc;
using System.Threading;
using ClientApp.Grpc;
using System.Windows;
//using Draughts;

namespace ClientApp.VM
{
    internal sealed class ViewM1 : VMBase
    {
        private ICommand _curCommand;
        private bool _canExecute = true;
        private bool _identification_success = false;
        private readonly DraughtsCallbackServiceImpl _callback;
        private string _authStatus = "Авторизуйся плиз";

        internal ViewM1(DraughtsCallbackServiceImpl callback)
        {
            AuthStatus = "Авторизуйся плиз";

            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            callback.Identified += message =>
            {
                AuthStatus = message;
            };
        }

        public string AuthStatus
        {
            get =>
                _authStatus;

            private set
            {
                _authStatus = value;
                OnPropertyChanged(nameof(AuthStatus));
            }
        }

        public bool CanExecute
        {
            get =>
                _canExecute;

            private set
            {
                _canExecute = value;
                OnPropertyChanged(nameof(CanExecute));
            }
        }

        public bool IdRes
        {
            get =>
                _identification_success;

            private set
            {
                _identification_success = value;
                OnPropertyChanged(nameof(IdRes));
            }
        }

        public ICommand IdentificationCommand =>
        _curCommand ??= new Relay(async _ => await IdentificationSendAsync(), _ => CanExecute);


        private async Task IdentificationSendAsync(CancellationToken token = default)
        {
            //Logic
            using var channel = GrpcChannel.ForAddress("https://localhost:5001");
            var client = new DraughtsService.DraughtsServiceClient(channel);
            var reply = await client.IdentificationAsync(new IdentificationRequest
            {
                Name = "Me", 
                Password = ByteString.CopyFrom(Encoding.Default.GetBytes("111")),
                Address = "127.0.0.1:5055"
            });
            IdRes = reply.Success;
        }
    }
}
