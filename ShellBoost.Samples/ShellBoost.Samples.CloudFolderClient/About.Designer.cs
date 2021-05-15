
namespace ShellBoost.Samples.CloudFolderClient
{
    partial class About
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutPanelAbout = new System.Windows.Forms.TableLayoutPanel();
            this.pictureBoxLogo = new System.Windows.Forms.PictureBox();
            this.labelText = new System.Windows.Forms.Label();
            this.buttonClose = new System.Windows.Forms.Button();
            this.tableLayoutPanelAbout.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanelAbout
            // 
            this.tableLayoutPanelAbout.ColumnCount = 2;
            this.tableLayoutPanelAbout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.tableLayoutPanelAbout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle());
            this.tableLayoutPanelAbout.Controls.Add(this.pictureBoxLogo, 0, 0);
            this.tableLayoutPanelAbout.Controls.Add(this.labelText, 1, 0);
            this.tableLayoutPanelAbout.Controls.Add(this.buttonClose, 1, 1);
            this.tableLayoutPanelAbout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanelAbout.Location = new System.Drawing.Point(10, 10);
            this.tableLayoutPanelAbout.Name = "tableLayoutPanelAbout";
            this.tableLayoutPanelAbout.RowCount = 2;
            this.tableLayoutPanelAbout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 48F));
            this.tableLayoutPanelAbout.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanelAbout.Size = new System.Drawing.Size(383, 82);
            this.tableLayoutPanelAbout.TabIndex = 0;
            // 
            // pictureBoxLogo
            // 
            this.pictureBoxLogo.Location = new System.Drawing.Point(3, 3);
            this.pictureBoxLogo.Name = "pictureBoxLogo";
            this.pictureBoxLogo.Size = new System.Drawing.Size(42, 42);
            this.pictureBoxLogo.TabIndex = 1;
            this.pictureBoxLogo.TabStop = false;
            // 
            // labelText
            // 
            this.labelText.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.labelText.AutoSize = true;
            this.labelText.Location = new System.Drawing.Point(51, 16);
            this.labelText.Name = "labelText";
            this.labelText.Size = new System.Drawing.Size(38, 15);
            this.labelText.TabIndex = 2;
            this.labelText.Text = "label1";
            // 
            // buttonClose
            // 
            this.buttonClose.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.buttonClose.Dock = System.Windows.Forms.DockStyle.Right;
            this.buttonClose.Location = new System.Drawing.Point(305, 51);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(75, 28);
            this.buttonClose.TabIndex = 3;
            this.buttonClose.Text = "OK";
            this.buttonClose.UseVisualStyleBackColor = true;
            // 
            // About
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(403, 102);
            this.Controls.Add(this.tableLayoutPanelAbout);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "About";
            this.Padding = new System.Windows.Forms.Padding(10);
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "About CloudFolderClient";
            this.tableLayoutPanelAbout.ResumeLayout(false);
            this.tableLayoutPanelAbout.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxLogo)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutPanelAbout;
        private System.Windows.Forms.PictureBox pictureBoxLogo;
        private System.Windows.Forms.Label labelText;
        private System.Windows.Forms.Button buttonClose;
    }
}
