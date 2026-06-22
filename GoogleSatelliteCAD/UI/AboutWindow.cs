using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleSatelliteCAD.UI
{
    /// <summary>
    /// "About" (Dastur haqida) oynasi. WPF orqali kod ichida quriladi
    /// (alohida XAML talab qilinmaydi).
    /// </summary>
    public sealed class AboutWindow : Window
    {
        public AboutWindow()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

            Title = "Google Satellite — Dastur haqida";
            Width = 440;
            Height = 280;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = false;
            Background = new SolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF4));

            var panel = new StackPanel { Margin = new Thickness(20) };

            panel.Children.Add(new TextBlock
            {
                Text = "Google Satellite Map Plugin",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 8)
            });

            panel.Children.Add(new TextBlock
            {
                Text = "AutoCAD Mechanical 2021 uchun Google Satellite\n"
                     + "sun'iy yo'ldosh fon xaritasi plagini.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 12)
            });

            panel.Children.Add(new TextBlock { Text = "Versiya: " + version, Margin = new Thickness(0, 2, 0, 2) });
            panel.Children.Add(new TextBlock { Text = "Buyruqlar: GSATON, GSATOFF, GSATCLEAR", Margin = new Thickness(0, 2, 0, 2) });
            panel.Children.Add(new TextBlock { Text = "Platforma: .NET Framework 4.8 / AutoCAD .NET API", Margin = new Thickness(0, 2, 0, 2) });
            panel.Children.Add(new TextBlock
            {
                Text = "Texnologiya ArcGIS google.lyr fon xaritasiga o'xshash ishlaydi.",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 10, 0, 12)
            });

            var okButton = new Button
            {
                Content = "Yopish",
                Width = 90,
                Height = 28,
                HorizontalAlignment = HorizontalAlignment.Right,
                IsCancel = true
            };
            okButton.Click += (s, e) => Close();
            panel.Children.Add(okButton);

            Content = panel;
        }
    }
}
