using System.Windows.Forms;
using NHapi.Base.Model;
using HL7Tester;
using NHapi.Model.V23.Message;
using NHapi.Model.V23.Segment;
using MaterialSkin.Controls;
using Microsoft.Win32;
using MaterialSkin;
using NLog;

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
            HL7Tester.Properties.Settings.Default.LastIpAddress9 = HL7Tester.Properties.Settings.Default.LastIpAddress8;
            HL7Tester.Properties.Settings.Default.LastIpAddress8 = HL7Tester.Properties.Settings.Default.LastIpAddress7;
            HL7Tester.Properties.Settings.Default.LastIpAddress7 = HL7Tester.Properties.Settings.Default.LastIpAddress6;
            HL7Tester.Properties.Settings.Default.LastIpAddress6 = HL7Tester.Properties.Settings.Default.LastIpAddress5;
            HL7Tester.Properties.Settings.Default.LastIpAddress5 = HL7Tester.Properties.Settings.Default.LastIpAddress4;
            HL7Tester.Properties.Settings.Default.LastIpAddress4 = HL7Tester.Properties.Settings.Default.LastIpAddress3;
            HL7Tester.Properties.Settings.Default.LastIpAddress3 = HL7Tester.Properties.Settings.Default.LastIpAddress2;
            HL7Tester.Properties.Settings.Default.LastIpAddress2 = HL7Tester.Properties.Settings.Default.LastIpAddress1;
            HL7Tester.Properties.Settings.Default.LastIpAddress1 = HL7Tester.Properties.Settings.Default.LastIpAddress;
            HL7Tester.Properties.Settings.Default.LastIpAddress = txtIpAddress.Text;
            HL7Tester.Properties.Settings.Default.LastPort9 = HL7Tester.Properties.Settings.Default.LastPort8;
            HL7Tester.Properties.Settings.Default.LastPort8 = HL7Tester.Properties.Settings.Default.LastPort7;
            HL7Tester.Properties.Settings.Default.LastPort7 = HL7Tester.Properties.Settings.Default.LastPort6;
            HL7Tester.Properties.Settings.Default.LastPort6 = HL7Tester.Properties.Settings.Default.LastPort5;
            HL7Tester.Properties.Settings.Default.LastPort5 = HL7Tester.Properties.Settings.Default.LastPort4;
            HL7Tester.Properties.Settings.Default.LastPort4 = HL7Tester.Properties.Settings.Default.LastPort3;
            HL7Tester.Properties.Settings.Default.LastPort3 = HL7Tester.Properties.Settings.Default.LastPort2;
            HL7Tester.Properties.Settings.Default.LastPort2 = HL7Tester.Properties.Settings.Default.LastPort1;
            HL7Tester.Properties.Settings.Default.LastPort1 = HL7Tester.Properties.Settings.Default.LastPort;
            HL7Tester.Properties.Settings.Default.LastPort = txtPort.Text;
            HL7Tester.Properties.Settings.Default.Save();
            logger.Info($"\nChanging IP to {txtIpAddress.Text}:{txtPort.Text}\n");
        }

        this.Close();
    }
    private void CheckUpdateBox_CheckedChanged(object sender, EventArgs e)
    {
        if (isInitializing)
            return;
        HL7Tester.Properties.Settings.Default.AutoUpdateCheck = CheckUpdateBox.Checked;
        HL7Tester.Properties.Settings.Default.Save();
        logger.Info($"\nUpdate check setting changed to {CheckUpdateBox.Checked}\n");
    }

    private void IPLabel_Click(object sender, EventArgs e)
    {
        if (Application.OpenForms.OfType<history>().Any())
        {
            history existingForm = Application.OpenForms.OfType<history>().First();

            existingForm.BringToFront();
        }
        else
        {
            history historyForm = new history();
            historyForm.Show();
        }
    }
}