using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static HL7MessageGenerator.Main;
using NLog;

namespace HL7MessageGenerator
{
        public partial class Launcher : Form
        {
            public Launcher()
            {
                this.Shown += StartupForm_Shown;
            }
            private static readonly Logger logger = LogManager.GetCurrentClassLogger();

            private async void StartupForm_Shown(object sender, EventArgs e)
            {
                this.Visible = false;

                Main.CheckAndCreateLogDirectory();
                await UpdateChecker.CheckForUpdateAsync();



            var mainForm = new Main();
                mainForm.Show();
                this.Hide();
                logger.Info($"\nComputer: {Environment.MachineName}\nUser: {Environment.UserName}\nApplication started successfully.\n");
                mainForm.FormClosed += (s, e) =>
                {
                    logger.Info("\nApplication exited successfully.\n");
                    this.Close();
                };

            }
        }
}
