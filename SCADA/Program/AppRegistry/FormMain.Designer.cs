namespace AppRegistry
{
    partial class FormMain
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
            this.button1 = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.FBD1 = new System.Windows.Forms.FolderBrowserDialog();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.txtDataConfig = new System.Windows.Forms.TextBox();
            this.btnDataConfig = new System.Windows.Forms.Button();
            this.btnDatabase = new System.Windows.Forms.Button();
            this.txtDatabase = new System.Windows.Forms.TextBox();
            this.ComDatabase = new System.Windows.Forms.ComboBox();
            this.SuspendLayout();
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(356, 135);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 0;
            this.button1.Text = "OK";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(275, 135);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 1;
            this.button2.Text = "button2";
            this.button2.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 29);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(65, 13);
            this.label1.TabIndex = 2;
            this.label1.Text = "DataConfig:";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(14, 64);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(57, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Database:";
            // 
            // txtDataConfig
            // 
            this.txtDataConfig.Location = new System.Drawing.Point(77, 26);
            this.txtDataConfig.Name = "txtDataConfig";
            this.txtDataConfig.Size = new System.Drawing.Size(285, 20);
            this.txtDataConfig.TabIndex = 4;
            // 
            // btnDataConfig
            // 
            this.btnDataConfig.Location = new System.Drawing.Point(372, 25);
            this.btnDataConfig.Name = "btnDataConfig";
            this.btnDataConfig.Size = new System.Drawing.Size(33, 23);
            this.btnDataConfig.TabIndex = 5;
            this.btnDataConfig.Text = "...";
            this.btnDataConfig.UseVisualStyleBackColor = true;
            this.btnDataConfig.Click += new System.EventHandler(this.btnDataConfig_Click);
            // 
            // btnDatabase
            // 
            this.btnDatabase.Location = new System.Drawing.Point(372, 61);
            this.btnDatabase.Name = "btnDatabase";
            this.btnDatabase.Size = new System.Drawing.Size(33, 23);
            this.btnDatabase.TabIndex = 7;
            this.btnDatabase.Text = "...";
            this.btnDatabase.UseVisualStyleBackColor = true;
            this.btnDatabase.Click += new System.EventHandler(this.btnDatabase_Click);
            // 
            // txtDatabase
            // 
            this.txtDatabase.Location = new System.Drawing.Point(77, 62);
            this.txtDatabase.Name = "txtDatabase";
            this.txtDatabase.Size = new System.Drawing.Size(285, 20);
            this.txtDatabase.TabIndex = 6;
            // 
            // ComDatabase
            // 
            this.ComDatabase.FormattingEnabled = true;
            this.ComDatabase.Location = new System.Drawing.Point(77, 88);
            this.ComDatabase.Name = "ComDatabase";
            this.ComDatabase.Size = new System.Drawing.Size(285, 21);
            this.ComDatabase.TabIndex = 8;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(443, 170);
            this.Controls.Add(this.ComDatabase);
            this.Controls.Add(this.btnDatabase);
            this.Controls.Add(this.txtDatabase);
            this.Controls.Add(this.btnDataConfig);
            this.Controls.Add(this.txtDataConfig);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.button1);
            this.Name = "FormMain";
            this.Text = "Form1";
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.FolderBrowserDialog FBD1;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox txtDataConfig;
        private System.Windows.Forms.Button btnDataConfig;
        private System.Windows.Forms.Button btnDatabase;
        private System.Windows.Forms.TextBox txtDatabase;
        private System.Windows.Forms.ComboBox ComDatabase;
    }
}

