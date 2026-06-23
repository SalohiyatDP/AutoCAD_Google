using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GoogleSatelliteCAD.Core;
using GoogleSatelliteCAD.Projection;

namespace GoogleSatelliteCAD.UI
{
    /// <summary>
    /// Sozlamalar oynasi — to'liq kod ichida quriladi (XAML markup
    /// kompilyatori talab qilinmaydi, shu sababli AutoCAD plagin loyihasida
    /// ishonchli yig'iladi).
    ///
    /// Parametrlar: Tile server URL, Koordinata tizimi, Kesh hajmi,
    /// Maksimal zoom, Parallel yuklab olishlar, Avtomatik yangilash.
    /// </summary>
    public sealed class SettingsWindow : Window
    {
        private readonly TextBox _urlBox;
        private readonly ComboBox _crsCombo;
        private readonly TextBox _cacheBox;
        private readonly TextBox _zoomBox;
        private readonly TextBox _parallelBox;
        private readonly CheckBox _autoRefreshCheck;

        public SettingsWindow()
        {
            PluginSettings s = ConfigManager.Instance.Settings;

            Title = "Google Satellite — Sozlamalar";
            Width = 540;
            Height = 430;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ShowInTaskbar = false;
            Background = new SolidColorBrush(Color.FromRgb(0xF4, 0xF4, 0xF4));

            var root = new Grid { Margin = new Thickness(16) };
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new TextBlock
            {
                Text = "Google Satellite plagin sozlamalari",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 14)
            };
            Grid.SetRow(header, 0);
            root.Children.Add(header);

            var form = new Grid();
            form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(190) });
            form.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            for (int i = 0; i < 6; i++)
                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddLabel(form, 0, "Tile server URL:");
            _urlBox = AddTextBox(form, 0, s.TileServerUrl);

            AddLabel(form, 1, "Koordinata tizimi:");
            _crsCombo = new ComboBox { Margin = new Thickness(0, 6, 0, 6) };
            // Foydalanuvchi ikki tizimda ishlaydi:
            //  - Web Mercator (EPSG:3857): umumiy/keng hudud uchun.
            //  - Pulkovo 1942 GK Zona 12N: aniq (haqiqiy yer metrlari) lokal ish uchun.
            _crsCombo.Items.Add(CoordinateSystemType.WebMercator_3857);
            _crsCombo.Items.Add(CoordinateSystemType.Pulkovo1942_GK_Zone12N);
            _crsCombo.SelectedItem =
                s.CoordinateSystem == CoordinateSystemType.Pulkovo1942_GK_Zone12N
                    ? CoordinateSystemType.Pulkovo1942_GK_Zone12N
                    : CoordinateSystemType.WebMercator_3857;
            PlaceInForm(form, _crsCombo, 1);

            AddLabel(form, 2, "Kesh hajmi (MB, 0=cheksiz):");
            _cacheBox = AddTextBox(form, 2, s.MaxCacheSizeMb.ToString());

            AddLabel(form, 3, "Maksimal zoom (0-22):");
            _zoomBox = AddTextBox(form, 3, s.MaxZoom.ToString());

            AddLabel(form, 4, "Parallel yuklab olishlar:");
            _parallelBox = AddTextBox(form, 4, s.MaxParallelDownloads.ToString());

            AddLabel(form, 5, "Avtomatik yangilash:");
            _autoRefreshCheck = new CheckBox
            {
                Content = "Pan/Zoom paytida xaritani avtomatik yangilash",
                IsChecked = s.AutoRefresh,
                Margin = new Thickness(0, 10, 0, 6),
                VerticalAlignment = VerticalAlignment.Center
            };
            PlaceInForm(form, _autoRefreshCheck, 5);

            Grid.SetRow(form, 1);
            root.Children.Add(form);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 14, 0, 0)
            };
            var ok = new Button { Content = "OK", Width = 90, Height = 28, Margin = new Thickness(0, 0, 10, 0), IsDefault = true };
            ok.Click += OnOkClick;
            var cancel = new Button { Content = "Bekor qilish", Width = 110, Height = 28, IsCancel = true };
            cancel.Click += (sender, e) => { DialogResult = false; Close(); };
            buttons.Children.Add(ok);
            buttons.Children.Add(cancel);

            Grid.SetRow(buttons, 2);
            root.Children.Add(buttons);

            Content = root;
        }

        private static void AddLabel(Grid form, int row, string text)
        {
            var tb = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 6, 8, 6)
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, 0);
            form.Children.Add(tb);
        }

        private static TextBox AddTextBox(Grid form, int row, string value)
        {
            var box = new TextBox { Text = value, Margin = new Thickness(0, 6, 0, 6) };
            PlaceInForm(form, box, row);
            return box;
        }

        private static void PlaceInForm(Grid form, UIElement element, int row)
        {
            Grid.SetRow(element, row);
            Grid.SetColumn(element, 1);
            form.Children.Add(element);
        }

        private void OnOkClick(object sender, RoutedEventArgs e)
        {
            try
            {
                var current = ConfigManager.Instance.Settings;
                var crs = _crsCombo.SelectedItem is CoordinateSystemType t ? t : current.CoordinateSystem;
                bool crsChanged = crs != current.CoordinateSystem;

                var newSettings = new PluginSettings
                {
                    TileServerUrl = string.IsNullOrWhiteSpace(_urlBox.Text) ? current.TileServerUrl : _urlBox.Text.Trim(),
                    CoordinateSystem = crs,
                    MaxCacheSizeMb = ParseInt(_cacheBox.Text, current.MaxCacheSizeMb),
                    MaxZoom = Clamp(ParseInt(_zoomBox.Text, current.MaxZoom), TileEngine.TileSystem.MinZoom, TileEngine.TileSystem.MaxZoom),
                    MaxParallelDownloads = Math.Max(1, ParseInt(_parallelBox.Text, current.MaxParallelDownloads)),
                    AutoRefresh = _autoRefreshCheck.IsChecked == true,
                    UserAgent = current.UserAgent
                };

                ConfigManager.Instance.Update(newSettings);

                if (crsChanged)
                {
                    PluginContext.Instance.RebuildTransform();
                }
                if (PluginContext.Instance.IsActive)
                {
                    PluginContext.Instance.RequestRefresh();
                }

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Logger.Error("Sozlamalarni saqlashda xatolik.", ex);
                MessageBox.Show("Sozlamalarni saqlab bo'lmadi: " + ex.Message, "Xatolik",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static int ParseInt(string text, int fallback)
        {
            return int.TryParse((text ?? "").Trim(), out int v) ? v : fallback;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
    }
}
