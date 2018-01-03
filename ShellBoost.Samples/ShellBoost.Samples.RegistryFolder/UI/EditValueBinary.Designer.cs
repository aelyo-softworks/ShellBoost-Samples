namespace ShellBoost.Samples.RegistryFolder.UI
{
    partial class EditValueBinary
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
            this.SuspendLayout();
            // 
            // ValueData
            // 
            this.ValueData.AutoSize = true;
            this.ValueData.Location = new System.Drawing.Point(6, 0);
            this.ValueData.Name = "ValueData";
            this.ValueData.Size = new System.Drawing.Size(61, 13);
            this.ValueData.TabIndex = 1;
            this.ValueData.Text = "&Value data:";
            // 
            // EditValueBinary
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.AutoSize = true;
            this.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.Controls.Add(this.ValueData);
            this.Name = "EditValueBinary";
            this.Size = new System.Drawing.Size(70, 13);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label ValueData;
    }
}
