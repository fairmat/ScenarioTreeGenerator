namespace ScenarioTreeGenerator.UI
{
    partial class SGSettings
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBoxStages = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxScenarios = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.textBoxOutputFolder = new System.Windows.Forms.TextBox();
            this.buttonChoseFolder = new System.Windows.Forms.Button();
            this.buttonStart = new System.Windows.Forms.Button();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxFileName = new System.Windows.Forms.TextBox();
            this.folderBrowserDialog1 = new System.Windows.Forms.FolderBrowserDialog();
            this.SuspendLayout();
            //
            // textBoxStages
            //
            this.textBoxStages.Location = new System.Drawing.Point(135, 16);
            this.textBoxStages.Name = "textBoxStages";
            this.textBoxStages.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.textBoxStages.Size = new System.Drawing.Size(102, 20);
            this.textBoxStages.TabIndex = 0;
            this.textBoxStages.Text = "10";
            //
            // label1
            //
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(13, 19);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Number of stages";
            //
            // label2
            //
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(13, 46);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Number of scenarios";
            //
            // textBoxScenarios
            //
            this.textBoxScenarios.Location = new System.Drawing.Point(135, 43);
            this.textBoxScenarios.Name = "textBoxScenarios";
            this.textBoxScenarios.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.textBoxScenarios.Size = new System.Drawing.Size(102, 20);
            this.textBoxScenarios.TabIndex = 2;
            this.textBoxScenarios.Text = "100";
            //
            // label3
            //
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(13, 73);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(68, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "Output folder";
            //
            // textBoxOutputFolder
            //
            this.textBoxOutputFolder.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxOutputFolder.Location = new System.Drawing.Point(135, 70);
            this.textBoxOutputFolder.Name = "textBoxOutputFolder";
            this.textBoxOutputFolder.Size = new System.Drawing.Size(363, 20);
            this.textBoxOutputFolder.TabIndex = 4;
            this.textBoxOutputFolder.Text = "C:\\tmp";
            //
            // buttonChoseFolder
            //
            this.buttonChoseFolder.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonChoseFolder.Location = new System.Drawing.Point(504, 70);
            this.buttonChoseFolder.Name = "buttonChoseFolder";
            this.buttonChoseFolder.Size = new System.Drawing.Size(35, 20);
            this.buttonChoseFolder.TabIndex = 6;
            this.buttonChoseFolder.Text = "..";
            this.buttonChoseFolder.UseVisualStyleBackColor = true;
            this.buttonChoseFolder.Click += new System.EventHandler(this.buttonChoseFolder_Click);
            //
            // buttonStart
            //
            this.buttonStart.Location = new System.Drawing.Point(16, 143);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(75, 23);
            this.buttonStart.TabIndex = 7;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.buttonStart_Click);
            //
            // label4
            //
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(12, 97);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(81, 13);
            this.label4.TabIndex = 9;
            this.label4.Text = "Output filename";
            //
            // textBoxFileName
            //
            this.textBoxFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxFileName.Location = new System.Drawing.Point(135, 94);
            this.textBoxFileName.Name = "textBoxFileName";
            this.textBoxFileName.ReadOnly = true;
            this.textBoxFileName.Size = new System.Drawing.Size(404, 20);
            this.textBoxFileName.TabIndex = 8;
            //
            // SGSettings
            //
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(551, 178);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.textBoxFileName);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.buttonChoseFolder);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxOutputFolder);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxScenarios);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxStages);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SGSettings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Scenario Tree Generation";
            this.Load += new System.EventHandler(this.SGSettings_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxStages;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox textBoxScenarios;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TextBox textBoxOutputFolder;
        private System.Windows.Forms.Button buttonChoseFolder;
        private System.Windows.Forms.Button buttonStart;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox textBoxFileName;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialog1;
    }
}
