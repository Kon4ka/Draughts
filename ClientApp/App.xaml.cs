using System.Windows;
using ClientApp.Grpc;
using ClientApp.VM;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace ClientApp
{

    public partial class App : Application
    {

        protected override void OnStartup(StartupEventArgs e)
        {
            Current.ShutdownMode = ShutdownMode.OnMainWindowClose;

            var host = CreateHostBuilder(e.Args)
                .Build();

            var callbackService = host.Services.GetService(typeof(DraughtsCallbackServiceImpl))
                as DraughtsCallbackServiceImpl;

            host.RunAsync();//.GetAwaiter().GetResult();

            (Current.MainWindow = new MainWindow
            {
                DataContext = new ViewM1(callbackService)
            }).Show();
        }

        private IHostBuilder CreateHostBuilder(string[] args)
        {
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {

                    webBuilder.UseUrls("http://127.0.0.1:5055");
                    webBuilder.UseStartup<Startup>();
                });
        }

    }
}
