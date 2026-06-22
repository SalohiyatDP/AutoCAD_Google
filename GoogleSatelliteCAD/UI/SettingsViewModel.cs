using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GoogleSatelliteCAD.Core;
using GoogleSatelliteCAD.Projection;

namespace GoogleSatelliteCAD.UI
{
    /// <summary>
    /// Sozlamalar oynasi uchun ViewModel (MVVM).
    /// <see cref="PluginSettings"/> qiymatlarini WPF bog'lanishlari (binding)
    /// uchun tahrirlanadigan xususiyatlarga o'raydi.
    /// </summary>
    public sealed class SettingsViewModel : INotifyPropertyChanged
    {
        private string _tileServerUrl;
        private int _maxCacheSizeMb;
        private int _maxZoom;
        private bool _autoRefresh;
        private int _maxParallelDownloads;
        private CoordinateSystemType _coordinateSystem;

        public SettingsViewModel(PluginSettings settings)
        {
            LoadFrom(settings);
        }

        /// <summary>Tanlash mumkin bo'lgan barcha koordinata tizimlari.</summary>
        public IReadOnlyList<CoordinateSystemType> AvailableCoordinateSystems { get; } =
            (CoordinateSystemType[])Enum.GetValues(typeof(CoordinateSystemType));

        public string TileServerUrl
        {
            get => _tileServerUrl;
            set { _tileServerUrl = value; OnPropertyChanged(); }
        }

        public int MaxCacheSizeMb
        {
            get => _maxCacheSizeMb;
            set { _maxCacheSizeMb = value; OnPropertyChanged(); }
        }

        public int MaxZoom
        {
            get => _maxZoom;
            set { _maxZoom = Clamp(value, TileEngine.TileSystem.MinZoom, TileEngine.TileSystem.MaxZoom); OnPropertyChanged(); }
        }

        public bool AutoRefresh
        {
            get => _autoRefresh;
            set { _autoRefresh = value; OnPropertyChanged(); }
        }

        public int MaxParallelDownloads
        {
            get => _maxParallelDownloads;
            set { _maxParallelDownloads = Math.Max(1, value); OnPropertyChanged(); }
        }

        public CoordinateSystemType CoordinateSystem
        {
            get => _coordinateSystem;
            set { _coordinateSystem = value; OnPropertyChanged(); }
        }

        /// <summary>Modeldagi qiymatlarni ViewModel ga yuklaydi.</summary>
        public void LoadFrom(PluginSettings s)
        {
            _tileServerUrl = s.TileServerUrl;
            _maxCacheSizeMb = s.MaxCacheSizeMb;
            _maxZoom = s.MaxZoom;
            _autoRefresh = s.AutoRefresh;
            _maxParallelDownloads = s.MaxParallelDownloads;
            _coordinateSystem = s.CoordinateSystem;
        }

        /// <summary>ViewModel qiymatlarini yangi <see cref="PluginSettings"/> ga ko'chiradi.</summary>
        public PluginSettings ToSettings()
        {
            return new PluginSettings
            {
                TileServerUrl = TileServerUrl,
                MaxCacheSizeMb = MaxCacheSizeMb,
                MaxZoom = MaxZoom,
                AutoRefresh = AutoRefresh,
                MaxParallelDownloads = MaxParallelDownloads,
                CoordinateSystem = CoordinateSystem,
                // UserAgent foydalanuvchi tomonidan tahrirlanmaydi — saqlab qolamiz.
                UserAgent = ConfigManager.Instance.Settings.UserAgent
            };
        }

        private static int Clamp(int v, int min, int max) => v < min ? min : (v > max ? max : v);

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
