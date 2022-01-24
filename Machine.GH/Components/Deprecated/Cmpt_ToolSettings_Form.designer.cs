namespace tas.Machine.GH
{
    partial class tasTP_ToolSettings_Form
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
            this.button_ok = new System.Windows.Forms.Button();
            this.button_cancel = new System.Windows.Forms.Button();
            this.label_tooldiameter = new System.Windows.Forms.Label();
            this.num_tooldiameter = new System.Windows.Forms.NumericUpDown();
            this.num_stepdown = new System.Windows.Forms.NumericUpDown();
            this.label_stepdown = new System.Windows.Forms.Label();
            this.num_stepover = new System.Windows.Forms.NumericUpDown();
            this.label_stepover = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.num_tooldiameter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_stepdown)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_stepover)).BeginInit();
            this.SuspendLayout();
            // 
            // button_ok
            // 
            this.button_ok.Location = new System.Drawing.Point(190, 226);
            this.button_ok.Name = "button_ok";
            this.button_ok.Size = new System.Drawing.Size(90, 30);
            this.button_ok.TabIndex = 0;
            this.button_ok.Text = "OK";
            this.button_ok.UseVisualStyleBackColor = true;
            this.button_ok.Click += new System.EventHandler(this.button_ok_Click);
            // 
            // button_cancel
            // 
            this.button_cancel.Location = new System.Drawing.Point(94, 226);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(90, 30);
            this.button_cancel.TabIndex = 1;
            this.button_cancel.Text = "Cancel";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // label_tooldiameter
            // 
            this.label_tooldiameter.AutoSize = true;
            this.label_tooldiameter.Location = new System.Drawing.Point(13, 13);
            this.label_tooldiameter.Name = "label_tooldiameter";
            this.label_tooldiameter.Size = new System.Drawing.Size(95, 17);
            this.label_tooldiameter.TabIndex = 2;
            this.label_tooldiameter.Text = "Tool diameter";
            // 
            // num_tooldiameter
            // 
            this.num_tooldiameter.DecimalPlaces = 2;
            this.num_tooldiameter.Location = new System.Drawing.Point(160, 8);
            this.num_tooldiameter.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.num_tooldiameter.Name = "num_tooldiameter";
            this.num_tooldiameter.Size = new System.Drawing.Size(120, 22);
            this.num_tooldiameter.TabIndex = 3;
            this.num_tooldiameter.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // num_stepdown
            // 
            this.num_stepdown.DecimalPlaces = 2;
            this.num_stepdown.Location = new System.Drawing.Point(160, 36);
            this.num_stepdown.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.num_stepdown.Name = "num_stepdown";
            this.num_stepdown.Size = new System.Drawing.Size(120, 22);
            this.num_stepdown.TabIndex = 5;
            this.num_stepdown.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            // 
            // label_stepdown
            // 
            this.label_stepdown.AutoSize = true;
            this.label_stepdown.Location = new System.Drawing.Point(13, 41);
            this.label_stepdown.Name = "label_stepdown";
            this.label_stepdown.Size = new System.Drawing.Size(70, 17);
            this.label_stepdown.TabIndex = 4;
            this.label_stepdown.Text = "Stepdown";
            // 
            // num_stepover
            // 
            this.num_stepover.DecimalPlaces = 2;
            this.num_stepover.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.num_stepover.Location = new System.Drawing.Point(160, 64);
            this.num_stepover.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.num_stepover.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            65536});
            this.num_stepover.Name = "num_stepover";
            this.num_stepover.Size = new System.Drawing.Size(120, 22);
            this.num_stepover.TabIndex = 7;
            this.num_stepover.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label_stepover
            // 
            this.label_stepover.AutoSize = true;
            this.label_stepover.Location = new System.Drawing.Point(13, 69);
            this.label_stepover.Name = "label_stepover";
            this.label_stepover.Size = new System.Drawing.Size(65, 17);
            this.label_stepover.TabIndex = 6;
            this.label_stepover.Text = "Stepover";
            // 
            // tasTP_ToolSettings_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 268);
            this.Controls.Add(this.num_stepover);
            this.Controls.Add(this.label_stepover);
            this.Controls.Add(this.num_stepdown);
            this.Controls.Add(this.label_stepdown);
            this.Controls.Add(this.num_tooldiameter);
            this.Controls.Add(this.label_tooldiameter);
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.button_ok);
            this.Name = "tasTP_ToolSettings_Form";
            this.Text = "Toolpath settings";
            this.Load += new System.EventHandler(this.tasTP_Processor2Settings_Form_Load);
            ((System.ComponentModel.ISupportInitialize)(this.num_tooldiameter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_stepdown)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_stepover)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_ok;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.Label label_tooldiameter;
        private System.Windows.Forms.NumericUpDown num_tooldiameter;
        private System.Windows.Forms.NumericUpDown num_stepdown;
        private System.Windows.Forms.Label label_stepdown;
        private System.Windows.Forms.NumericUpDown num_stepover;
        private System.Windows.Forms.Label label_stepover;
    }
}