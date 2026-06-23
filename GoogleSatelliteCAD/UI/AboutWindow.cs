using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GoogleSatelliteCAD.UI
{
    /// <summary>
    /// "About" (Dastur haqida) oynasi. WPF orqali kod ichida quriladi.
    /// Dastur, muallif va tashkilot ma'lumotlarini ko'rsatadi.
    /// </summary>
    public sealed class AboutWindow : Window
    {
        private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0x43));
        private static readonly Brush Muted = new SolidColorBrush(Color.FromRgb(0x55, 0x55, 0x55));

        public AboutWindow()
        {
            string version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0.0";

            Title = "Google Satellite — Dastur haqida";
            Width = 480;
            Height = 470;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = false;
            Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xF7));

            var root = new StackPanel { Margin = new Thickness(24, 20, 24, 18) };

            root.Children.Add(new TextBlock
            {
                Text = "Google Satellite Map Plugin",
                FontSize = 20,
                FontWeight = FontWeights.Bold,
                Foreground = Accent
            });
            root.Children.Add(new TextBlock
            {
                Text = "AutoCAD Mechanical 2021 uchun Google sun'iy yo'ldosh fon xaritasi",
                TextWrapping = TextWrapping.Wrap,
                Foreground = Muted,
                Margin = new Thickness(0, 2, 0, 14)
            });

            Content = new ScrollViewer { Content = root, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
            BuildBody(root, version);
        }

        private void BuildBody(StackPanel root, string version)
        {
            // Texnik ma'lumotlar
            root.Children.Add(Row("Versiya:", version));
            root.Children.Add(Row("Buyruqlar:", "GSATON, GSATOFF, GSATCLEAR"));
            root.Children.Add(Row("Platforma:", ".NET Framework 4.8 / AutoCAD .NET API"));
            root.Children.Add(Row("Hudud:", "O'zbekiston Respublikasi"));

            root.Children.Add(Divider());

            // Mualliflar va tashkilot
            root.Children.Add(InfoBlock("Dasturchi", "Abdujabborov Sherzod Jahongir o'g'li"));
            root.Children.Add(InfoBlock("G'oya muallifi", "Karimbekov Asadbek Nasibbek o'g'li"));
            root.Children.Add(InfoBlock("Tashkilot",
                "Davlat Kadastrlari Palatasi\nKosonsoy tuman filiali"));

            root.Children.Add(Divider());

            var okButton = new Button
            {
                Content = "Yopish",
                Width = 100,
                Height = 30,
                HorizontalAlignment = HorizontalAlignment.Right,
                IsCancel = true,
                IsDefault = true
            };
            okButton.Click += (s, e) => Close();
            root.Children.Add(okButton);
        }

        /// <summary>Bitta qatorli "kalit: qiymat" ko'rinishi (texnik ma'lumotlar uchun).</summary>
        private static UIElement Row(string key, string value)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 3, 0, 3) };
            panel.Children.Add(new TextBlock { Text = key, FontWeight = FontWeights.SemiBold, Width = 90 });
            panel.Children.Add(new TextBlock { Text = value, TextWrapping = TextWrapping.Wrap });
            return panel;
        }

        /// <summary>Sarlavhali ma'lumot bloki (muallif/tashkilot uchun, ko'zga ko'rinarli).</summary>
        private static UIElement InfoBlock(string caption, string value)
        {
            var panel = new StackPanel { Margin = new Thickness(0, 6, 0, 6) };
            panel.Children.Add(new TextBlock
            {
                Text = caption,
                FontSize = 11,
                Foreground = Muted,
                FontWeight = FontWeights.SemiBold
            });
            panel.Children.Add(new TextBlock
            {
                Text = value,
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = Accent,
                TextWrapping = TextWrapping.Wrap
            });
            return panel;
        }

        /// <summary>Bo'limlar orasidagi nozik ajratuvchi chiziq.</summary>
        private static UIElement Divider()
        {
            return new Border
            {
                Height = 1,
                Background = new SolidColorBrush(Color.FromRgb(0xDD, 0xDD, 0xDD)),
                Margin = new Thickness(0, 12, 0, 12)
            };
        }
    }
}

