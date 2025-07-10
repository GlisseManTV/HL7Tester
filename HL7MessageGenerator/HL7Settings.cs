using System;
using System.Windows.Forms;
using NHapi.Base.Model;
using HL7Tester;
using NHapi.Model.V23.Message;
using NHapi.Model.V23.Segment;
using MaterialSkin.Controls;
using Microsoft.Win32;
using MaterialSkin;
using NLog;
using System.Text;



public partial class HL7Settings : MaterialForm
{
    private static readonly Logger logger = LogManager.GetCurrentClassLogger();
    private bool isInitializing = true;
    public HL7Settings()
    {

        InitializeComponent();
        isInitializing = true;
        if (HL7Tester.Properties.Settings.Default.Properties["AutoUpdateCheck"] != null &&
            HL7Tester.Properties.Settings.Default["AutoUpdateCheck"] == null)
        {
            HL7Tester.Properties.Settings.Default.AutoUpdateCheck = true;
            HL7Tester.Properties.Settings.Default.Save();
        }

        CheckUpdateBox.Checked = HL7Tester.Properties.Settings.Default.AutoUpdateCheck;
        string lastIpAddress = HL7Tester.Properties.Settings.Default.LastIpAddress;
        string lastPort = HL7Tester.Properties.Settings.Default.LastPort;

        txtIpAddress.Text = lastIpAddress;
        txtPort.Text = lastPort;
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
        materialSkinManager.ColorScheme = new ColorScheme(
            Primary.BlueGrey800,
            Primary.BlueGrey900,
            Primary.BlueGrey500,
            Accent.Blue200,
            TextShade.WHITE
                );
        isInitializing = false;

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


    private void btnSend_MouseEnter(object sender, EventArgs e)
    {
        btnSend.UseAccentColor = true;
        btnSend.NoAccentTextColor = Color.White;

    }

    private void btnSend_MouseLeave(object sender, EventArgs e)
    {
        btnSend.UseAccentColor = false;
        btnSend.NoAccentTextColor = Color.Empty;
        //btnSend.Invalidate(); 
    }

    private void btnSend_MouseDown(object sender, MouseEventArgs e)
    {
        btnSend.UseAccentColor = false;
        btnSend.NoAccentTextColor = Color.White;
        btnSend.MouseState = MaterialSkin.MouseState.DOWN;
        btnSend.Invalidate();
    }

    private void btnSend_MouseUp(object sender, MouseEventArgs e)
    {
        btnSend.UseAccentColor = true;
        btnSend.NoAccentTextColor = Color.Empty;
        btnSend.MouseState = MaterialSkin.MouseState.HOVER;
        btnSend.Invalidate();
    }
    private void btnCancel_MouseEnter(object sender, EventArgs e)
    {
        btnCancel.UseAccentColor = true;
        btnCancel.NoAccentTextColor = Color.White;
        //btnCancel.Invalidate(); 
    }

    private void btnCancel_MouseLeave(object sender, EventArgs e)
    {
        btnCancel.UseAccentColor = false;
        btnCancel.NoAccentTextColor = Color.Empty;
        //btnCancel.Invalidate(); 
    }

    private void btnCancel_MouseDown(object sender, MouseEventArgs e)
    {
        btnCancel.UseAccentColor = false;
        btnCancel.NoAccentTextColor = Color.White;
        btnCancel.MouseState = MaterialSkin.MouseState.DOWN;
        btnCancel.Invalidate();
    }

    private void btnCancel_MouseUp(object sender, MouseEventArgs e)
    {
        btnCancel.UseAccentColor = true;
        btnCancel.NoAccentTextColor = Color.Empty;
        btnCancel.MouseState = MaterialSkin.MouseState.HOVER;
        btnCancel.Invalidate();
    }




    private void btnCancel_Click(object sender, EventArgs e)
    {
        this.Close();
    }

    private void btnSend_Click(object sender, EventArgs e)
    {
        string currentIpAddress = HL7Tester.Properties.Settings.Default.LastIpAddress;
        string currentPort = HL7Tester.Properties.Settings.Default.LastPort;

        if (txtIpAddress.Text != currentIpAddress || txtPort.Text != currentPort)
        {
            HL7Tester.Properties.Settings.Default.LastIpAddress = txtIpAddress.Text;
            HL7Tester.Properties.Settings.Default.LastPort = txtPort.Text;
            HL7Tester.Properties.Settings.Default.Save();
            logger.Info($"\nChanging IP to {txtIpAddress.Text}:{txtPort.Text}\n");
        }

        this.Close();
    }
    /// future improvements
    /*private void btnSend_Click(object sender, EventArgs e)
    {
        string newIpAddress = cmbIpAddress.Text.Trim();
        string selectedIpAddress = cmbIpAddress.SelectedItem?.ToString() ?? string.Empty;
        string currentIpAddress = HL7Tester.Properties.Settings.Default.LastIpAddress;
        string currentPort = HL7Tester.Properties.Settings.Default.LastPort;
        if (string.IsNullOrEmpty(newIpAddress))
        {
            MessageBox.Show("Please enter a valid IP address.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (newIpAddress != currentIpAddress || txtPort.Text != currentPort)
        {
            HL7Tester.Properties.Settings.Default.LastIpAddress3 = HL7Tester.Properties.Settings.Default.LastIpAddress2;
            HL7Tester.Properties.Settings.Default.LastIpAddress2 = HL7Tester.Properties.Settings.Default.LastIpAddress;
            HL7Tester.Properties.Settings.Default.LastIpAddress = selectedIpAddress;
            HL7Tester.Properties.Settings.Default.LastPort = txtPort.Text;
            HL7Tester.Properties.Settings.Default.Save();

            logger.Info($"\nChanging IP to {selectedIpAddress}:{txtPort.Text}\n");
        }
        this.Close();
    }*/


    private void CheckUpdateBox_CheckedChanged(object sender, EventArgs e)
    {
        if (isInitializing)
            return;
        HL7Tester.Properties.Settings.Default.AutoUpdateCheck = CheckUpdateBox.Checked;
        HL7Tester.Properties.Settings.Default.Save();
        logger.Info($"\nUpdate check setting changed to {CheckUpdateBox.Checked}\n");
    }

}