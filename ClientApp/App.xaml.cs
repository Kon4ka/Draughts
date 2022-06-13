using System;
using System.Windows;
using ClientApp.Grpc;
using ClientApp.VM;
using Draughts.Grpc;
using Grpc.Net.Client;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ClientApp
{

    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            GrpcChannel channel = GrpcChannel.ForAddress("https://localhost:5001");
            DraughtsService.DraughtsServiceClient client = new DraughtsService.DraughtsServiceClient(channel);
            Random rd = new Random();
            int port = 5055;

            ViewM1.ShowMess += MessageB;
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            bool isFree = false;
            while (!isFree)
            {
                port = 5050 + (rd.Next() % 10);
                var res = client.IsPortFree(new IsFree
                {
                    Port = (uint)port
                });
                isFree = res.Success;
            }

            var host = CreateHostBuilder(e.Args, port)
                .Build();

            var callbackService = host.Services.GetService(typeof(DraughtsCallbackServiceImpl))
                as DraughtsCallbackServiceImpl;

            host.RunAsync();//.GetAwaiter().GetResult();

            (Current.MainWindow = new MainWindow
            {
                DataContext = new ViewM1(callbackService, port)
            }).Show();
        }

        private bool MessageB(string mess)
        {
            if(((int)MessageBox.Show(mess, "Вопрос", MessageBoxButton.YesNo)) == 6)
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private IHostBuilder CreateHostBuilder(string[] args, int port)
        {
/*            string[] urls = new string[50];
            for (int i = 5050; i < 5050 + 50; i++)
                urls[i-5050] = $"http://127.0.0.1:{i}";*/
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {

                    webBuilder.UseUrls($"http://127.0.0.1:{port}");
                    webBuilder.UseStartup<Startup>();
                });
        }

    }
}
