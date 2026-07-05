using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GoogleSatelliteCAD.Licensing;
using Exception = System.Exception;

namespace GoogleSatelliteCAD.UI
{
    /// <summary>
    /// Faollashtirish oynasi (WPF): "Product key" (Machine ID) ko'rsatiladi,
    /// foydalanuvchi "Activation key" (litsenziya matni) ni joylab "Activate" bosadi.
    /// </summary>
    public sealed class ActivationWindow : Window
    {
        private static readonly Brush Accent = new SolidColorBrush(Color.FromRgb(0x1F, 0x6F, 0x43));

        private readonly TextBox _productKey;
        private readonly TextBox _activationKey;
        private readonly TextBlock _message;

        public ActivationWindow()
        {
            Title = "Google Satellite — Faollashtirish";
            Width = 640;
            Height = 320;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = false;
            Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xF7, 0xF7));

            var grid = new Grid { Margin = new Thickness(24) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            for (int i = 0; i < 5; i++)
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            _message = new TextBlock
            {
                Text = "Faollashtirish zarur. Bu plagindan foydalanishda davom etish uchun " +
                       "faollashtirish kalitini quyida joylang.",
                FontWeight = FontWeights.Bold,
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                Margin = new Thickness(0, 6, 0, 26)
            };
            Grid.SetRow(_message, 0);
            Grid.SetColumnSpan(_message, 3);
            grid.Children.Add(_message);

            // Product key
            AddLabel(grid, "Product key", 1);
            _productKey = new TextBox { IsReadOnly = true, Height = 26, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 4, 8, 4) };
            try { _productKey.Text = MachineIdProvider.Get(); } catch (Exception ex) { _productKey.Text = "(xato: " + ex.Message + ")"; }
            Grid.SetRow(_productKey, 1); Grid.SetColumn(_productKey, 1);
            grid.Children.Add(_productKey);
            grid.Children.Add(MakeButton("Copy", 1, () => { try { if (!string.IsNullOrEmpty(_productKey.Text)) Clipboard.SetText(_productKey.Text); } catch { } }));

            // Activation key
            AddLabel(grid, "Activation key", 2);
            _activationKey = new TextBox { Height = 26, VerticalContentAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 4, 8, 4) };
            Grid.SetRow(_activationKey, 2); Grid.SetColumn(_activationKey, 1);
            grid.Children.Add(_activationKey);
            grid.Children.Add(MakeButton("Paste", 2, () => { try { if (Clipboard.ContainsText()) _activationKey.Text = Clipboard.GetText().Trim(); } catch { } }));

            // Activate button (bottom-right)
            var activate = new Button
            {
                Content = "Activate",
                Width = 100,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                IsDefault = true
            };
            activate.Click += (s, e) => OnActivate();
            Grid.SetRow(activate, 5);
            Grid.SetColumn(activate, 1);
            Grid.SetColumnSpan(activate, 2);
            grid.Children.Add(activate);

            Content = grid;

            LicenseResult cur = LicenseManager.CheckInstalled();
            if (cur.IsValid)
            {
                _message.Text = "Plagin faollashtirilgan. " + cur.Message;
                _message.Foreground = Accent;
            }
        }

        private static void AddLabel(Grid grid, string text, int row)
        {
            var lbl = new TextBlock { Text = text, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetRow(lbl, row);
            Grid.SetColumn(lbl, 0);
            grid.Children.Add(lbl);
        }

        private static Button MakeButton(string text, int row, System.Action onClick)
        {
            var b = new Button { Content = text, Width = 78, Height = 26, Margin = new Thickness(0, 4, 0, 4) };
            b.Click += (s, e) => onClick();
            Grid.SetRow(b, row);
            Grid.SetColumn(b, 2);
            return b;
        }

        private void OnActivate()
        {
            LicenseResult result = LicenseManager.Install(_activationKey.Text);
            MessageBox.Show(result.Message,
                result.IsValid ? "Faollashtirildi" : "Faollashtirish xatosi",
                MessageBoxButton.OK,
                result.IsValid ? MessageBoxImage.Information : MessageBoxImage.Warning);
            if (result.IsValid)
            {
                DialogResult = true;
                Close();
            }
        }
    }
}
