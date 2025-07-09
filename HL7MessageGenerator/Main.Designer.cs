using MaterialSkin.Controls;

namespace HL7MessageGenerator
{
    partial class Main
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            comboMessageType = new RoundedMaterialComboBox();
            lblPatientID = new MaterialLabel();
            txtPatientID = new RoundedMaterialTextBox();
            lblNewPatientID = new MaterialLabel();
            txtNewPatientID = new RoundedMaterialTextBox();
            lblPatientName = new MaterialLabel();
            txtPatientName = new RoundedMaterialTextBox();
            lblPatGivenName = new MaterialLabel();
            txtPatGivenName = new RoundedMaterialTextBox();
            lblBirthDate = new MaterialLabel();
            txtBirthDate = new RoundedMaterialTextBox();
            lblSex = new MaterialLabel();
            txtSex = new RoundedMaterialTextBox();
            lblRoom = new MaterialLabel();
            txtRoom = new RoundedMaterialTextBox();
            lblBed = new MaterialLabel();
            txtBed = new RoundedMaterialTextBox();
            lblAdmissionNumber = new MaterialLabel();
            txtAdmissionNumber = new RoundedMaterialTextBox();
            btnGenerate = new RoundedMaterialButton();
            btnGenerateAndSend = new RoundedMaterialButton();
            txtGeneratedMessage = new RoundedMaterialMultiLineTextBox2();
            lblDischargeReason = new MaterialLabel();
            txtDischargeReason = new RoundedMaterialTextBox();
            lblLeaveOfAbsenceReason = new MaterialLabel();
            txtLeaveOfAbsenceReason = new RoundedMaterialTextBox();
            lblMessageType = new MaterialLabel();
            materialLabel1 = new MaterialLabel();
            CopyButton = new RoundedMaterialButton();
            ResetButton = new RoundedMaterialButton();
            SettingsButton = new RoundedMaterialButton();
            UnitLabel = new MaterialLabel();
            FloorLabel = new MaterialLabel();
            UnitTextBox = new RoundedMaterialTextBox();
            FloorTextBox = new RoundedMaterialTextBox();
            txtEVENT = new RoundedMaterialTextBox();
            lblEVENT = new MaterialLabel();
            TxtFacility = new RoundedMaterialTextBox();
            LblFacility = new MaterialLabel();
            OBXReason1TextBox = new RoundedMaterialTextBox();
            OBXReasonLabel1 = new MaterialLabel();
            OBXType1TextBox = new RoundedMaterialTextBox();
            OBX1TypeLabel = new MaterialLabel();
            OBXType2TextBox = new RoundedMaterialTextBox();
            OBX2TypeLabel = new MaterialLabel();
            OBXReason2TextBox = new RoundedMaterialTextBox();
            OBXReasonLabel2 = new MaterialLabel();
            OBXType3TextBox = new RoundedMaterialTextBox();
            OBXTypeLabel3 = new MaterialLabel();
            OBXReason3TextBox = new RoundedMaterialTextBox();
            OBXReasonLabel3 = new MaterialLabel();
            panel4 = new Panel();
            panel1 = new Panel();
            panel2 = new Panel();
            LogsButton = new RoundedMaterialButton();
            SuspendLayout();
            // 
            // comboMessageType
            // 
            comboMessageType.AutoResize = false;
            comboMessageType.BackColor = SystemColors.Window;
            comboMessageType.Depth = 0;
            comboMessageType.DrawMode = DrawMode.OwnerDrawVariable;
            comboMessageType.DropDownHeight = 174;
            comboMessageType.DropDownStyle = ComboBoxStyle.DropDownList;
            comboMessageType.DropDownWidth = 121;
            comboMessageType.Font = new Font("Corbel", 9F, FontStyle.Bold);
            comboMessageType.ForeColor = SystemColors.MenuText;
            comboMessageType.FormattingEnabled = true;
            comboMessageType.IntegralHeight = false;
            comboMessageType.ItemHeight = 43;
            comboMessageType.Location = new Point(13, 89);
            comboMessageType.Margin = new Padding(4, 3, 4, 3);
            comboMessageType.MaxDropDownItems = 4;
            comboMessageType.MouseState = MaterialSkin.MouseState.OUT;
            comboMessageType.Name = "comboMessageType";
            comboMessageType.Size = new Size(447, 49);
            comboMessageType.StartIndex = 0;
            comboMessageType.TabIndex = 0;
            // 
            // lblPatientID
            // 
            lblPatientID.AutoSize = true;
            lblPatientID.Depth = 0;
            lblPatientID.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblPatientID.Location = new Point(13, 154);
            lblPatientID.Margin = new Padding(4, 0, 4, 0);
            lblPatientID.MouseState = MaterialSkin.MouseState.HOVER;
            lblPatientID.Name = "lblPatientID";
            lblPatientID.Size = new Size(75, 19);
            lblPatientID.TabIndex = 1;
            lblPatientID.Text = "Patient Nr:";
            lblPatientID.Visible = false;
            // 
            // txtPatientID
            // 
            txtPatientID.AnimateReadOnly = false;
            txtPatientID.BackColor = SystemColors.WindowText;
            txtPatientID.BorderStyle = BorderStyle.None;
            txtPatientID.Depth = 0;
            txtPatientID.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtPatientID.LeadingIcon = null;
            txtPatientID.Location = new Point(12, 176);
            txtPatientID.Margin = new Padding(4, 3, 4, 3);
            txtPatientID.MaxLength = 50;
            txtPatientID.MouseState = MaterialSkin.MouseState.OUT;
            txtPatientID.Multiline = false;
            txtPatientID.Name = "txtPatientID";
            txtPatientID.Size = new Size(157, 50);
            txtPatientID.TabIndex = 2;
            txtPatientID.Text = "";
            txtPatientID.TrailingIcon = null;
            // 
            // lblNewPatientID
            // 
            lblNewPatientID.AutoSize = true;
            lblNewPatientID.Depth = 0;
            lblNewPatientID.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblNewPatientID.Location = new Point(171, 577);
            lblNewPatientID.Margin = new Padding(4, 0, 4, 0);
            lblNewPatientID.MouseState = MaterialSkin.MouseState.HOVER;
            lblNewPatientID.Name = "lblNewPatientID";
            lblNewPatientID.Size = new Size(110, 19);
            lblNewPatientID.TabIndex = 1;
            lblNewPatientID.Text = "New Patient Nr:";
            lblNewPatientID.Visible = false;
            // 
            // txtNewPatientID
            // 
            txtNewPatientID.AnimateReadOnly = false;
            txtNewPatientID.BorderStyle = BorderStyle.None;
            txtNewPatientID.Depth = 0;
            txtNewPatientID.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtNewPatientID.LeadingIcon = null;
            txtNewPatientID.Location = new Point(173, 599);
            txtNewPatientID.Margin = new Padding(4, 3, 4, 3);
            txtNewPatientID.MaxLength = 50;
            txtNewPatientID.MouseState = MaterialSkin.MouseState.OUT;
            txtNewPatientID.Multiline = false;
            txtNewPatientID.Name = "txtNewPatientID";
            txtNewPatientID.Size = new Size(179, 50);
            txtNewPatientID.TabIndex = 2;
            txtNewPatientID.Text = "";
            txtNewPatientID.TrailingIcon = null;
            txtNewPatientID.Visible = false;
            // 
            // lblPatientName
            // 
            lblPatientName.AutoSize = true;
            lblPatientName.Depth = 0;
            lblPatientName.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblPatientName.Location = new Point(12, 235);
            lblPatientName.Margin = new Padding(4, 0, 4, 0);
            lblPatientName.MouseState = MaterialSkin.MouseState.HOVER;
            lblPatientName.Name = "lblPatientName";
            lblPatientName.Size = new Size(165, 19);
            lblPatientName.TabIndex = 5;
            lblPatientName.Text = "Family Name: (PID-5.1)";
            lblPatientName.Visible = false;
            // 
            // txtPatientName
            // 
            txtPatientName.AnimateReadOnly = false;
            txtPatientName.BackColor = SystemColors.Window;
            txtPatientName.BorderStyle = BorderStyle.None;
            txtPatientName.Depth = 0;
            txtPatientName.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtPatientName.LeadingIcon = null;
            txtPatientName.Location = new Point(12, 257);
            txtPatientName.Margin = new Padding(4, 3, 4, 3);
            txtPatientName.MaxLength = 50;
            txtPatientName.MouseState = MaterialSkin.MouseState.OUT;
            txtPatientName.Multiline = false;
            txtPatientName.Name = "txtPatientName";
            txtPatientName.Size = new Size(157, 50);
            txtPatientName.TabIndex = 6;
            txtPatientName.Text = "";
            txtPatientName.TrailingIcon = null;
            txtPatientName.Visible = false;
            // 
            // lblPatGivenName
            // 
            lblPatGivenName.AutoSize = true;
            lblPatGivenName.Depth = 0;
            lblPatGivenName.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblPatGivenName.Location = new Point(185, 235);
            lblPatGivenName.Margin = new Padding(4, 0, 4, 0);
            lblPatGivenName.MouseState = MaterialSkin.MouseState.HOVER;
            lblPatGivenName.Name = "lblPatGivenName";
            lblPatGivenName.Size = new Size(157, 19);
            lblPatGivenName.TabIndex = 7;
            lblPatGivenName.Text = "Given Name: (PID-5.2)";
            lblPatGivenName.Visible = false;
            // 
            // txtPatGivenName
            // 
            txtPatGivenName.AnimateReadOnly = false;
            txtPatGivenName.BackColor = SystemColors.Window;
            txtPatGivenName.BorderStyle = BorderStyle.None;
            txtPatGivenName.Depth = 0;
            txtPatGivenName.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtPatGivenName.LeadingIcon = null;
            txtPatGivenName.Location = new Point(185, 257);
            txtPatGivenName.Margin = new Padding(4, 3, 4, 3);
            txtPatGivenName.MaxLength = 50;
            txtPatGivenName.MouseState = MaterialSkin.MouseState.OUT;
            txtPatGivenName.Multiline = false;
            txtPatGivenName.Name = "txtPatGivenName";
            txtPatGivenName.Size = new Size(153, 50);
            txtPatGivenName.TabIndex = 8;
            txtPatGivenName.Text = "";
            txtPatGivenName.TrailingIcon = null;
            txtPatGivenName.Visible = false;
            // 
            // lblBirthDate
            // 
            lblBirthDate.AutoSize = true;
            lblBirthDate.Depth = 0;
            lblBirthDate.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblBirthDate.Location = new Point(185, 317);
            lblBirthDate.Margin = new Padding(4, 0, 4, 0);
            lblBirthDate.MouseState = MaterialSkin.MouseState.HOVER;
            lblBirthDate.Name = "lblBirthDate";
            lblBirthDate.Size = new Size(168, 19);
            lblBirthDate.TabIndex = 11;
            lblBirthDate.Text = "Birth Date (yyyyMMdd):";
            lblBirthDate.Visible = false;
            // 
            // txtBirthDate
            // 
            txtBirthDate.AnimateReadOnly = false;
            txtBirthDate.BackColor = SystemColors.Window;
            txtBirthDate.BorderStyle = BorderStyle.None;
            txtBirthDate.Depth = 0;
            txtBirthDate.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtBirthDate.LeadingIcon = null;
            txtBirthDate.Location = new Point(185, 339);
            txtBirthDate.Margin = new Padding(4, 3, 4, 3);
            txtBirthDate.MaxLength = 50;
            txtBirthDate.MouseState = MaterialSkin.MouseState.OUT;
            txtBirthDate.Multiline = false;
            txtBirthDate.Name = "txtBirthDate";
            txtBirthDate.Size = new Size(153, 50);
            txtBirthDate.TabIndex = 12;
            txtBirthDate.Text = "";
            txtBirthDate.TrailingIcon = null;
            txtBirthDate.Visible = false;
            // 
            // lblSex
            // 
            lblSex.AutoSize = true;
            lblSex.Depth = 0;
            lblSex.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblSex.Location = new Point(13, 317);
            lblSex.Margin = new Padding(4, 0, 4, 0);
            lblSex.MouseState = MaterialSkin.MouseState.HOVER;
            lblSex.Name = "lblSex";
            lblSex.Size = new Size(31, 19);
            lblSex.TabIndex = 9;
            lblSex.Text = "Sex:";
            lblSex.Visible = false;
            // 
            // txtSex
            // 
            txtSex.AnimateReadOnly = false;
            txtSex.BackColor = SystemColors.Window;
            txtSex.BorderStyle = BorderStyle.None;
            txtSex.Depth = 0;
            txtSex.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtSex.LeadingIcon = null;
            txtSex.Location = new Point(12, 339);
            txtSex.Margin = new Padding(4, 3, 4, 3);
            txtSex.MaxLength = 50;
            txtSex.MouseState = MaterialSkin.MouseState.OUT;
            txtSex.Multiline = false;
            txtSex.Name = "txtSex";
            txtSex.Size = new Size(157, 50);
            txtSex.TabIndex = 10;
            txtSex.Text = "";
            txtSex.TrailingIcon = null;
            txtSex.Visible = false;
            // 
            // lblRoom
            // 
            lblRoom.AutoSize = true;
            lblRoom.Depth = 0;
            lblRoom.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblRoom.Location = new Point(173, 414);
            lblRoom.Margin = new Padding(4, 0, 4, 0);
            lblRoom.MouseState = MaterialSkin.MouseState.HOVER;
            lblRoom.Name = "lblRoom";
            lblRoom.Size = new Size(117, 19);
            lblRoom.TabIndex = 17;
            lblRoom.Text = "Room: (PV1-3.2)";
            lblRoom.Visible = false;
            // 
            // txtRoom
            // 
            txtRoom.AnimateReadOnly = false;
            txtRoom.BackColor = SystemColors.Window;
            txtRoom.BorderStyle = BorderStyle.None;
            txtRoom.Depth = 0;
            txtRoom.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtRoom.LeadingIcon = null;
            txtRoom.Location = new Point(173, 436);
            txtRoom.Margin = new Padding(4, 3, 4, 3);
            txtRoom.MaxLength = 50;
            txtRoom.MouseState = MaterialSkin.MouseState.OUT;
            txtRoom.Multiline = false;
            txtRoom.Name = "txtRoom";
            txtRoom.Size = new Size(152, 50);
            txtRoom.TabIndex = 18;
            txtRoom.Text = "";
            txtRoom.TrailingIcon = null;
            txtRoom.Visible = false;
            // 
            // lblBed
            // 
            lblBed.AutoSize = true;
            lblBed.Depth = 0;
            lblBed.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblBed.Location = new Point(13, 494);
            lblBed.Margin = new Padding(4, 0, 4, 0);
            lblBed.MouseState = MaterialSkin.MouseState.HOVER;
            lblBed.Name = "lblBed";
            lblBed.Size = new Size(102, 19);
            lblBed.TabIndex = 19;
            lblBed.Text = "Bed: (PV1-3.3)";
            lblBed.Visible = false;
            // 
            // txtBed
            // 
            txtBed.AnimateReadOnly = false;
            txtBed.BackColor = SystemColors.Window;
            txtBed.BorderStyle = BorderStyle.None;
            txtBed.Depth = 0;
            txtBed.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtBed.LeadingIcon = null;
            txtBed.Location = new Point(13, 518);
            txtBed.Margin = new Padding(4, 3, 4, 3);
            txtBed.MaxLength = 50;
            txtBed.MouseState = MaterialSkin.MouseState.OUT;
            txtBed.Multiline = false;
            txtBed.Name = "txtBed";
            txtBed.ScrollBars = RichTextBoxScrollBars.None;
            txtBed.Size = new Size(152, 50);
            txtBed.TabIndex = 20;
            txtBed.Text = "";
            txtBed.TrailingIcon = null;
            txtBed.Visible = false;
            // 
            // lblAdmissionNumber
            // 
            lblAdmissionNumber.AutoSize = true;
            lblAdmissionNumber.Depth = 0;
            lblAdmissionNumber.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblAdmissionNumber.Location = new Point(185, 154);
            lblAdmissionNumber.Margin = new Padding(4, 0, 4, 0);
            lblAdmissionNumber.MouseState = MaterialSkin.MouseState.HOVER;
            lblAdmissionNumber.Name = "lblAdmissionNumber";
            lblAdmissionNumber.Size = new Size(140, 19);
            lblAdmissionNumber.TabIndex = 3;
            lblAdmissionNumber.Text = "Admission Number:";
            lblAdmissionNumber.Visible = false;
            // 
            // txtAdmissionNumber
            // 
            txtAdmissionNumber.AnimateReadOnly = false;
            txtAdmissionNumber.BackColor = SystemColors.Window;
            txtAdmissionNumber.BorderStyle = BorderStyle.None;
            txtAdmissionNumber.Depth = 0;
            txtAdmissionNumber.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtAdmissionNumber.LeadingIcon = null;
            txtAdmissionNumber.Location = new Point(185, 176);
            txtAdmissionNumber.Margin = new Padding(4, 3, 4, 3);
            txtAdmissionNumber.MaxLength = 50;
            txtAdmissionNumber.MouseState = MaterialSkin.MouseState.OUT;
            txtAdmissionNumber.Multiline = false;
            txtAdmissionNumber.Name = "txtAdmissionNumber";
            txtAdmissionNumber.Size = new Size(153, 50);
            txtAdmissionNumber.TabIndex = 4;
            txtAdmissionNumber.Text = "";
            txtAdmissionNumber.TrailingIcon = null;
            txtAdmissionNumber.Visible = false;
            // 
            // btnGenerate
            // 
            btnGenerate.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnGenerate.Density = MaterialButton.MaterialButtonDensity.Default;
            btnGenerate.Depth = 0;
            btnGenerate.FlatStyle = FlatStyle.Flat;
            btnGenerate.HighEmphasis = true;
            btnGenerate.Icon = null;
            btnGenerate.Location = new Point(590, 25);
            btnGenerate.Margin = new Padding(4, 3, 4, 3);
            btnGenerate.MouseState = MaterialSkin.MouseState.HOVER;
            btnGenerate.Name = "btnGenerate";
            btnGenerate.NoAccentTextColor = Color.Empty;
            btnGenerate.Size = new Size(95, 36);
            btnGenerate.TabIndex = 0;
            btnGenerate.Text = "Generate";
            btnGenerate.Type = MaterialButton.MaterialButtonType.Contained;
            btnGenerate.UseAccentColor = false;
            btnGenerate.UseVisualStyleBackColor = true;
            btnGenerate.Click += btnGenerate_Click;
            btnGenerate.MouseDown += btnGenerate_MouseDown;
            btnGenerate.MouseEnter += btnGenerate_MouseEnter;
            btnGenerate.MouseLeave += btnGenerate_MouseLeave;
            btnGenerate.MouseUp += btnGenerate_MouseUp;
            // 
            // btnGenerateAndSend
            // 
            btnGenerateAndSend.AutoSize = false;
            btnGenerateAndSend.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btnGenerateAndSend.Density = MaterialButton.MaterialButtonDensity.Default;
            btnGenerateAndSend.Depth = 0;
            btnGenerateAndSend.FlatStyle = FlatStyle.Flat;
            btnGenerateAndSend.HighEmphasis = true;
            btnGenerateAndSend.Icon = null;
            btnGenerateAndSend.Location = new Point(464, 838);
            btnGenerateAndSend.Margin = new Padding(4, 3, 4, 3);
            btnGenerateAndSend.MouseState = MaterialSkin.MouseState.HOVER;
            btnGenerateAndSend.Name = "btnGenerateAndSend";
            btnGenerateAndSend.NoAccentTextColor = Color.Empty;
            btnGenerateAndSend.Size = new Size(138, 47);
            btnGenerateAndSend.TabIndex = 37;
            btnGenerateAndSend.Text = "Send";
            btnGenerateAndSend.Type = MaterialButton.MaterialButtonType.Contained;
            btnGenerateAndSend.UseAccentColor = true;
            btnGenerateAndSend.UseVisualStyleBackColor = true;
            btnGenerateAndSend.Visible = false;
            btnGenerateAndSend.Click += btnGenerateAndSend_Click;
            btnGenerateAndSend.MouseDown += btnGenerateAndSend_MouseDown;
            btnGenerateAndSend.MouseEnter += btnGenerateAndSend_MouseEnter;
            btnGenerateAndSend.MouseLeave += btnGenerateAndSend_MouseLeave;
            btnGenerateAndSend.MouseUp += btnGenerateAndSend_MouseUp;
            // 
            // txtGeneratedMessage
            // 
            txtGeneratedMessage.AnimateReadOnly = false;
            txtGeneratedMessage.BackColor = SystemColors.ButtonShadow;
            txtGeneratedMessage.BackgroundImageLayout = ImageLayout.None;
            txtGeneratedMessage.CharacterCasing = CharacterCasing.Normal;
            txtGeneratedMessage.Depth = 0;
            txtGeneratedMessage.Font = new Font("Corbel", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
            txtGeneratedMessage.HideSelection = true;
            txtGeneratedMessage.Location = new Point(8, 688);
            txtGeneratedMessage.Margin = new Padding(4, 3, 4, 3);
            txtGeneratedMessage.MaxLength = 9999;
            txtGeneratedMessage.MouseState = MaterialSkin.MouseState.OUT;
            txtGeneratedMessage.Name = "txtGeneratedMessage";
            txtGeneratedMessage.PasswordChar = '\0';
            txtGeneratedMessage.ReadOnly = false;
            txtGeneratedMessage.ScrollBars = ScrollBars.None;
            txtGeneratedMessage.SelectedText = "";
            txtGeneratedMessage.SelectionLength = 0;
            txtGeneratedMessage.SelectionStart = 0;
            txtGeneratedMessage.ShortcutsEnabled = true;
            txtGeneratedMessage.Size = new Size(677, 144);
            txtGeneratedMessage.TabIndex = 50;
            txtGeneratedMessage.TabStop = false;
            txtGeneratedMessage.TextAlign = HorizontalAlignment.Left;
            txtGeneratedMessage.UseSystemPasswordChar = false;
            // 
            // lblDischargeReason
            // 
            lblDischargeReason.AutoSize = true;
            lblDischargeReason.Depth = 0;
            lblDischargeReason.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblDischargeReason.Location = new Point(171, 577);
            lblDischargeReason.Margin = new Padding(4, 0, 4, 0);
            lblDischargeReason.MouseState = MaterialSkin.MouseState.HOVER;
            lblDischargeReason.Name = "lblDischargeReason";
            lblDischargeReason.Size = new Size(133, 19);
            lblDischargeReason.TabIndex = 15;
            lblDischargeReason.Text = "Discharge Reason:";
            lblDischargeReason.Visible = false;
            // 
            // txtDischargeReason
            // 
            txtDischargeReason.AnimateReadOnly = false;
            txtDischargeReason.BorderStyle = BorderStyle.None;
            txtDischargeReason.Depth = 0;
            txtDischargeReason.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtDischargeReason.LeadingIcon = null;
            txtDischargeReason.Location = new Point(171, 599);
            txtDischargeReason.Margin = new Padding(4, 3, 4, 3);
            txtDischargeReason.MaxLength = 50;
            txtDischargeReason.MouseState = MaterialSkin.MouseState.OUT;
            txtDischargeReason.Multiline = false;
            txtDischargeReason.Name = "txtDischargeReason";
            txtDischargeReason.Size = new Size(181, 50);
            txtDischargeReason.TabIndex = 16;
            txtDischargeReason.Text = "";
            txtDischargeReason.TrailingIcon = null;
            txtDischargeReason.Visible = false;
            // 
            // lblLeaveOfAbsenceReason
            // 
            lblLeaveOfAbsenceReason.AutoSize = true;
            lblLeaveOfAbsenceReason.Depth = 0;
            lblLeaveOfAbsenceReason.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblLeaveOfAbsenceReason.Location = new Point(169, 575);
            lblLeaveOfAbsenceReason.Margin = new Padding(4, 0, 4, 0);
            lblLeaveOfAbsenceReason.MouseState = MaterialSkin.MouseState.HOVER;
            lblLeaveOfAbsenceReason.Name = "lblLeaveOfAbsenceReason";
            lblLeaveOfAbsenceReason.Size = new Size(187, 19);
            lblLeaveOfAbsenceReason.TabIndex = 17;
            lblLeaveOfAbsenceReason.Text = "Leave of Absence Reason:";
            lblLeaveOfAbsenceReason.Visible = false;
            // 
            // txtLeaveOfAbsenceReason
            // 
            txtLeaveOfAbsenceReason.AnimateReadOnly = false;
            txtLeaveOfAbsenceReason.BackColor = SystemColors.Window;
            txtLeaveOfAbsenceReason.BorderStyle = BorderStyle.None;
            txtLeaveOfAbsenceReason.Depth = 0;
            txtLeaveOfAbsenceReason.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtLeaveOfAbsenceReason.LeadingIcon = null;
            txtLeaveOfAbsenceReason.Location = new Point(171, 597);
            txtLeaveOfAbsenceReason.Margin = new Padding(4, 3, 4, 3);
            txtLeaveOfAbsenceReason.MaxLength = 50;
            txtLeaveOfAbsenceReason.MouseState = MaterialSkin.MouseState.OUT;
            txtLeaveOfAbsenceReason.Multiline = false;
            txtLeaveOfAbsenceReason.Name = "txtLeaveOfAbsenceReason";
            txtLeaveOfAbsenceReason.Size = new Size(167, 50);
            txtLeaveOfAbsenceReason.TabIndex = 50;
            txtLeaveOfAbsenceReason.TabStop = false;
            txtLeaveOfAbsenceReason.Text = "";
            txtLeaveOfAbsenceReason.TrailingIcon = null;
            txtLeaveOfAbsenceReason.Visible = false;
            // 
            // lblMessageType
            // 
            lblMessageType.AutoSize = true;
            lblMessageType.Depth = 0;
            lblMessageType.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblMessageType.Location = new Point(13, 67);
            lblMessageType.Margin = new Padding(4, 0, 4, 0);
            lblMessageType.MouseState = MaterialSkin.MouseState.HOVER;
            lblMessageType.Name = "lblMessageType";
            lblMessageType.Size = new Size(108, 19);
            lblMessageType.TabIndex = 19;
            lblMessageType.Text = "Message Type:";
            // 
            // materialLabel1
            // 
            materialLabel1.AutoSize = true;
            materialLabel1.Depth = 0;
            materialLabel1.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            materialLabel1.Location = new Point(9, 666);
            materialLabel1.Margin = new Padding(4, 0, 4, 0);
            materialLabel1.MouseState = MaterialSkin.MouseState.HOVER;
            materialLabel1.Name = "materialLabel1";
            materialLabel1.Size = new Size(69, 19);
            materialLabel1.TabIndex = 32;
            materialLabel1.Text = "Message:";
            materialLabel1.Visible = false;
            // 
            // CopyButton
            // 
            CopyButton.AutoSize = false;
            CopyButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            CopyButton.Density = MaterialButton.MaterialButtonDensity.Default;
            CopyButton.Depth = 0;
            CopyButton.FlatStyle = FlatStyle.Flat;
            CopyButton.Font = new Font("Segoe UI", 9F);
            CopyButton.HighEmphasis = true;
            CopyButton.Icon = null;
            CopyButton.Location = new Point(109, 838);
            CopyButton.Margin = new Padding(4, 6, 4, 6);
            CopyButton.MouseState = MaterialSkin.MouseState.HOVER;
            CopyButton.Name = "CopyButton";
            CopyButton.NoAccentTextColor = Color.Empty;
            CopyButton.Size = new Size(138, 47);
            CopyButton.TabIndex = 50;
            CopyButton.Text = "Copy";
            CopyButton.Type = MaterialButton.MaterialButtonType.Contained;
            CopyButton.UseAccentColor = false;
            CopyButton.UseVisualStyleBackColor = true;
            CopyButton.Visible = false;
            CopyButton.Click += CopyButton_Click;
            CopyButton.MouseDown += CopyButton_MouseDown;
            CopyButton.MouseEnter += CopyButton_MouseEnter;
            CopyButton.MouseLeave += CopyButton_MouseLeave;
            CopyButton.MouseUp += CopyButton_MouseUp;
            // 
            // ResetButton
            // 
            ResetButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            ResetButton.Density = MaterialButton.MaterialButtonDensity.Default;
            ResetButton.Depth = 0;
            ResetButton.FlatStyle = FlatStyle.Flat;
            ResetButton.HighEmphasis = true;
            ResetButton.Icon = null;
            ResetButton.Location = new Point(439, 25);
            ResetButton.Margin = new Padding(4, 3, 4, 3);
            ResetButton.MouseState = MaterialSkin.MouseState.HOVER;
            ResetButton.Name = "ResetButton";
            ResetButton.NoAccentTextColor = Color.Empty;
            ResetButton.Size = new Size(65, 36);
            ResetButton.TabIndex = 50;
            ResetButton.Text = "Reset";
            ResetButton.Type = MaterialButton.MaterialButtonType.Contained;
            ResetButton.UseAccentColor = true;
            ResetButton.UseVisualStyleBackColor = true;
            ResetButton.Click += ResetButton_Click;
            ResetButton.MouseDown += ResetButton_MouseDown;
            ResetButton.MouseEnter += ResetButton_MouseEnter;
            ResetButton.MouseLeave += ResetButton_MouseLeave;
            ResetButton.MouseUp += ResetButton_MouseUp;
            // 
            // SettingsButton
            // 
            SettingsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            SettingsButton.Density = MaterialButton.MaterialButtonDensity.Default;
            SettingsButton.Depth = 0;
            SettingsButton.HighEmphasis = true;
            SettingsButton.Icon = null;
            SettingsButton.Location = new Point(266, 25);
            SettingsButton.Margin = new Padding(4, 6, 4, 6);
            SettingsButton.MouseState = MaterialSkin.MouseState.HOVER;
            SettingsButton.Name = "SettingsButton";
            SettingsButton.NoAccentTextColor = Color.Empty;
            SettingsButton.Size = new Size(90, 36);
            SettingsButton.TabIndex = 49;
            SettingsButton.Text = "Settings";
            SettingsButton.Type = MaterialButton.MaterialButtonType.Contained;
            SettingsButton.UseAccentColor = false;
            SettingsButton.UseVisualStyleBackColor = true;
            SettingsButton.Click += SettingsButton_Click;
            SettingsButton.MouseDown += SettingsButton_MouseDown;
            SettingsButton.MouseEnter += SettingsButton_MouseEnter;
            SettingsButton.MouseLeave += SettingsButton_MouseLeave;
            SettingsButton.MouseUp += SettingsButton_MouseUp;
            // 
            // UnitLabel
            // 
            UnitLabel.AutoSize = true;
            UnitLabel.Depth = 0;
            UnitLabel.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            UnitLabel.Location = new Point(14, 415);
            UnitLabel.MouseState = MaterialSkin.MouseState.HOVER;
            UnitLabel.Name = "UnitLabel";
            UnitLabel.Size = new Size(99, 19);
            UnitLabel.TabIndex = 15;
            UnitLabel.Text = "Unit (PV1-3.1)";
            // 
            // FloorLabel
            // 
            FloorLabel.AutoSize = true;
            FloorLabel.Depth = 0;
            FloorLabel.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            FloorLabel.Location = new Point(12, 575);
            FloorLabel.MouseState = MaterialSkin.MouseState.HOVER;
            FloorLabel.Name = "FloorLabel";
            FloorLabel.Size = new Size(107, 19);
            FloorLabel.TabIndex = 23;
            FloorLabel.Text = "Floor (PV1-3.8)";
            // 
            // UnitTextBox
            // 
            UnitTextBox.AnimateReadOnly = false;
            UnitTextBox.BorderStyle = BorderStyle.None;
            UnitTextBox.Depth = 0;
            UnitTextBox.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            UnitTextBox.LeadingIcon = null;
            UnitTextBox.Location = new Point(15, 437);
            UnitTextBox.MaxLength = 50;
            UnitTextBox.MouseState = MaterialSkin.MouseState.OUT;
            UnitTextBox.Multiline = false;
            UnitTextBox.Name = "UnitTextBox";
            UnitTextBox.Size = new Size(154, 50);
            UnitTextBox.TabIndex = 16;
            UnitTextBox.Text = "";
            UnitTextBox.TrailingIcon = null;
            // 
            // FloorTextBox
            // 
            FloorTextBox.AnimateReadOnly = false;
            FloorTextBox.BorderStyle = BorderStyle.None;
            FloorTextBox.Depth = 0;
            FloorTextBox.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            FloorTextBox.LeadingIcon = null;
            FloorTextBox.Location = new Point(12, 597);
            FloorTextBox.MaxLength = 50;
            FloorTextBox.MouseState = MaterialSkin.MouseState.OUT;
            FloorTextBox.Multiline = false;
            FloorTextBox.Name = "FloorTextBox";
            FloorTextBox.Size = new Size(152, 50);
            FloorTextBox.TabIndex = 24;
            FloorTextBox.Text = "";
            FloorTextBox.TrailingIcon = null;
            // 
            // txtEVENT
            // 
            txtEVENT.AnimateReadOnly = false;
            txtEVENT.BackColor = SystemColors.Window;
            txtEVENT.BorderStyle = BorderStyle.None;
            txtEVENT.Depth = 0;
            txtEVENT.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txtEVENT.LeadingIcon = null;
            txtEVENT.Location = new Point(468, 89);
            txtEVENT.Margin = new Padding(4, 3, 4, 3);
            txtEVENT.MaxLength = 50;
            txtEVENT.MouseState = MaterialSkin.MouseState.OUT;
            txtEVENT.Multiline = false;
            txtEVENT.Name = "txtEVENT";
            txtEVENT.Size = new Size(208, 50);
            txtEVENT.TabIndex = 1;
            txtEVENT.Text = "";
            txtEVENT.TrailingIcon = null;
            txtEVENT.Visible = false;
            // 
            // lblEVENT
            // 
            lblEVENT.AutoSize = true;
            lblEVENT.Depth = 0;
            lblEVENT.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            lblEVENT.Location = new Point(468, 67);
            lblEVENT.Margin = new Padding(4, 0, 4, 0);
            lblEVENT.MouseState = MaterialSkin.MouseState.HOVER;
            lblEVENT.Name = "lblEVENT";
            lblEVENT.Size = new Size(204, 19);
            lblEVENT.TabIndex = 1;
            lblEVENT.Text = "EVN 6-1 (yyyyMMddHHmm):";
            lblEVENT.Visible = false;
            // 
            // TxtFacility
            // 
            TxtFacility.AnimateReadOnly = false;
            TxtFacility.BorderStyle = BorderStyle.None;
            TxtFacility.Depth = 0;
            TxtFacility.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            TxtFacility.LeadingIcon = null;
            TxtFacility.Location = new Point(173, 518);
            TxtFacility.MaxLength = 50;
            TxtFacility.MouseState = MaterialSkin.MouseState.OUT;
            TxtFacility.Multiline = false;
            TxtFacility.Name = "TxtFacility";
            TxtFacility.Size = new Size(153, 50);
            TxtFacility.TabIndex = 22;
            TxtFacility.Text = "";
            TxtFacility.TrailingIcon = null;
            // 
            // LblFacility
            // 
            LblFacility.AutoSize = true;
            LblFacility.Depth = 0;
            LblFacility.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            LblFacility.Location = new Point(173, 496);
            LblFacility.MouseState = MaterialSkin.MouseState.HOVER;
            LblFacility.Name = "LblFacility";
            LblFacility.Size = new Size(122, 19);
            LblFacility.TabIndex = 21;
            LblFacility.Text = "Facility (PV1-3.4)";
            // 
            // OBXReason1TextBox
            // 
            OBXReason1TextBox.AnimateReadOnly = false;
            OBXReason1TextBox.BackColor = SystemColors.Window;
            OBXReason1TextBox.BorderStyle = BorderStyle.None;
            OBXReason1TextBox.Depth = 0;
            OBXReason1TextBox.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBXReason1TextBox.LeadingIcon = null;
            OBXReason1TextBox.Location = new Point(371, 257);
            OBXReason1TextBox.Margin = new Padding(4, 3, 4, 3);
            OBXReason1TextBox.MaxLength = 50;
            OBXReason1TextBox.MouseState = MaterialSkin.MouseState.OUT;
            OBXReason1TextBox.Multiline = false;
            OBXReason1TextBox.Name = "OBXReason1TextBox";
            OBXReason1TextBox.Size = new Size(304, 50);
            OBXReason1TextBox.TabIndex = 26;
            OBXReason1TextBox.Text = "";
            OBXReason1TextBox.TrailingIcon = null;
            OBXReason1TextBox.Visible = false;
            // 
            // OBXReasonLabel1
            // 
            OBXReasonLabel1.AutoSize = true;
            OBXReasonLabel1.Depth = 0;
            OBXReasonLabel1.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBXReasonLabel1.Location = new Point(371, 235);
            OBXReasonLabel1.Margin = new Padding(4, 0, 4, 0);
            OBXReasonLabel1.MouseState = MaterialSkin.MouseState.HOVER;
            OBXReasonLabel1.Name = "OBXReasonLabel1";
            OBXReasonLabel1.Size = new Size(178, 19);
            OBXReasonLabel1.TabIndex = 25;
            OBXReasonLabel1.Text = "OBX 1 Reason (OBX-5-1):";
            OBXReasonLabel1.Visible = false;
            // 
            // OBXType1TextBox
            // 
            OBXType1TextBox.AnimateReadOnly = false;
            OBXType1TextBox.BackColor = SystemColors.Window;
            OBXType1TextBox.BorderStyle = BorderStyle.None;
            OBXType1TextBox.Depth = 0;
            OBXType1TextBox.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBXType1TextBox.LeadingIcon = null;
            OBXType1TextBox.Location = new Point(372, 176);
            OBXType1TextBox.Margin = new Padding(4, 3, 4, 3);
            OBXType1TextBox.MaxLength = 50;
            OBXType1TextBox.MouseState = MaterialSkin.MouseState.OUT;
            OBXType1TextBox.Multiline = false;
            OBXType1TextBox.Name = "OBXType1TextBox";
            OBXType1TextBox.Size = new Size(303, 50);
            OBXType1TextBox.TabIndex = 28;
            OBXType1TextBox.Text = "";
            OBXType1TextBox.TrailingIcon = null;
            OBXType1TextBox.Visible = false;
            // 
            // OBX1TypeLabel
            // 
            OBX1TypeLabel.AutoSize = true;
            OBX1TypeLabel.Depth = 0;
            OBX1TypeLabel.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBX1TypeLabel.Location = new Point(371, 154);
            OBX1TypeLabel.Margin = new Padding(4, 0, 4, 0);
            OBX1TypeLabel.MouseState = MaterialSkin.MouseState.HOVER;
            OBX1TypeLabel.Name = "OBX1TypeLabel";
            OBX1TypeLabel.Size = new Size(160, 19);
            OBX1TypeLabel.TabIndex = 27;
            OBX1TypeLabel.Text = "OBX 1 Type (OBX-3-1):";
            OBX1TypeLabel.Visible = false;
            // 
            // OBXType2TextBox
            // 
            OBXType2TextBox.AnimateReadOnly = false;
            OBXType2TextBox.BackColor = SystemColors.Window;
            OBXType2TextBox.BorderStyle = BorderStyle.None;
            OBXType2TextBox.Depth = 0;
            OBXType2TextBox.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBXType2TextBox.LeadingIcon = null;
            OBXType2TextBox.Location = new Point(371, 339);
            OBXType2TextBox.Margin = new Padding(4, 3, 4, 3);
            OBXType2TextBox.MaxLength = 50;
            OBXType2TextBox.MouseState = MaterialSkin.MouseState.OUT;
            OBXType2TextBox.Multiline = false;
            OBXType2TextBox.Name = "OBXType2TextBox";
            OBXType2TextBox.Size = new Size(304, 50);
            OBXType2TextBox.TabIndex = 32;
            OBXType2TextBox.Text = "";
            OBXType2TextBox.TrailingIcon = null;
            OBXType2TextBox.Visible = false;
            // 
            // OBX2TypeLabel
            // 
            OBX2TypeLabel.AutoSize = true;
            OBX2TypeLabel.Depth = 0;
            OBX2TypeLabel.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBX2TypeLabel.Location = new Point(371, 317);
            OBX2TypeLabel.Margin = new Padding(4, 0, 4, 0);
            OBX2TypeLabel.MouseState = MaterialSkin.MouseState.HOVER;
            OBX2TypeLabel.Name = "OBX2TypeLabel";
            OBX2TypeLabel.Size = new Size(160, 19);
            OBX2TypeLabel.TabIndex = 31;
            OBX2TypeLabel.Text = "OBX 2 Type (OBX-3-1):";
            OBX2TypeLabel.Visible = false;
            // 
            // OBXReason2TextBox
            // 
            OBXReason2TextBox.AnimateReadOnly = false;
            OBXReason2TextBox.BackColor = SystemColors.Window;
            OBXReason2TextBox.BorderStyle = BorderStyle.None;
            OBXReason2TextBox.Depth = 0;
            OBXReason2TextBox.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBXReason2TextBox.LeadingIcon = null;
            OBXReason2TextBox.Location = new Point(371, 436);
            OBXReason2TextBox.Margin = new Padding(4, 3, 4, 3);
            OBXReason2TextBox.MaxLength = 50;
            OBXReason2TextBox.MouseState = MaterialSkin.MouseState.OUT;
            OBXReason2TextBox.Multiline = false;
            OBXReason2TextBox.Name = "OBXReason2TextBox";
            OBXReason2TextBox.Size = new Size(304, 50);
            OBXReason2TextBox.TabIndex = 30;
            OBXReason2TextBox.Text = "";
            OBXReason2TextBox.TrailingIcon = null;
            OBXReason2TextBox.Visible = false;
            // 
            // OBXReasonLabel2
            // 
            OBXReasonLabel2.AutoSize = true;
            OBXReasonLabel2.Depth = 0;
            OBXReasonLabel2.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBXReasonLabel2.Location = new Point(371, 414);
            OBXReasonLabel2.Margin = new Padding(4, 0, 4, 0);
            OBXReasonLabel2.MouseState = MaterialSkin.MouseState.HOVER;
            OBXReasonLabel2.Name = "OBXReasonLabel2";
            OBXReasonLabel2.Size = new Size(178, 19);
            OBXReasonLabel2.TabIndex = 29;
            OBXReasonLabel2.Text = "OBX 2 Reason (OBX-5-1):";
            OBXReasonLabel2.Visible = false;
            // 
            // OBXType3TextBox
            // 
            OBXType3TextBox.AnimateReadOnly = false;
            OBXType3TextBox.BackColor = SystemColors.Window;
            OBXType3TextBox.BorderStyle = BorderStyle.None;
            OBXType3TextBox.Depth = 0;
            OBXType3TextBox.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBXType3TextBox.LeadingIcon = null;
            OBXType3TextBox.Location = new Point(371, 518);
            OBXType3TextBox.Margin = new Padding(4, 3, 4, 3);
            OBXType3TextBox.MaxLength = 50;
            OBXType3TextBox.MouseState = MaterialSkin.MouseState.OUT;
            OBXType3TextBox.Multiline = false;
            OBXType3TextBox.Name = "OBXType3TextBox";
            OBXType3TextBox.Size = new Size(304, 50);
            OBXType3TextBox.TabIndex = 36;
            OBXType3TextBox.Text = "";
            OBXType3TextBox.TrailingIcon = null;
            OBXType3TextBox.Visible = false;
            // 
            // OBXTypeLabel3
            // 
            OBXTypeLabel3.AutoSize = true;
            OBXTypeLabel3.Depth = 0;
            OBXTypeLabel3.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBXTypeLabel3.Location = new Point(371, 494);
            OBXTypeLabel3.Margin = new Padding(4, 0, 4, 0);
            OBXTypeLabel3.MouseState = MaterialSkin.MouseState.HOVER;
            OBXTypeLabel3.Name = "OBXTypeLabel3";
            OBXTypeLabel3.Size = new Size(160, 19);
            OBXTypeLabel3.TabIndex = 35;
            OBXTypeLabel3.Text = "OBX 3 Type (OBX-3-1):";
            OBXTypeLabel3.Visible = false;
            // 
            // OBXReason3TextBox
            // 
            OBXReason3TextBox.AnimateReadOnly = false;
            OBXReason3TextBox.BackColor = SystemColors.Window;
            OBXReason3TextBox.BorderStyle = BorderStyle.None;
            OBXReason3TextBox.Depth = 0;
            OBXReason3TextBox.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBXReason3TextBox.LeadingIcon = null;
            OBXReason3TextBox.Location = new Point(371, 597);
            OBXReason3TextBox.Margin = new Padding(4, 3, 4, 3);
            OBXReason3TextBox.MaxLength = 50;
            OBXReason3TextBox.MouseState = MaterialSkin.MouseState.OUT;
            OBXReason3TextBox.Multiline = false;
            OBXReason3TextBox.Name = "OBXReason3TextBox";
            OBXReason3TextBox.Size = new Size(304, 50);
            OBXReason3TextBox.TabIndex = 34;
            OBXReason3TextBox.Text = "";
            OBXReason3TextBox.TrailingIcon = null;
            OBXReason3TextBox.Visible = false;
            // 
            // OBXReasonLabel3
            // 
            OBXReasonLabel3.AutoSize = true;
            OBXReasonLabel3.Depth = 0;
            OBXReasonLabel3.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            OBXReasonLabel3.Location = new Point(371, 577);
            OBXReasonLabel3.Margin = new Padding(4, 0, 4, 0);
            OBXReasonLabel3.MouseState = MaterialSkin.MouseState.HOVER;
            OBXReasonLabel3.Name = "OBXReasonLabel3";
            OBXReasonLabel3.Size = new Size(178, 19);
            OBXReasonLabel3.TabIndex = 33;
            OBXReasonLabel3.Text = "OBX 3 Reason (OBX-5-1):";
            OBXReasonLabel3.Visible = false;
            // 
            // panel4
            // 
            panel4.BackColor = Color.Transparent;
            panel4.BorderStyle = BorderStyle.FixedSingle;
            panel4.Location = new Point(9, 403);
            panel4.Name = "panel4";
            panel4.Size = new Size(348, 249);
            panel4.TabIndex = 50;
            // 
            // panel1
            // 
            panel1.BackColor = Color.Transparent;
            panel1.BorderStyle = BorderStyle.FixedSingle;
            panel1.Location = new Point(8, 144);
            panel1.Name = "panel1";
            panel1.Size = new Size(349, 252);
            panel1.TabIndex = 48;
            // 
            // panel2
            // 
            panel2.BackColor = Color.Transparent;
            panel2.BorderStyle = BorderStyle.FixedSingle;
            panel2.Location = new Point(363, 144);
            panel2.Name = "panel2";
            panel2.Size = new Size(322, 508);
            panel2.TabIndex = 51;
            // 
            // LogsButton
            // 
            LogsButton.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            LogsButton.Density = MaterialButton.MaterialButtonDensity.Default;
            LogsButton.Depth = 0;
            LogsButton.HighEmphasis = true;
            LogsButton.Icon = null;
            LogsButton.Location = new Point(364, 25);
            LogsButton.Margin = new Padding(4, 3, 4, 3);
            LogsButton.MouseState = MaterialSkin.MouseState.HOVER;
            LogsButton.Name = "LogsButton";
            LogsButton.NoAccentTextColor = Color.Empty;
            LogsButton.Size = new Size(64, 36);
            LogsButton.TabIndex = 52;
            LogsButton.Text = "Logs";
            LogsButton.Type = MaterialButton.MaterialButtonType.Contained;
            LogsButton.UseAccentColor = false;
            LogsButton.UseVisualStyleBackColor = true;
            LogsButton.Click += LogsButton_Click;
            LogsButton.MouseDown += LogsButton_MouseDown;
            LogsButton.MouseEnter += LogsButton_MouseEnter;
            LogsButton.MouseLeave += LogsButton_MouseLeave;
            LogsButton.MouseUp += LogsButton_MouseUp;
            // 
            // Main
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.ButtonShadow;
            ClientSize = new Size(692, 892);
            Controls.Add(LogsButton);
            Controls.Add(OBXType1TextBox);
            Controls.Add(OBX1TypeLabel);
            Controls.Add(OBXReason1TextBox);
            Controls.Add(OBXReasonLabel1);
            Controls.Add(OBXType3TextBox);
            Controls.Add(OBXTypeLabel3);
            Controls.Add(OBXReason3TextBox);
            Controls.Add(OBXReasonLabel3);
            Controls.Add(OBXType2TextBox);
            Controls.Add(OBX2TypeLabel);
            Controls.Add(OBXReason2TextBox);
            Controls.Add(OBXReasonLabel2);
            Controls.Add(TxtFacility);
            Controls.Add(LblFacility);
            Controls.Add(lblEVENT);
            Controls.Add(txtEVENT);
            Controls.Add(FloorTextBox);
            Controls.Add(UnitTextBox);
            Controls.Add(FloorLabel);
            Controls.Add(UnitLabel);
            Controls.Add(SettingsButton);
            Controls.Add(ResetButton);
            Controls.Add(CopyButton);
            Controls.Add(materialLabel1);
            Controls.Add(lblMessageType);
            Controls.Add(txtLeaveOfAbsenceReason);
            Controls.Add(lblLeaveOfAbsenceReason);
            Controls.Add(txtDischargeReason);
            Controls.Add(lblDischargeReason);
            Controls.Add(txtGeneratedMessage);
            Controls.Add(btnGenerate);
            Controls.Add(btnGenerateAndSend);
            Controls.Add(txtAdmissionNumber);
            Controls.Add(lblAdmissionNumber);
            Controls.Add(txtRoom);
            Controls.Add(lblRoom);
            Controls.Add(lblBed);
            Controls.Add(txtBed);
            Controls.Add(txtSex);
            Controls.Add(lblSex);
            Controls.Add(txtBirthDate);
            Controls.Add(lblBirthDate);
            Controls.Add(txtPatientName);
            Controls.Add(lblPatientName);
            Controls.Add(txtPatGivenName);
            Controls.Add(lblPatGivenName);
            Controls.Add(txtPatientID);
            Controls.Add(lblPatientID);
            Controls.Add(txtNewPatientID);
            Controls.Add(lblNewPatientID);
            Controls.Add(comboMessageType);
            Controls.Add(panel1);
            Controls.Add(panel4);
            Controls.Add(panel2);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Margin = new Padding(4, 3, 4, 3);
            MaximizeBox = false;
            Name = "Main";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "HL7 Message Generator";
            ResumeLayout(false);
            PerformLayout();
        }

        private MaterialLabel lblPatientID;
        private MaterialLabel lblNewPatientID;
        private MaterialLabel lblPatientName;
        private MaterialLabel lblPatGivenName;
        private MaterialLabel lblBirthDate;
        private MaterialLabel lblSex;
        private MaterialLabel lblRoom;
        private MaterialLabel lblBed;
        private MaterialLabel lblAdmissionNumber;
        private RoundedMaterialButton btnGenerate;
        private MaterialLabel lblDischargeReason;
        private MaterialLabel lblLeaveOfAbsenceReason;
        private MaterialLabel lblMessageType;
        private RoundedMaterialComboBox comboMessageType;
        private RoundedMaterialTextBox txtPatientID;
        private RoundedMaterialTextBox txtNewPatientID;
        private RoundedMaterialTextBox txtPatientName;
        private RoundedMaterialTextBox txtPatGivenName;
        private RoundedMaterialTextBox txtBirthDate;
        private RoundedMaterialTextBox txtSex;
        private RoundedMaterialTextBox txtRoom;
        private RoundedMaterialTextBox txtBed;
        private RoundedMaterialTextBox txtAdmissionNumber;
        private RoundedMaterialButton btnGenerateAndSend;
        private RoundedMaterialMultiLineTextBox2 txtGeneratedMessage;
        private RoundedMaterialTextBox txtDischargeReason;
        private RoundedMaterialTextBox txtLeaveOfAbsenceReason;
        private MaterialLabel materialLabel1;
        private RoundedMaterialButton CopyButton;
        private RoundedMaterialButton ResetButton;
        private RoundedMaterialButton SettingsButton;
        private MaterialLabel UnitLabel;
        private MaterialLabel FloorLabel;
        private RoundedMaterialTextBox UnitTextBox;
        private RoundedMaterialTextBox FloorTextBox;
        private RoundedMaterialTextBox txtEVENT;
        private MaterialLabel lblEVENT;
        private RoundedMaterialTextBox TxtFacility;
        private MaterialLabel LblFacility;
        private RoundedMaterialTextBox OBXReason1TextBox;
        private MaterialLabel OBXReasonLabel1;
        private RoundedMaterialTextBox OBXType1TextBox;
        private MaterialLabel OBX1TypeLabel;
        private RoundedMaterialTextBox OBXType2TextBox;
        private MaterialLabel OBX2TypeLabel;
        private RoundedMaterialTextBox OBXReason2TextBox;
        private MaterialLabel OBXReasonLabel2;
        private RoundedMaterialTextBox OBXType3TextBox;
        private MaterialLabel OBXTypeLabel3;
        private RoundedMaterialTextBox OBXReason3TextBox;
        private MaterialLabel OBXReasonLabel3;
        private Panel panel1;
        private Panel panel3;
        private Panel panel4;
        private Panel panel2;
        private RoundedMaterialButton LogsButton;
    }
}
