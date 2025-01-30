using Microsoft.Extensions.DependencyInjection;
using DriveMasterApp.Interfaces;
using DriveMasterApp.Services;

namespace DriveMasterApp
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);

            using var serviceProvider = services.BuildServiceProvider();

            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            var mainForm = serviceProvider.GetRequiredService<Form1>();
            Application.Run(mainForm);
        }
        /// <summary>
        /// Конфигурация сервисов для DI
        /// </summary>
        /// <param name="services"></param>
        private static void ConfigureServices(ServiceCollection services)
        {
            services.AddSingleton<IComPortConnection, ComPortConnectionService>();
            services.AddSingleton<IComPortSend, ComPortSendService>();
            services.AddSingleton<Form1>();
            services.AddTransient<PlotForm>();
            services.AddSingleton<IServiceProvider>(sp => sp);
        }
    }
}
