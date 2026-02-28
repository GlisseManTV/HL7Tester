using static HL7Tester.Main;
using System.Configuration;
using System.IO;

namespace HL7Tester
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            MigrateUserSettingsIfNeeded();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Launcher());
        }
        static void MigrateUserSettingsIfNeeded()
        {
            if (HL7Tester.Properties.Settings.Default.LastUpgradeVersion != Application.ProductVersion)
            {
                HL7Tester.Properties.Settings.Default.Upgrade();
                HL7Tester.Properties.Settings.Default.LastUpgradeVersion = Application.ProductVersion;
                HL7Tester.Properties.Settings.Default.Save();
            }
        }
    }

}