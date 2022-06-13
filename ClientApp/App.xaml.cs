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
            ViewM1.ShowMess += MessageB;
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

        private IHostBuilder CreateHostBuilder(string[] args)
        {
            string[] urls = new string[50];
            for (int i = 5050; i < 5050 + 50; i++)
                urls[i-5050] = $"http://127.0.0.1:{i}";
            return Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {

                    webBuilder.UseUrls(urls);
                    webBuilder.UseStartup<Startup>();
                });
        }

    }
}
