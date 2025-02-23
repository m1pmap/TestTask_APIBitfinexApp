using System.Configuration;
using System.Data;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Effects;
using Microsoft.Extensions.DependencyInjection;
using TestTask.BLL.Interfaces;
using TestTask.BLL.Services;


namespace TestTask;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public IServiceProvider ServiceProvider { get; private set; }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        ServiceProvider = ConfigureServices();

        var mainWindow = ServiceProvider.GetRequiredService<MainWindow>();
        mainWindow.Show();
    }

    private IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        //Регистрация BLL сервисов
        services.AddScoped<ITestConnector, TaskConnector_Service>();

        //Регистрация окон
        services.AddTransient<MainWindow>();


        return services.BuildServiceProvider();
    }
}

