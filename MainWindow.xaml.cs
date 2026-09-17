using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Win32;

namespace uniqueCharSetCombosWpf;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private long _comboCount;
    private readonly Stopwatch _elapsedStopwatch = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private async void GetCombinationsCount(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*",
            Title = "Select a character set"
        };

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        string target = dialog.FileName;
        SelectedFileTextBlock.Text = target;
        ComboCountTextBlock.Text = "0";
        ProcessedCountTextBlock.Text = "0 items";
        ElapsedTimeTextBlock.Text = "00:00.000";
        ProgressBar.Value = 0;
        ProgressBar.IsIndeterminate = false;
        ProgressPercentTextBlock.Text = "0%";
        ProcessingStatusTextBlock.Text = "Generating combinations";
        WorkingIndicatorEllipse.Fill = new SolidColorBrush(Color.FromRgb(227, 160, 8));
        WorkingStatusTextBlock.Text = "WORKING";
        WorkingStatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(164, 109, 0));
        CombinationsPanel.Children.Clear();

        IsEnabled = false;
        _elapsedStopwatch.Restart();

        var progress = new Progress<double>(_ =>
        {
            ProgressBar.IsIndeterminate = true;
            ProgressPercentTextBlock.Text = "Searching...";
            ElapsedTimeTextBlock.Text = _elapsedStopwatch.Elapsed.ToString(@"mm\:ss\.fff");
        });

        try
        {
            var result = await Task.Run(() =>
                CharComboService.run_process(target, progress));

            _comboCount = result.Count;
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = 100;
            ProgressPercentTextBlock.Text = "100.0%";
            ComboCountTextBlock.Text = result.Count.ToString();
            ProcessedCountTextBlock.Text = $"{result.Count} items";
            ElapsedTimeTextBlock.Text = result.BuildTime.ToString(@"mm\:ss\.fff");
            ProcessingStatusTextBlock.Text = "Combination generation complete";

            foreach (string combination in result.Combinations)
            {
                CombinationsPanel.Children.Add(new TextBlock
                {
                    Text = $"Combo: {combination}",
                    Foreground = new SolidColorBrush(Color.FromRgb(23, 35, 61)),
                    FontSize = 12,
                    Margin = new Thickness(0, 0, 0, 8),
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }
        catch (Exception exception)
        {
            ProcessingStatusTextBlock.Text = "Generation failed";
            MessageBox.Show(this, exception.Message, "Unable to generate combinations", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _elapsedStopwatch.Stop();
            ProgressBar.IsIndeterminate = false;
            WorkingIndicatorEllipse.Fill = new SolidColorBrush(Color.FromRgb(21, 154, 156));
            WorkingStatusTextBlock.Text = "READY";
            WorkingStatusTextBlock.Foreground = new SolidColorBrush(Color.FromRgb(20, 125, 128));
            IsEnabled = true;
        }
    }
}