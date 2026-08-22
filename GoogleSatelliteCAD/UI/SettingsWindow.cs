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
        private readonly TextBox _offsetXBox;
        private readonly TextBox _offsetYBox;

        public SettingsWindow()
        {
            PluginSettings s = ConfigManager.Instance.Settings;

            Title = "Google Satellite — Sozlamalar";
            Width = 540;
            Height = 520;
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
            for (int i = 0; i < 8; i++)
                form.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            AddLabel(form, 0, "Tile server URL:");
            _urlBox = AddTextBox(form, 0, s.TileServerUrl);

            AddLabel(form, 1, "Koordinata tizimi:");
            _crsCombo = new ComboBox { Margin = new Thickness(0, 6, 0, 6) };
            // Qo'llab-quvvatlanadigan tizimlar (EPSG kodlari bilan aniq ko'rsatiladi).
            // Butun O'zbekiston hududi Gauss-Kruger 10N..13N zonalari bilan qoplanadi.

            // Tavsiya etiladigan standart: avto-zona (butun O'zbekiston bo'ylab).
            AddCrsItem(CoordinateSystemType.Pulkovo1942_GK_ZoneAuto,
                "Pulkovo 1942 / GK AVTO-ZONA — butun O'zbekiston (10N..13N, prefiksli easting) [tavsiya]",
                s.CoordinateSystem);

            // Prefiksli (zonalangan) easting, EPSG:284xx — easting ~zona,5xx,xxx.
            AddCrsItem(CoordinateSystemType.Pulkovo1942_GK_Zone10N,
                "Pulkovo 1942 / GK Zona 10N (EPSG:28410, easting ~10,5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.Pulkovo1942_GK_Zone11N,
                "Pulkovo 1942 / GK Zona 11N (EPSG:28411, easting ~11,5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.Pulkovo1942_GK_Zone12N,
                "Pulkovo 1942 / GK Zona 12N (EPSG:28412, easting ~12,5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.Pulkovo1942_GK_Zone13N,
                "Pulkovo 1942 / GK Zona 13N (EPSG:28413, easting ~13,5xx,xxx)", s.CoordinateSystem);

            // Prefikssiz easting (false easting 500000), EPSG:2846x — easting ~5xx,xxx.
            AddCrsItem(CoordinateSystemType.Pulkovo1942_GK_Zone10N_28460,
                "Pulkovo 1942 / GK Zona 10N (EPSG:28460, easting ~5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.Pulkovo1942_GK_Zone11N_28461,
                "Pulkovo 1942 / GK Zona 11N (EPSG:28461, easting ~5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.Pulkovo1942_GK_Zone12N_28462,
                "Pulkovo 1942 / GK Zona 12N (EPSG:28462, easting ~5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.Pulkovo1942_GK_Zone13N_28463,
                "Pulkovo 1942 / GK Zona 13N (EPSG:28463, easting ~5xx,xxx)", s.CoordinateSystem);

            // WGS 84 / UTM zonalari (O'zbekiston 40N..43N) — prefikssiz, easting ~5xx,xxx (toposyomka).
            AddCrsItem(CoordinateSystemType.WGS84_UTM_Zone40N,
                "WGS 84 / UTM Zona 40N (EPSG:32640, MM 57°E, easting ~5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.WGS84_UTM_Zone41N,
                "WGS 84 / UTM Zona 41N (EPSG:32641, MM 63°E, easting ~5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.WGS84_UTM_Zone42N,
                "WGS 84 / UTM Zona 42N (EPSG:32642, MM 69°E, easting ~5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.WGS84_UTM_Zone43N,
                "WGS 84 / UTM Zona 43N (EPSG:32643, MM 75°E, easting ~5xx,xxx)", s.CoordinateSystem);

            // WGS 84 / UTM PREFIKSLI easting (false easting = zona·1e6 + 500000) — easting ~zona,5xx,xxx.
            AddCrsItem(CoordinateSystemType.WGS84_UTM_ZoneAuto,
                "WGS 84 / UTM AVTO-ZONA — butun O'zbekiston (40N..43N, prefiksli easting)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.WGS84_UTM_Zone40N_Zoned,
                "WGS 84 / UTM Zona 40N — prefiksli (easting ~40,5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.WGS84_UTM_Zone41N_Zoned,
                "WGS 84 / UTM Zona 41N — prefiksli (easting ~41,5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.WGS84_UTM_Zone42N_Zoned,
                "WGS 84 / UTM Zona 42N — prefiksli (easting ~42,5xx,xxx)", s.CoordinateSystem);
            AddCrsItem(CoordinateSystemType.WGS84_UTM_Zone43N_Zoned,
                "WGS 84 / UTM Zona 43N — prefiksli (easting ~43,5xx,xxx)", s.CoordinateSystem);

            // Umumiy/keng hudud uchun.
            AddCrsItem(CoordinateSystemType.WebMercator_3857,
                "WGS 1984 Web Mercator (EPSG:3857)", s.CoordinateSystem);

            if (_crsCombo.SelectedIndex < 0) _crsCombo.SelectedIndex = 0;
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

            AddLabel(form, 6, "Qatlam surilishi X (metr):");
            _offsetXBox = AddTextBox(form, 6, s.OffsetX.ToString("G"));

            AddLabel(form, 7, "Qatlam surilishi Y (metr):");
            _offsetYBox = AddTextBox(form, 7, s.OffsetY.ToString("G"));

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

        /// <summary>Koordinata tizimi elementini chiroyli nom (EPSG kodi bilan) qo'shadi.</summary>
        private void AddCrsItem(CoordinateSystemType crs, string label, CoordinateSystemType current)
        {
            var item = new ComboBoxItem { Content = label, Tag = crs };
            _crsCombo.Items.Add(item);
            if (crs == current) _crsCombo.SelectedItem = item;
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
                var crs = current.CoordinateSystem;
                if (_crsCombo.SelectedItem is ComboBoxItem ci && ci.Tag is CoordinateSystemType sel)
                    crs = sel;
                bool crsChanged = crs != current.CoordinateSystem;

                var newSettings = new PluginSettings
                {
                    TileServerUrl = string.IsNullOrWhiteSpace(_urlBox.Text) ? current.TileServerUrl : _urlBox.Text.Trim(),
                    CoordinateSystem = crs,
                    MaxCacheSizeMb = ParseInt(_cacheBox.Text, current.MaxCacheSizeMb),
                    MaxZoom = Clamp(ParseInt(_zoomBox.Text, current.MaxZoom), TileEngine.TileSystem.MinZoom, TileEngine.TileSystem.MaxZoom),
                    MaxParallelDownloads = Math.Max(1, ParseInt(_parallelBox.Text, current.MaxParallelDownloads)),
                    AutoRefresh = _autoRefreshCheck.IsChecked == true,
                    OffsetX = ParseDouble(_offsetXBox.Text, current.OffsetX),
                    OffsetY = ParseDouble(_offsetYBox.Text, current.OffsetY),
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

        private static double ParseDouble(string text, double fallback)
        {
            return double.TryParse((text ?? "").Trim(),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v) ? v : fallback;
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);
    }
}
