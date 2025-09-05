using System;
using System.IO;
using System.Windows;
using LoloRecorder.Services;

namespace LoloRecorder.Views
{
    public partial class MainWindow : Window
    {
        private readonly ScreenRecorderService _recorderService;

        public MainWindow()
        {
            InitializeComponent();
            var outputPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gravacao.mp4");
            _recorderService = new ScreenRecorderService(outputPath);
        }

        private async void RecordToggle_Checked(object sender, RoutedEventArgs e)
        {
            try
            {
                var (success, error) = await _recorderService.StartAsync();
                if (!success)
                {
                    StatusLabel.Content = "Erro ao iniciar";
                    RecordToggle.IsChecked = false;
                    MessageBox.Show(error ?? "Falha desconhecida ao iniciar gravação.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                StatusLabel.Content = "Gravando...";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = "Erro ao iniciar";
                RecordToggle.IsChecked = false;
                MessageBox.Show($"Falha inesperada ao iniciar a gravação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RecordToggle_Unchecked(object sender, RoutedEventArgs e)
        {
            try
            {
                await _recorderService.StopAsync();
                StatusLabel.Content = "Parado";
            }
            catch (Exception ex)
            {
                StatusLabel.Content = "Erro ao parar";
                MessageBox.Show($"Falha inesperada ao encerrar a gravação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        protected override async void OnClosed(EventArgs e)
        {
            await _recorderService.DisposeAsync();
            base.OnClosed(e);
        }
    }
}
