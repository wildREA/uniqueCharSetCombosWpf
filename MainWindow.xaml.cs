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

namespace uniqueCharSetCombosWpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private long _comboCount;

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void GetCombinationsCount(object sender, RoutedEventArgs e)
    {
        string dir = Directory.GetCurrentDirectory();
        string target = Path.Combine(
            AppContext.BaseDirectory,
            "assets",
            "alpha.txt");

        var result = await Task.Run(() =>
            CharComboService.run_process(target));

        _comboCount = result.Count;
        ComboCountTextBlock.Text = result.Count.ToString();
        ElapsedTimeTextBlock.Text = result.BuildTime.ToString(
            @"mm\:ss\.fff"
        );
    }
}