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
using System.Security.Cryptography;
//using System.Windows.Forms;

namespace ClientApp.VM
{
    internal sealed class ViewM1 : VMBase
    {
        private ICommand _curCommand;
        private bool _canExecute = true;
        private bool _identification_success = false;

        private readonly DraughtsCallbackServiceImpl _callback;
        public delegate bool MessageB(string mess);
        public static event MessageB ShowMess;

        GrpcChannel channel;
        DraughtsService.DraughtsServiceClient client;
        private string _authStatus = "Авторизуйся плиз";
        private string _login = "User1";
        private string _password = "sobaka";
        private int port;
        private MD5 md;
        private Random rd;

        internal ViewM1(DraughtsCallbackServiceImpl callback)
        {
            channel = GrpcChannel.ForAddress("https://localhost:5001");
            client = new DraughtsService.DraughtsServiceClient(channel);
            AuthStatus = "Авторизуйся плиз";

            _callback = callback ?? throw new ArgumentNullException(nameof(callback));

            callback.Identified += message =>
            {
                AuthStatus = message;
            };
            callback.AgreeForNewGame += Callback_AgreeForNewGame;
            md = MD5.Create();
            rd = new Random();
            _ = IdentificationSendAsync();
            port = 5050 + (rd.Next() % 10);

        }

        private bool Callback_AgreeForNewGame(string obj)
        {
            return ShowMess("Хотите сыграть с" + (string)obj);
        }

        #region Fields
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

        public string Login
        {
            get =>
                _login;

            private set
            {
                _login = value;
                OnPropertyChanged(nameof(Login));
            }
        }
        public string Password
        {
            get =>
                _password;

            private set
            {
                _password = value;
                OnPropertyChanged(nameof(_password));
            }
        }

        #endregion

        public ICommand IdentificationCommand =>
        _curCommand ??= new Relay(async _ => await NewRandomGame(), _ => CanExecute);


        private async Task IdentificationSendAsync(CancellationToken token = default)
        {/*
            using GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:5001");
            DraughtsService.DraughtsServiceClient client = new DraughtsService.DraughtsServiceClient(channel);*/
            bool isFree = false;
            while (!isFree)
            {
                port = 5050 + (rd.Next() % 10);
                var res = await client.IsPortFreeAsync(new IsFree
                {
                    Port = (uint)port
                });
                isFree = res.Success;
            }
            //Logic
            var reply = await client.IdentificationAsync(new IdentificationRequest
            {
                Name = _login, 
                Password = ByteString.CopyFrom(md.ComputeHash(Encoding.Default.GetBytes(_login+_password))),
                Address = $"127.0.0.1:{port}"
            });
            IdRes = reply.Success;
        }

        private async Task NewRandomGame()
        {
            var reply = await client.NewRandomGameAsync(new IdentName
            {
                Name = _login
            });

            switch(reply.Name)
            {
                case "-1": MessageBox.Show("Вы один"); break;
                case ("-2"): MessageBox.Show("Все заняты"); break;
                case ("-3"): MessageBox.Show("Игрок отказался"); break;
                default:  MessageBox.Show("Начинаем..."); break;
            }

        }
    }
}
