using System;
using System.Windows.Input;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.Windows;
using GoogleSatelliteCAD.Core;

namespace GoogleSatelliteCAD.UI
{
    /// <summary>
    /// "Google Maps" Ribbon yorlig'ini (tab) va uning tugmalarini quradi.
    /// Tugmalar: ON, OFF, Clear Cache, Coordinate System, About.
    ///
    /// Ribbon AutoCAD ishga tushgach kechroq tayyor bo'lishi mumkin — shuning
    /// uchun <see cref="TryBuild"/> idempotent (qayta chaqirsa zarar qilmaydi).
    /// </summary>
    public sealed class RibbonUI
    {
        private const string TabId = "GSAT_RIBBON_TAB";
        private const string TabTitle = "Google Maps";

        private static readonly Lazy<RibbonUI> _instance = new Lazy<RibbonUI>(() => new RibbonUI());
        public static RibbonUI Instance => _instance.Value;

        private RibbonTab _tab;

        private RibbonUI() { }

        /// <summary>
        /// Ribbon mavjud bo'lsa, "Google Maps" yorlig'ini quradi.
        /// Yorliq allaqachon mavjud bo'lsa, hech narsa qilmaydi.
        /// </summary>
        public void TryBuild()
        {
            try
            {
                RibbonControl ribbon = ComponentManager.Ribbon;
                if (ribbon == null) return; // Ribbon hali tayyor emas — keyinroq qayta urinamiz.

                // Allaqachon qo'shilgan bo'lsa — chiqamiz.
                foreach (RibbonTab t in ribbon.Tabs)
                {
                    if (t.Id == TabId) { _tab = t; return; }
                }

                _tab = new RibbonTab { Title = TabTitle, Id = TabId };
                ribbon.Tabs.Add(_tab);

                var source = new RibbonPanelSource { Title = "Google Satellite" };
                var panel = new RibbonPanel { Source = source };
                _tab.Panels.Add(panel);

                source.Items.Add(CreateButton("Satellite ON\n(REFRESH)",
                    "Google Satellite fon xaritasini yoqish. Allaqachon yoqilgan bo'lsa, " +
                    "ko'rinib turgan hududni qayta yuklaydi/yangilaydi (GSATON)",
                    new RelayCommand(() => SendCommand("GSATON "))));
                source.Items.Add(CreateButton("Go to\nKosonsoy", "Ko'rinishni Namangan viloyati, Kosonsoy tumaniga olib borish (GSATHOME)",
                    new RelayCommand(() => SendCommand("GSATHOME "))));
                source.Items.Add(CreateButton("Satellite\nOFF", "Fon xaritasini o'chirish (GSATOFF)",
                    new RelayCommand(() => SendCommand("GSATOFF "))));
                source.Items.Add(new RibbonSeparator());
                source.Items.Add(CreateButton("Clear\nCache", "Tile keshini tozalash (GSATCLEAR)",
                    new RelayCommand(() => SendCommand("GSATCLEAR "))));
                source.Items.Add(CreateButton("Coordinate\nSystem", "Koordinata tizimi va sozlamalar",
                    new RelayCommand(ShowSettings)));
                source.Items.Add(CreateButton("About", "Dastur haqida",
                    new RelayCommand(ShowAbout)));

                Logger.Info("\"Google Maps\" Ribbon yorlig'i qo'shildi.");
            }
            catch (Exception ex)
            {
                Logger.Error("Ribbon qurishda xatolik.", ex);
            }
        }

        /// <summary>Ribbon yorlig'ini olib tashlaydi (plagin to'xtaganda).</summary>
        public void Remove()
        {
            try
            {
                RibbonControl ribbon = ComponentManager.Ribbon;
                if (ribbon == null || _tab == null) return;
                ribbon.Tabs.Remove(_tab);
                _tab = null;
            }
            catch (Exception ex)
            {
                Logger.Warn("Ribbon olib tashlashda xatolik: " + ex.Message);
            }
        }

        private RibbonButton CreateButton(string text, string tooltip, ICommand command)
        {
            return new RibbonButton
            {
                Text = text,
                ShowText = true,
                ShowImage = false,
                Size = RibbonItemSize.Large,
                Orientation = System.Windows.Controls.Orientation.Vertical,
                CommandHandler = command,
                ToolTip = tooltip,
                IsToolTipEnabled = true
            };
        }

        private static void SendCommand(string command)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;
            // SendStringToExecute: buyruqni AutoCAD buyruq qatorida xavfsiz ishga tushiradi.
            doc?.SendStringToExecute(command, true, false, true);
        }

        private static void ShowSettings()
        {
            try
            {
                var win = new SettingsWindow();
                Application.ShowModalWindow(win);
            }
            catch (Exception ex)
            {
                Logger.Error("Sozlamalar oynasini ochishda xatolik.", ex);
            }
        }

        private static void ShowAbout()
        {
            try
            {
                var win = new AboutWindow();
                Application.ShowModalWindow(win);
            }
            catch (Exception ex)
            {
                Logger.Error("About oynasini ochishda xatolik.", ex);
            }
        }
    }

    /// <summary>
    /// Oddiy <see cref="ICommand"/> amalga oshirilishi — Ribbon tugmalarini
    /// delegatlar (Action) bilan bog'lash uchun.
    /// </summary>
    internal sealed class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;

        public void Execute(object parameter) => _execute();

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
