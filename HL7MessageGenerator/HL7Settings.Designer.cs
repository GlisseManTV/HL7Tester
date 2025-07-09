using MaterialSkin.Controls;
using static HL7MessageGenerator.Main;

partial class HL7Settings
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
        System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HL7Settings));
        txtIpAddress = new RoundedMaterialTextBox();
        txtPort = new RoundedMaterialTextBox();
        btnSend = new RoundedMaterialButton();
        btnCancel = new RoundedMaterialButton();
        IPLabel = new MaterialLabel();
        PortLabel = new MaterialLabel();
        CheckUpdateBox = new CheckBox();
        SuspendLayout();
        // 
        // txtIpAddress
        // 
        txtIpAddress.AnimateReadOnly = false;
        txtIpAddress.BackColor = SystemColors.Window;
        txtIpAddress.BorderStyle = BorderStyle.None;
        txtIpAddress.Depth = 0;
        txtIpAddress.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
        txtIpAddress.LeadingIcon = null;
        txtIpAddress.Location = new Point(7, 96);
        txtIpAddress.Margin = new Padding(4, 3, 4, 3);
        txtIpAddress.MaxLength = 50;
        txtIpAddress.MouseState = MaterialSkin.MouseState.OUT;
        txtIpAddress.Multiline = false;
        txtIpAddress.Name = "txtIpAddress";
        txtIpAddress.Size = new Size(174, 50);
        txtIpAddress.TabIndex = 0;
        txtIpAddress.Text = "";
        txtIpAddress.TrailingIcon = null;
        // 
        // txtPort
        // 
        txtPort.AnimateReadOnly = false;
        txtPort.BorderStyle = BorderStyle.None;
        txtPort.Depth = 0;
        txtPort.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
        txtPort.LeadingIcon = null;
        txtPort.Location = new Point(239, 96);
        txtPort.Margin = new Padding(4, 3, 4, 3);
        txtPort.MaxLength = 50;
        txtPort.MouseState = MaterialSkin.MouseState.OUT;
        txtPort.Multiline = false;
        txtPort.Name = "txtPort";
        txtPort.Size = new Size(96, 50);
        txtPort.TabIndex = 1;
        txtPort.Text = "";
        txtPort.TrailingIcon = null;
        // 
        // btnSend
        // 
        btnSend.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnSend.Density = MaterialButton.MaterialButtonDensity.Default;
        btnSend.Depth = 0;
        btnSend.HighEmphasis = true;
        btnSend.Icon = null;
        btnSend.Location = new Point(275, 210);
        btnSend.Margin = new Padding(4, 3, 4, 3);
        btnSend.MouseState = MaterialSkin.MouseState.HOVER;
        btnSend.Name = "btnSend";
        btnSend.NoAccentTextColor = Color.Empty;
        btnSend.Size = new Size(64, 36);
        btnSend.TabIndex = 2;
        btnSend.Text = "Save";
        btnSend.Type = MaterialButton.MaterialButtonType.Contained;
        btnSend.UseAccentColor = false;
        btnSend.UseVisualStyleBackColor = true;
        btnSend.Click += btnSend_Click;
        btnSend.MouseDown += btnSend_MouseDown;
        btnSend.MouseEnter += btnSend_MouseEnter;
        btnSend.MouseLeave += btnSend_MouseLeave;
        btnSend.MouseUp += btnSend_MouseUp;
        // 
        // btnCancel
        // 
        btnCancel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
        btnCancel.Density = MaterialButton.MaterialButtonDensity.Default;
        btnCancel.Depth = 0;
        btnCancel.HighEmphasis = true;
        btnCancel.Icon = null;
        btnCancel.Location = new Point(10, 210);
        btnCancel.Margin = new Padding(4, 3, 4, 3);
        btnCancel.MouseState = MaterialSkin.MouseState.HOVER;
        btnCancel.Name = "btnCancel";
        btnCancel.NoAccentTextColor = Color.Empty;
        btnCancel.Size = new Size(77, 36);
        btnCancel.TabIndex = 3;
        btnCancel.Text = "Cancel";
        btnCancel.Type = MaterialButton.MaterialButtonType.Contained;
        btnCancel.UseAccentColor = false;
        btnCancel.UseVisualStyleBackColor = true;
        btnCancel.Click += btnCancel_Click;
        btnCancel.MouseDown += btnCancel_MouseDown;
        btnCancel.MouseEnter += btnCancel_MouseEnter;
        btnCancel.MouseLeave += btnCancel_MouseLeave;
        btnCancel.MouseUp += btnCancel_MouseUp;
        // 
        // IPLabel
        // 
        IPLabel.AutoSize = true;
        IPLabel.Depth = 0;
        IPLabel.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
        IPLabel.Location = new Point(8, 73);
        IPLabel.Margin = new Padding(4, 0, 4, 0);
        IPLabel.MouseState = MaterialSkin.MouseState.HOVER;
        IPLabel.Name = "IPLabel";
        IPLabel.Size = new Size(75, 19);
        IPLabel.TabIndex = 4;
        IPLabel.Text = "IP address";
        // 
        // PortLabel
        // 
        PortLabel.AutoSize = true;
        PortLabel.Depth = 0;
        PortLabel.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
        PortLabel.Location = new Point(239, 73);
        PortLabel.Margin = new Padding(4, 0, 4, 0);
        PortLabel.MouseState = MaterialSkin.MouseState.HOVER;
        PortLabel.Name = "PortLabel";
        PortLabel.Size = new Size(30, 19);
        PortLabel.TabIndex = 5;
        PortLabel.Text = "Port";
        // 
        // CheckUpdateBox
        // 
        CheckUpdateBox.AutoSize = true;
        CheckUpdateBox.Location = new Point(10, 168);
        CheckUpdateBox.Name = "CheckUpdateBox";
        CheckUpdateBox.Size = new Size(100, 19);
        CheckUpdateBox.TabIndex = 7;
        CheckUpdateBox.Text = "Update Check";
        CheckUpdateBox.UseVisualStyleBackColor = true;
        CheckUpdateBox.CheckedChanged += CheckUpdateBox_CheckedChanged;
        // 
        // HL7Settings
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(347, 252);
        Controls.Add(CheckUpdateBox);
        Controls.Add(PortLabel);
        Controls.Add(IPLabel);
        Controls.Add(btnCancel);
        Controls.Add(btnSend);
        Controls.Add(txtPort);
        Controls.Add(txtIpAddress);
        Icon = (Icon)resources.GetObject("$this.Icon");
        Margin = new Padding(4, 3, 4, 3);
        MaximizeBox = false;
        Name = "HL7Settings";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "HL7 Listener settings";
        ResumeLayout(false);
        PerformLayout();
    }

    private RoundedMaterialTextBox txtIpAddress;
    private RoundedMaterialTextBox txtPort;
    private RoundedMaterialButton btnSend;
    private RoundedMaterialButton btnCancel;
    private MaterialLabel IPLabel;
    private MaterialLabel PortLabel;
    private CheckBox CheckUpdateBox;
}