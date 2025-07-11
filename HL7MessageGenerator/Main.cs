
using MaterialSkin;
using MaterialSkin.Controls;
using NHapi.Base.Model;
using NHapi.Base.Parser;
using NHapi.Model.V23.Message;
using NHapi.Model.V23.Segment;
using NLog;
using NLog.Config;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using System.Drawing.Drawing2D;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using static HL7Tester.Main;


namespace HL7Tester
{
    public partial class Main : MaterialForm
    {

        private async void Form1_Shown(object sender, EventArgs e)
        {

            await UpdateChecker.CheckForUpdateAsync();

        }
        public Main()
        {

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            InitializeComponent();
            InitializeComboBox();
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

        }
        private static void SafeSetClipboardText(string text)
        {
            var thread = new Thread(() => Clipboard.SetText(text));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
        }
        public class UpdateChecker
        {
            private static readonly string GitHubApiUrl = "https://api.github.com/repos/GlisseManTV/HL7Tester/releases/latest";

            private static void SafeSetClipboardText(string text)
            {
                var thread = new Thread(() => Clipboard.SetText(text));
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
            }

            public static async Task CheckForUpdateAsync()
            {
                if (Properties.Settings.Default.AutoUpdateCheck != true)
                {
                    MessageBox.Show("Auto-update is disabled.\n", "Auto-Update Disabled", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    logger.Info("\nAuto-update is disabled.\n");
                    return;
                }

                try
                {
                    using var client = new HttpClient();
                    client.DefaultRequestHeaders.UserAgent.ParseAdd("request");
                    var response = await client.GetStringAsync(GitHubApiUrl);

                    using var doc = JsonDocument.Parse(response);
                    var latestVersionString = doc.RootElement.GetProperty("tag_name").GetString();

                    var latestVersion = new Version(latestVersionString.TrimStart('v'));
                    var currentVersion = Assembly.GetExecutingAssembly().GetName().Version;



                    if (latestVersion > currentVersion)
                    {
                        string downloadUrl = null;

                        foreach (var asset in doc.RootElement.GetProperty("assets").EnumerateArray())
                        {
                            var name = asset.GetProperty("name").GetString();
                            if (name.EndsWith("setup.msi", StringComparison.OrdinalIgnoreCase))
                            {
                                downloadUrl = asset.GetProperty("browser_download_url").GetString();
                                break;
                            }

                        }
                        logger.Info($"\nUpdate check: \nCurrent version {currentVersion}\nLatest version {latestVersion}\n");
                        var result = MessageBox.Show(
                            $"A new version v{latestVersion} is available.\n" +
                            $"You are currently using v{currentVersion}.\n\n" +
                            "Would you like to be redirected to the download page?\n",
                            "Update available",
                            MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information);

                        if (result == DialogResult.Yes)
                        {
                            try
                            {
                                var psi = new System.Diagnostics.ProcessStartInfo
                                {
                                    FileName = "cmd",
                                    Arguments = "/c start https://github.com/GlisseManTV/HL7Tester/releases/latest",
                                    CreateNoWindow = true,
                                    UseShellExecute = false
                                };
                                System.Diagnostics.Process.Start(psi);
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Unable to open browser. Please visit :\nhttps://github.com/GlisseManTV/HL7Tester/releases/latest",
                                                "Error",
                                                MessageBoxButtons.OK,
                                                MessageBoxIcon.Error);
                                logger.Error($"Browser opening:\nFailed: {ex.Message}\n");
                            }
                        }
                    }
                    else if (latestVersion < currentVersion)
                    {
                        MessageBox.Show($"You are using a development version {currentVersion}.\n" +
                                        "This version may not be stable and is intended for testing purposes only.",
                                        "Development Version",
                                        MessageBoxButtons.OK,
                                        MessageBoxIcon.Warning);
                        logger.Warn($"\nUpdate check: \nCurrent version {currentVersion} is a development version.\n");
                    }
                    else
                    {
                        MessageBox.Show("You are using the latest version of the application.", "No Update Available", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        logger.Info("\nUpdate check: \nNo update available.\n");
                    }
                }
                catch (Exception ex)
                {

                    Console.WriteLine("Update check failed : " + ex.Message);
                    logger.Error($"\nUpdate check: \nFailed: {ex.Message}\n");
                }
            }
        }
        public static void CheckAndCreateLogDirectory()
        {
            string logDirectoryPath = @"C:\Users\Public\Documents\HL7_Logs\";
            string ipAddress = Properties.Settings.Default.LastIpAddress;
            string port = Properties.Settings.Default.LastPort;
            if (!Directory.Exists(logDirectoryPath))
            {
                Directory.CreateDirectory(logDirectoryPath);
                MessageBox.Show($"Logs folder has been created at: {logDirectoryPath}\nConfigured IP: {ipAddress}, Port: {port}",
                                "Folder Created",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"Logs folder is here: {logDirectoryPath}\nConfigured IP: {ipAddress}, Port: {port}",
                                "Folder Exists",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
            }
        }

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
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
            //Windows 11
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

        public class RoundedMaterialButton : MaterialButton
        {

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                GraphicsPath path = new GraphicsPath();
                int radius = 20;
                path.StartFigure();
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(Width - radius - 1, 0, radius, radius, 270, 90);
                path.AddArc(Width - radius - 1, Height - radius - 1, radius, radius, 0, 90);
                path.AddArc(0, Height - radius - 1, radius, radius, 90, 90);
                path.CloseFigure();
                this.Region = new Region(path);
            }
        }
        private void btnGenerate_MouseEnter(object sender, EventArgs e)
        {
            btnGenerate.UseAccentColor = true;
            btnGenerate.NoAccentTextColor = Color.White;
            //btnGenerate.Invalidate(); 
        }
        private void btnGenerate_MouseLeave(object sender, EventArgs e)
        {
            btnGenerate.UseAccentColor = false;
            btnGenerate.NoAccentTextColor = Color.Empty;
            //btnGenerate.Invalidate(); 
        }
        private void btnGenerate_MouseDown(object sender, MouseEventArgs e)
        {
            btnGenerate.UseAccentColor = false;
            btnGenerate.NoAccentTextColor = Color.White;
            btnGenerate.MouseState = MaterialSkin.MouseState.DOWN;
            btnGenerate.Invalidate();
        }
        private void btnGenerate_MouseUp(object sender, MouseEventArgs e)
        {
            btnGenerate.UseAccentColor = true;
            btnGenerate.NoAccentTextColor = Color.Empty;
            btnGenerate.MouseState = MaterialSkin.MouseState.HOVER;
            btnGenerate.Invalidate();
        }
        private void CopyButton_MouseEnter(object sender, EventArgs e)
        {
            CopyButton.UseAccentColor = true;
            CopyButton.NoAccentTextColor = Color.White;
            //btnGenerate.Invalidate(); 
        }
        private void CopyButton_MouseLeave(object sender, EventArgs e)
        {
            CopyButton.UseAccentColor = false;
            CopyButton.NoAccentTextColor = Color.Empty;
            //btnGenerate.Invalidate(); 
        }
        private void CopyButton_MouseDown(object sender, MouseEventArgs e)
        {
            CopyButton.UseAccentColor = false;
            CopyButton.NoAccentTextColor = Color.White;
            CopyButton.MouseState = MaterialSkin.MouseState.DOWN;
            CopyButton.Invalidate();
        }
        private void CopyButton_MouseUp(object sender, MouseEventArgs e)
        {
            CopyButton.UseAccentColor = true;
            CopyButton.NoAccentTextColor = Color.Empty;
            CopyButton.MouseState = MaterialSkin.MouseState.HOVER;
            CopyButton.Invalidate();
        }
        private void ResetButton_MouseEnter(object sender, EventArgs e)
        {
            ResetButton.UseAccentColor = false;
            ResetButton.NoAccentTextColor = Color.White;
            //btnGenerate.Invalidate(); 
        }
        private void ResetButton_MouseLeave(object sender, EventArgs e)
        {
            ResetButton.UseAccentColor = true;
            ResetButton.NoAccentTextColor = Color.Empty;
            //btnGenerate.Invalidate(); 
        }
        private void ResetButton_MouseDown(object sender, MouseEventArgs e)
        {
            ResetButton.UseAccentColor = true;
            ResetButton.NoAccentTextColor = Color.White;
            ResetButton.MouseState = MaterialSkin.MouseState.DOWN;
            ResetButton.Invalidate();
        }
        private void ResetButton_MouseUp(object sender, MouseEventArgs e)
        {
            ResetButton.UseAccentColor = false;
            ResetButton.NoAccentTextColor = Color.Empty;
            ResetButton.MouseState = MaterialSkin.MouseState.HOVER;
            ResetButton.Invalidate();
        }
        private void LogsButton_MouseEnter(object sender, EventArgs e)
        {
            LogsButton.UseAccentColor = true;
            LogsButton.NoAccentTextColor = Color.White;
            //btnGenerate.Invalidate(); 
        }
        private void LogsButton_MouseLeave(object sender, EventArgs e)
        {
            LogsButton.UseAccentColor = false;
            LogsButton.NoAccentTextColor = Color.Empty;
            //btnGenerate.Invalidate(); 
        }
        private void LogsButton_MouseDown(object sender, MouseEventArgs e)
        {
            LogsButton.UseAccentColor = false;
            LogsButton.NoAccentTextColor = Color.White;
            LogsButton.MouseState = MaterialSkin.MouseState.DOWN;
            LogsButton.Invalidate();
        }
        private void LogsButton_MouseUp(object sender, MouseEventArgs e)
        {
            LogsButton.UseAccentColor = true;
            LogsButton.NoAccentTextColor = Color.Empty;
            LogsButton.MouseState = MaterialSkin.MouseState.HOVER;
            LogsButton.Invalidate();
        }
        private void SettingsButton_MouseEnter(object sender, EventArgs e)
        {
            SettingsButton.UseAccentColor = true;
            SettingsButton.NoAccentTextColor = Color.White;
            //btnGenerate.Invalidate(); 
        }
        private void SettingsButton_MouseLeave(object sender, EventArgs e)
        {
            SettingsButton.UseAccentColor = false;
            SettingsButton.NoAccentTextColor = Color.Empty;
            //btnGenerate.Invalidate(); 
        }
        private void SettingsButton_MouseDown(object sender, MouseEventArgs e)
        {
            SettingsButton.UseAccentColor = false;
            SettingsButton.NoAccentTextColor = Color.White;
            SettingsButton.MouseState = MaterialSkin.MouseState.DOWN;
            SettingsButton.Invalidate();
        }
        private void SettingsButton_MouseUp(object sender, MouseEventArgs e)
        {
            SettingsButton.UseAccentColor = true;
            SettingsButton.NoAccentTextColor = Color.Empty;
            SettingsButton.MouseState = MaterialSkin.MouseState.HOVER;
            SettingsButton.Invalidate();
        }
        private void btnGenerateAndSend_MouseEnter(object sender, EventArgs e)
        {
            btnGenerateAndSend.UseAccentColor = false;
            btnGenerateAndSend.NoAccentTextColor = Color.White;
            //btnGenerate.Invalidate(); 
        }
        private void btnGenerateAndSend_MouseLeave(object sender, EventArgs e)
        {
            btnGenerateAndSend.UseAccentColor = true;
            btnGenerateAndSend.NoAccentTextColor = Color.Empty;
            //btnGenerate.Invalidate(); 
        }
        private void btnGenerateAndSend_MouseDown(object sender, MouseEventArgs e)
        {
            btnGenerateAndSend.UseAccentColor = true;
            btnGenerateAndSend.NoAccentTextColor = Color.White;
            btnGenerateAndSend.MouseState = MaterialSkin.MouseState.DOWN;
            btnGenerateAndSend.Invalidate();
        }
        private void btnGenerateAndSend_MouseUp(object sender, MouseEventArgs e)
        {
            btnGenerateAndSend.UseAccentColor = false;
            btnGenerateAndSend.NoAccentTextColor = Color.Empty;
            btnGenerateAndSend.MouseState = MaterialSkin.MouseState.HOVER;
            btnGenerateAndSend.Invalidate();
        }
        public class RoundedMaterialTextBox : MaterialTextBox
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                GraphicsPath path = new GraphicsPath();
                int radius = 50;
                path.StartFigure();
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(Width - radius - 1, 0, radius, radius, 270, 90);
                path.AddArc(Width - radius - 1, Height - radius - 1, radius, radius, 0, 90);
                path.AddArc(0, Height - radius - 1, radius, radius, 90, 90);
                path.CloseFigure();
                this.Region = new Region(path);
            }
        }
        public class RoundedMaterialMultiLineTextBox2 : MaterialSkin.Controls.MaterialMultiLineTextBox2
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                GraphicsPath path = new GraphicsPath();
                int radius = 50;
                path.StartFigure();
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(Width - radius - 1, 0, radius, radius, 270, 90);
                path.AddArc(Width - radius - 1, Height - radius - 1, radius, radius, 0, 90);
                path.AddArc(0, Height - radius - 1, radius, radius, 90, 90);
                path.CloseFigure();
                this.Region = new Region(path);
            }
        }
        public class RoundedMaterialComboBox : MaterialSkin.Controls.MaterialComboBox
        {
            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                GraphicsPath path = new GraphicsPath();
                int radius = 50;
                path.StartFigure();
                path.AddArc(0, 0, radius, radius, 180, 90);
                path.AddArc(Width - radius - 1, 0, radius, radius, 270, 90);
                path.AddArc(Width - radius - 1, Height - radius - 1, radius, radius, 0, 90);
                path.AddArc(0, Height - radius - 1, radius, radius, 90, 90);
                path.CloseFigure();
                this.Region = new Region(path);
            }
        }
        private void InitializeComboBox()
        {
            comboMessageType.Items.Add("ADT A01 - Inpatient or Day Hospital Admission");
            comboMessageType.Items.Add("ADT A02 - Patient Movement");
            comboMessageType.Items.Add("ADT A03 - Discharge");
            comboMessageType.Items.Add("ADT A04 - Outpatient Admission");
            comboMessageType.Items.Add("ADT A05 - Pre-admission");
            comboMessageType.Items.Add("ADT A06 - Transformation of an Outpatient Visit into Admission");
            comboMessageType.Items.Add("ADT A07 - Transformation of an Admission into Outpatient Visit");
            comboMessageType.Items.Add("ADT A08 - Update Patient Stay");
            comboMessageType.Items.Add("ADT A09 - Temporary Movement");
            comboMessageType.Items.Add("ADT A10 - Return from Temporary Movement");
            comboMessageType.Items.Add("ADT A11 - Admission Cancellation");
            comboMessageType.Items.Add("ADT A12 - Movement Cancellation");
            comboMessageType.Items.Add("ADT A13 - Discharge Cancellation");
            comboMessageType.Items.Add("ADT A14 - Scheduled Admission in the Future (not used)");
            comboMessageType.Items.Add("ADT A15 - Scheduled Movement in the Future (not used)");
            comboMessageType.Items.Add("ADT A16 - Scheduled Discharge in the Future (not used)");
            comboMessageType.Items.Add("ADT A18 - Merge Patient Records");
            comboMessageType.Items.Add("ADT A21 - Leave of Absence Departure");
            comboMessageType.Items.Add("ADT A22 - Return from Leave of Absence");
            comboMessageType.Items.Add("ADT A24 - Link between Two Patients (not used)");
            comboMessageType.Items.Add("ADT A25 - Cancellation of Future Scheduled Admission (not used)");
            comboMessageType.Items.Add("ADT A26 - Cancellation of Future Scheduled Movement (not used)");
            comboMessageType.Items.Add("ADT A27 - Cancellation of Future Scheduled Discharge (not used)");
            comboMessageType.Items.Add("ADT A28 - Patient Creation");
            comboMessageType.Items.Add("ADT A31 - Update Patient");
            comboMessageType.Items.Add("ADT A32 - Cancellation of a Return from Temporary Movement");
            comboMessageType.Items.Add("ADT A33 - Cancellation of Temporary Movement");
            comboMessageType.Items.Add("ADT A37 - Cancellation of a Patient Link (not used)");
            comboMessageType.Items.Add("ADT A38 - Pre-admission Cancellation");
            comboMessageType.Items.Add("ADT A40 - Patient Record Merge");
            comboMessageType.SelectedIndex = 0;
            comboMessageType.SelectedIndexChanged += ComboMessageType_SelectedIndexChanged;
            UpdateFormForSelectedType();
        }
        private string GenerateControlIDFromMessage(string message)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.GetEncoding("windows-1252").GetBytes(message));
                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString().Substring(0, 19);
            }
        }

        private void ComboMessageType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateFormForSelectedType();
        }
        private void UpdateFormForSelectedType()
        {
            lblPatientID.Visible = true;
            txtPatientID.Visible = true;
            lblPatientName.Visible = true;
            txtPatientName.Visible = true;
            lblPatGivenName.Visible = true;
            txtPatGivenName.Visible = true;
            lblBirthDate.Visible = true;
            txtBirthDate.Visible = true;
            lblSex.Visible = true;
            txtSex.Visible = true;
            lblRoom.Visible = true;
            txtRoom.Visible = true;
            lblBed.Visible = true;
            txtBed.Visible = true;
            lblAdmissionNumber.Visible = true;
            txtAdmissionNumber.Visible = true;
            UnitLabel.Visible = true;
            UnitTextBox.Visible = true;
            FloorLabel.Visible = true;
            FloorTextBox.Visible = true;
            lblEVENT.Visible = true;
            txtEVENT.Visible = true;
            TxtFacility.Visible = true;
            OBXReason1TextBox.Visible = true;
            OBXReason2TextBox.Visible = true;
            OBXReason3TextBox.Visible = true;
            OBXType1TextBox.Visible = true;
            OBXType2TextBox.Visible = true;
            OBXType3TextBox.Visible = true;
            OBXReasonLabel1.Visible = true;
            OBXReasonLabel2.Visible = true;
            OBXReasonLabel3.Visible = true;
            OBX1TypeLabel.Visible = true;
            OBX2TypeLabel.Visible = true;
            OBXTypeLabel3.Visible = true;
            materialLabel1.Visible = true;
            string selectedType = comboMessageType.SelectedItem.ToString();
            lblPatientID.Text = $"Patient Nr ({selectedType.Split('-')[0].Trim()}):";

            if (selectedType.StartsWith("ADT A03") || selectedType.StartsWith("ADT A11"))
            {
                lblDischargeReason.Visible = true;
                txtDischargeReason.Visible = true;
            }
            else
            {
                lblDischargeReason.Visible = false;
                txtDischargeReason.Visible = false;
            }

            if (selectedType.StartsWith("ADT A21") || selectedType.StartsWith("ADT A22"))
            {
                lblLeaveOfAbsenceReason.Visible = true;
                txtLeaveOfAbsenceReason.Visible = true;
            }
            else
            {
                lblLeaveOfAbsenceReason.Visible = false;
                txtLeaveOfAbsenceReason.Visible = false;
            }
            if (selectedType.StartsWith("ADT A40") || selectedType.StartsWith("ADT A18"))
            {
                lblNewPatientID.Visible = true;
                txtNewPatientID.Visible = true;
            }
            else
            {
                lblNewPatientID.Visible = false;
                txtNewPatientID.Visible = false;
            }
            if (selectedType.Contains("not used"))
            {

            }
        }
        private void btnGenerate_Click(object sender, EventArgs e)
        {
            try
            {
                IMessage message;
                string selectedType = comboMessageType.SelectedItem.ToString();

                switch (selectedType.Split('-')[0].Trim())
                {
                    case "ADT A01":
                        message = new ADT_A01();
                        break;
                    case "ADT A02":
                        message = new ADT_A02();
                        break;
                    case "ADT A03":
                        message = new ADT_A03();
                        break;
                    case "ADT A04":
                        message = new ADT_A04();
                        break;
                    case "ADT A05":
                        message = new ADT_A05();
                        break;
                    case "ADT A06":
                        message = new ADT_A06();
                        break;
                    case "ADT A07":
                        message = new ADT_A07();
                        break;
                    case "ADT A08":
                        message = new ADT_A08();
                        break;
                    case "ADT A09":
                        message = new ADT_A09();
                        break;
                    case "ADT A10":
                        message = new ADT_A10();
                        break;
                    case "ADT A11":
                        message = new ADT_A11();
                        break;
                    case "ADT A12":
                        message = new ADT_A12();
                        break;
                    case "ADT A13":
                        message = new ADT_A13();
                        break;
                    case "ADT A14":
                        message = new ADT_A14();
                        break;
                    case "ADT A15":
                        message = new ADT_A15();
                        break;
                    case "ADT A16":
                        message = new ADT_A16();
                        break;
                    case "ADT A18":
                        message = new ADT_A18();
                        break;
                    case "ADT A21":
                        message = new ADT_A21();
                        break;
                    case "ADT A22":
                        message = new ADT_A22();
                        break;
                    case "ADT A24":
                        message = new ADT_A24();
                        break;
                    case "ADT A25":
                        message = new ADT_A25();
                        break;
                    case "ADT A26":
                        message = new ADT_A26();
                        break;
                    case "ADT A27":
                        message = new ADT_A27();
                        break;
                    case "ADT A28":
                        message = new ADT_A28();
                        break;
                    case "ADT A31":
                        message = new ADT_A31();
                        break;
                    case "ADT A32":
                        message = new ADT_A32();
                        break;
                    case "ADT A33":
                        message = new ADT_A33();
                        break;
                    case "ADT A37":
                        message = new ADT_A37();
                        break;
                    case "ADT A38":
                        message = new ADT_A38();
                        break;
                    case "ADT A40":
                        message = new ADT_A40();
                        break;
                    default:
                        throw new Exception("Unknown message type");
                }
                var msh = (MSH)message.GetStructure("MSH");
                msh.FieldSeparator.Value = "|";
                msh.EncodingCharacters.Value = "^~\\&";
                msh.SendingApplication.NamespaceID.Value = "Hl7Tester-" + Environment.UserName;
                msh.MessageType.MessageType.Value = selectedType.Split(' ')[0].Trim();
                msh.MessageType.TriggerEvent.Value = selectedType.Split(' ')[1].Trim();
                msh.ProcessingID.ProcessingID.Value = "P";
                msh.VersionID.Value = "2.3";
                msh.DateTimeOfMessage.TimeOfAnEvent.Value = DateTime.Now.ToString("yyyyMMddHHmmss");
                msh.CharacterSet.Value = "ASCII";
                string encodedMessage = new PipeParser().Encode(message);

                string controlID = GenerateControlIDFromMessage(encodedMessage);

                msh.MessageControlID.Value = controlID;

                if (!(message is ADT_A40 || message is ADT_A18))
                {
                    var pid = (PID)message.GetStructure("PID");
                    pid.SetIDPatientID.Value = "1";
                    pid.GetPatientIDInternalID(0).ID.Value = txtPatientID.Text;
                    pid.GetPatientName(0).FamilyName.Value = txtPatientName.Text;
                    pid.GetPatientName(0).GivenName.Value = txtPatGivenName.Text;
                    pid.DateOfBirth.TimeOfAnEvent.Value = txtBirthDate.Text;
                    pid.Sex.Value = txtSex.Text;

                    var pv1 = (PV1)message.GetStructure("PV1");
                    pv1.SetIDPatientVisit.Value = "1";
                    pv1.PatientClass.Value = "I";
                    pv1.AssignedPatientLocation.PointOfCare.Value = UnitTextBox.Text;
                    pv1.AssignedPatientLocation.Room.Value = txtRoom.Text;
                    pv1.AssignedPatientLocation.Bed.Value = txtBed.Text;
                    pv1.AssignedPatientLocation.Facility.NamespaceID.Value = TxtFacility.Text;
                    pv1.AssignedPatientLocation.Floor.Value = FloorTextBox.Text;
                    pv1.VisitNumber.ID.Value = txtAdmissionNumber.Text;
                    int obxIndex = 0;

                    for (int i = 1; i <= 3; i++)
                    {
                        string type = Controls.Find($"OBXType{i}TextBox", true).FirstOrDefault()?.Text;
                        string reason = Controls.Find($"OBXReason{i}TextBox", true).FirstOrDefault()?.Text;

                        if (!string.IsNullOrWhiteSpace(type) || !string.IsNullOrWhiteSpace(reason))
                        {
                            OBX obx = (OBX)message.GetStructure("OBX", obxIndex++);
                            obx.SetIDOBX.Value = (obxIndex).ToString();
                            obx.ValueType.Value = "TX";
                            obx.ObservationIdentifier.Identifier.Value = type ?? "UNK"; obx.ObservationSubID.Value = "1";
                            var value = new NHapi.Model.V23.Datatype.TX(message);
                            value.Value = reason ?? "Non spécifié";
                            obx.GetObservationValue(0).Data = value;
                            obx.ObservResultStatus.Value = "F";
                        }
                    }

                }
                else if (message is ADT_A40)
                {
                    var adtA40 = (ADT_A40)message;

                    var patient = adtA40.GetPATIENT();
                    var pidA40 = patient.PID;
                    pidA40.SetIDPatientID.Value = "1";
                    pidA40.GetPatientIDInternalID(0).ID.Value = txtNewPatientID.Text;
                    pidA40.GetPatientName(0).FamilyName.Value = txtPatientName.Text;
                    pidA40.GetPatientName(0).GivenName.Value = txtPatGivenName.Text;
                    pidA40.DateOfBirth.TimeOfAnEvent.Value = txtBirthDate.Text;
                    pidA40.Sex.Value = txtSex.Text;
                    var mrg = patient.MRG;
                    mrg.GetPriorPatientIDInternal(0).ID.Value = txtPatientID.Text;
                }
                else if (message is ADT_A18)
                {
                    var adtA18 = (ADT_A18)message;
                    var pidA18 = (PID)message.GetStructure("PID");
                    pidA18.SetIDPatientID.Value = "1";
                    pidA18.GetPatientIDInternalID(0).ID.Value = txtNewPatientID.Text;
                    pidA18.GetPatientName(0).FamilyName.Value = txtPatientName.Text;
                    pidA18.GetPatientName(0).GivenName.Value = txtPatGivenName.Text;
                    pidA18.DateOfBirth.TimeOfAnEvent.Value = txtBirthDate.Text;
                    pidA18.Sex.Value = txtSex.Text;
                    var mrg = (MRG)message.GetStructure("MRG");
                    mrg.GetPriorPatientIDInternal(0).ID.Value = txtPatientID.Text;
                }

                var evn = (EVN)message.GetStructure("EVN");
                string selectedEventType = comboMessageType.SelectedItem.ToString().Split(' ')[1].Trim();
                evn.EventTypeCode.Value = selectedEventType;
                evn.RecordedDateTime.TimeOfAnEvent.Value = DateTime.Now.ToString("yyyyMMddHHmm");
                if (string.IsNullOrEmpty(txtEVENT.Text))
                {
                    evn.EventOccured.TimeOfAnEvent.Value = DateTime.Now.ToString("yyyyMMddHHmm");
                }
                else
                {
                    evn.EventOccured.TimeOfAnEvent.Value = txtEVENT.Text;
                }
                string hl7Message = new PipeParser().Encode(message);
                hl7Message = hl7Message.Replace("\r", "\r\n");
                txtGeneratedMessage.Text = hl7Message;
                CopyButton.Visible = true;
                btnGenerateAndSend.Visible = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error when generating message : {ex.Message}");
            }
        }

        private void CopyButton_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtGeneratedMessage.Text))
            {
                SafeSetClipboardText(txtGeneratedMessage.Text);
            }
        }

        private void ResetButton_Click(object sender, EventArgs e)
        {
            comboMessageType.SelectedIndex = 0;
            txtPatientID.Text = "";
            txtNewPatientID.Text = "";
            txtPatientName.Text = "";
            txtPatGivenName.Text = "";
            txtBirthDate.Text = "";
            txtSex.Text = "";
            UnitTextBox.Text = "";
            txtRoom.Text = "";
            txtBed.Text = "";
            FloorTextBox.Text = "";
            txtAdmissionNumber.Text = "";
            txtEVENT.Text = "";
            TxtFacility.Text = "";
            OBXReason1TextBox.Text = "";
            OBXReason2TextBox.Text = "";
            OBXReason3TextBox.Text = "";
            OBXType1TextBox.Text = "";
            OBXType2TextBox.Text = "";
            OBXType3TextBox.Text = "";
        }

        private void SettingsButton_Click(object sender, EventArgs e)
        {
            if (Application.OpenForms.OfType<HL7Settings>().Any())
            {
                HL7Settings existingForm = Application.OpenForms.OfType<HL7Settings>().First();

                existingForm.BringToFront();
            }
            else
            {
                HL7Settings sendHL7Form = new HL7Settings();
                sendHL7Form.Show();
            }
        }


        private async void btnGenerateAndSend_Click(object sender, EventArgs e)
        {

            string lastIpAddress = Properties.Settings.Default.LastIpAddress;
            string lastPort = Properties.Settings.Default.LastPort;
            if (!IPAddress.TryParse(lastIpAddress, out var ip))
            {
                logger.Error("Invalid IP address.");
                return;
            }

            if (!int.TryParse(lastPort, out int port) || port < 1 || port > 65535)
            {
                logger.Error("Invalid network port.");
                return;
            }
            string message = txtGeneratedMessage.Text;
            byte[] sendBytes = new byte[0];
            if (string.IsNullOrEmpty(message))
            {
                logger.Warn("Nothing to send");
                return;
            }
            else
            {
                sendBytes = Encoding.GetEncoding("windows-1252").GetBytes("\x0B" + message + "\x1C\r");
            }
            try
            {
                using (TcpClient tcpClient = new TcpClient())
                {
                    await tcpClient.ConnectAsync(ip, port);
                    using (NetworkStream stream = tcpClient.GetStream())
                    {
                        await stream.WriteAsync(sendBytes, 0, sendBytes.Length);
                        logger.Info($"Sent to: {lastIpAddress}:{lastPort}\nContent:\n{message}");
                        using (var reader = new StreamReader(stream))
                        {
                            var ackMessage = await reader.ReadLineAsync();
                            logger.Info($"Server acknowledgement:\n{ackMessage}\n");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex, $"Error when sending message to: {lastIpAddress}:{lastPort}");
            }
        }

        private void LogsButton_Click(object sender, EventArgs e)
        {
            try
            {
                string logDirectoryPath = @"C:\Users\Public\Documents\HL7_Logs\";
                string logFileName = "application.log";
                string logFilePath = Path.Combine(logDirectoryPath, logFileName);

                if (File.Exists(logFilePath))
                {
                    ProcessStartInfo psi = new ProcessStartInfo(logFilePath)
                    {
                        UseShellExecute = true,
                        Verb = "open"
                    };

                    Process.Start(psi);
                }
                else
                {
                    MessageBox.Show($"The log file '{logFileName}'was not found in the: {logDirectoryPath}",
                                    "File not found",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error opening log file: {ex.Message}",
                                "Error",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }


    }
}
