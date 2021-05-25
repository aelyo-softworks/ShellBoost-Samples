namespace ShellBoost.Samples.RegistryFolder.UI
{
    partial class EditValueDWord
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.TextBoxValueData = new System.Windows.Forms.TextBox();
            this.ValueData = new System.Windows.Forms.Label();
            this.GroupBoxType = new System.Windows.Forms.GroupBox();
            this.RadioButtonDecimal = new System.Windows.Forms.RadioButton();
            this.RadioButtonHexadecimal = new System.Windows.Forms.RadioButton();
            this.GroupBoxType.SuspendLayout();
            this.SuspendLayout();
            // 
            // TextBoxValueData
            // 
            this.TextBoxValueData.Location = new System.Drawing.Point(9, 17);
            this.TextBoxValueData.Margin = new System.Windows.Forms.Padding(3, 3, 0, 3);
            this.TextBoxValueData.Name = "TextBoxValueData";
            this.TextBoxValueData.Size = new System.Drawing.Size(130, 20);
            this.TextBoxValueData.TabIndex = 1;
            // 
            // ValueData
            // 
            this.ValueData.AutoSize = true;
            this.ValueData.Location = new System.Drawing.Point(6, 0);
            this.ValueData.Name = "ValueData";
            this.ValueData.Size = new System.Drawing.Size(61, 13);
            this.ValueData.TabIndex = 0;
            this.ValueData.Text = "&Value data:";
            // 
            // GroupBoxType
            // 
            this.GroupBoxType.Controls.Add(this.RadioButtonDecimal);
            this.GroupBoxType.Controls.Add(this.RadioButtonHexadecimal);
            this.GroupBoxType.Location = new System.Drawing.Point(154, 0);
            this.GroupBoxType.Name = "GroupBoxType";
            this.GroupBoxType.Size = new System.Drawing.Size(159, 68);
            this.GroupBoxType.TabIndex = 2;
            this.GroupBoxType.TabStop = false;
            this.GroupBoxType.Text = "Base";
            // 
            // RadioButtonDecimal
            // 
            this.RadioButtonDecimal.AutoSize = true;
            this.RadioButtonDecimal.Location = new System.Drawing.Point(19, 42);
            this.RadioButtonDecimal.Name = "RadioButtonDecimal";
            this.RadioButtonDecimal.Size = new System.Drawing.Size(63, 17);
            this.RadioButtonDecimal.TabIndex = 1;
            this.RadioButtonDecimal.Text = "&Decimal";
            this.RadioButtonDecimal.UseVisualStyleBackColor = true;
            // 
            // RadioButtonHexadecimal
            // 
            this.RadioButtonHexadecimal.AutoSize = true;
            this.RadioButtonHexadecimal.Checked = true;
            this.RadioButtonHexadecimal.Location = new System.Drawing.Point(19, 19);
            this.RadioButtonHexadecimal.Name = "RadioButtonHexadecimal";
            this.RadioButtonHexadecimal.Size = new System.Drawing.Size(86, 17);
            this.RadioButtonHexadecimal.TabIndex = 0;
            this.RadioButtonHexadecimal.TabStop = true;
            this.RadioButtonHexadecimal.Text = "&Hexadecimal";
            this.RadioButtonHexadecimal.UseVisualStyleBackColor = true;
            this.RadioButtonHexadecimal.CheckedChanged += new System.EventHandler(this.RadioButtonHexadecimal_CheckedChanged);
            // 
            // EditValueDWord
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.GroupBoxType);
            this.Controls.Add(this.TextBoxValueData);
            this.Controls.Add(this.ValueData);
            this.Name = "EditValueDWord";
            this.Size = new System.Drawing.Size(316, 71);
            this.GroupBoxType.ResumeLayout(false);
            this.GroupBoxType.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox TextBoxValueData;
        private System.Windows.Forms.Label ValueData;
        private System.Windows.Forms.GroupBox GroupBoxType;
        private System.Windows.Forms.RadioButton RadioButtonDecimal;
        private System.Windows.Forms.RadioButton RadioButtonHexadecimal;
    }
}
