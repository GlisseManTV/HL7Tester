using System.Windows.Forms;
using static HL7Tester.Main;
using HL7Tester;
using MaterialSkin.Controls;
using MaterialSkin;


namespace HL7Tester
{
    partial class history
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
            lastUsedIpsMultiBox = new RoundedMaterialMultiLineTextBox2();
            SuspendLayout();
            // 
            // lastUsedIpsMultiBox
            // 
            lastUsedIpsMultiBox.AnimateReadOnly = false;
            lastUsedIpsMultiBox.BackgroundImageLayout = ImageLayout.None;
            lastUsedIpsMultiBox.CharacterCasing = CharacterCasing.Normal;
            lastUsedIpsMultiBox.Depth = 0;
            lastUsedIpsMultiBox.HideSelection = true;
            lastUsedIpsMultiBox.Location = new Point(6, 69);
            lastUsedIpsMultiBox.MaxLength = 32767;
            lastUsedIpsMultiBox.MouseState = MouseState.OUT;
            lastUsedIpsMultiBox.Name = "lastUsedIpsMultiBox";
            lastUsedIpsMultiBox.PasswordChar = '\0';
            lastUsedIpsMultiBox.ReadOnly = false;
            lastUsedIpsMultiBox.ScrollBars = ScrollBars.None;
            lastUsedIpsMultiBox.SelectedText = "";
            lastUsedIpsMultiBox.SelectionLength = 0;
            lastUsedIpsMultiBox.SelectionStart = 0;
            lastUsedIpsMultiBox.ShortcutsEnabled = true;
            lastUsedIpsMultiBox.Size = new Size(250, 377);
            lastUsedIpsMultiBox.TabIndex = 0;
            lastUsedIpsMultiBox.TabStop = false;
            lastUsedIpsMultiBox.TextAlign = HorizontalAlignment.Left;
            lastUsedIpsMultiBox.UseSystemPasswordChar = false;
            // 
            // history
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(261, 450);
            Controls.Add(lastUsedIpsMultiBox);
            Name = "history";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Last Used IP's";
            ResumeLayout(false);
        }



        private RoundedMaterialMultiLineTextBox2 lastUsedIpsMultiBox;
    }
}