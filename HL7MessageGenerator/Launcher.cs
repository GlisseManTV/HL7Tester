﻿using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static HL7Tester.Main;

namespace HL7Tester
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
                logger.Info($"\nComputer: {Environment.MachineName}\nUser: {Environment.UserName}\nApp Version: {Assembly.GetExecutingAssembly().GetName().Version}\nApplication started successfully.\n");
                mainForm.FormClosed += (s, e) =>
                {
                    logger.Info("\nApplication exited successfully.\n");
                    this.Close();
                };

            }
        }
}
