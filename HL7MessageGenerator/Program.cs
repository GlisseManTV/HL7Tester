using static HL7MessageGenerator.Main;
using System.Configuration;
using System.IO;

namespace HL7MessageGenerator
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
            if (HL7MessageGenerator.Properties.Settings.Default.LastUpgradeVersion != Application.ProductVersion)
            {
                HL7MessageGenerator.Properties.Settings.Default.Upgrade();
                HL7MessageGenerator.Properties.Settings.Default.LastUpgradeVersion = Application.ProductVersion;
                HL7MessageGenerator.Properties.Settings.Default.Save();
            }
        }
    }

}