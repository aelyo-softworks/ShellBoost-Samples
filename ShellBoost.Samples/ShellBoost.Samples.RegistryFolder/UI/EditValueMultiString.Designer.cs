namespace ShellBoost.Samples.RegistryFolder.UI
{
    partial class EditValueMultiString
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
            this.ValueData = new System.Windows.Forms.Label();
            this.TextBoxList = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // ValueData
            // 
            this.ValueData.AutoSize = true;
            this.ValueData.Location = new System.Drawing.Point(6, 0);
            this.ValueData.Name = "ValueData";
            this.ValueData.Size = new System.Drawing.Size(61, 13);
            this.ValueData.TabIndex = 2;
            this.ValueData.Text = "&Value data:";
            // 
            // TextBoxList
            // 
            this.TextBoxList.AcceptsReturn = true;
            this.TextBoxList.Location = new System.Drawing.Point(9, 16);
            this.TextBoxList.Multiline = true;
            this.TextBoxList.Name = "TextBoxList";
            this.TextBoxList.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.TextBoxList.Size = new System.Drawing.Size(336, 230);
            this.TextBoxList.TabIndex = 3;
            // 
            // EditValueMultiString
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.TextBoxList);
            this.Controls.Add(this.ValueData);
            this.Name = "EditValueMultiString";
            this.Size = new System.Drawing.Size(348, 249);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label ValueData;
        private System.Windows.Forms.TextBox TextBoxList;
    }
}
