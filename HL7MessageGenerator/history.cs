using MaterialSkin.Controls;
using MaterialSkin;
using HL7Tester;
using System;
using System.Text;



namespace HL7Tester
{
    public partial class history : MaterialForm
    {
        public history()
        {
            InitializeComponent();
            lastUsedIpsMultiBox.Text = string.Join("\n", GetLastUsedConnections());
            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            if (IsDarkThemeEnabled())
            {
                materialSkinManager.Theme = MaterialSkinManager.Themes.DARK;
            }
            else
            {
                materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            }
        }
        private string FormatConnection(string ip, string port)
        {
            if (ip == "" && port == "")
            {
                return "";
            }
            else
            {
                return $"{ip}:{port}"; // Affiche "IP:Port" si au moins une valeur est valide
            }
        }
        private string[] GetLastUsedConnections()
        {
            return new[]
            {
                FormatConnection(Properties.Settings.Default.LastIpAddress ?? "N/A", Properties.Settings.Default.LastPort ?? "N/A"),
                FormatConnection(Properties.Settings.Default.LastIpAddress2 ?? "N/A", Properties.Settings.Default.LastPort2 ?? "N/A"),
                FormatConnection(Properties.Settings.Default.LastIpAddress3 ?? "N/A", Properties.Settings.Default.LastPort3 ?? "N/A"),
                FormatConnection(Properties.Settings.Default.LastIpAddress4 ?? "N/A", Properties.Settings.Default.LastPort4 ?? "N/A"),
                FormatConnection(Properties.Settings.Default.LastIpAddress5 ?? "N/A", Properties.Settings.Default.LastPort5 ?? "N/A")
            };

            //(string.IsNullOrEmpty(Properties.Settings.Default.LastIpAddress) ? "N/A" : Properties.Settings.Default.LastIpAddress);
        }
        private bool IsDarkThemeEnabled()
        {
            // W10 before 2003
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes"))
            {
                if (key != null)
                {
                    var theme = key.GetValue("AppsUseLightTheme");
                    if (theme != null)
                        return (int)theme == 0;
                }
            }

            // W11
            using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
            {
                if (key != null)
                {
                    var theme = key.GetValue("AppsUseLightTheme");
                    if (theme != null)
                        return (int)theme == 0;
                }
            }

            return false;
        }
    }
}