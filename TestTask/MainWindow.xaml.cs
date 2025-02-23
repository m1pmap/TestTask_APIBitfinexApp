using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using TestTask.BLL.Interfaces;
using TestTask.BLL.Models;
using TestTask.BLL.Services;

namespace TestTask;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow() { InitializeComponent(); }
    private readonly ITestConnector _testConnectorService;
    public MainWindow(ITestConnector testConnectorService)
    {
        InitializeComponent();

        _testConnectorService = testConnectorService;

        _testConnectorService.GetNewTradesAsync("BTCUSD", 12);
    }
}