namespace tas.Machine.GH.Extended
{
    partial class ABBPostComponent_Form
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
            this.label_safez = new System.Windows.Forms.Label();
            this.num_safez = new System.Windows.Forms.NumericUpDown();
            this.num_rapidz = new System.Windows.Forms.NumericUpDown();
            this.label_rapidz = new System.Windows.Forms.Label();
            this.num_feedrate = new System.Windows.Forms.NumericUpDown();
            this.label_feedrate = new System.Windows.Forms.Label();
            this.num_rapidrate = new System.Windows.Forms.NumericUpDown();
            this.label_rapidrate = new System.Windows.Forms.Label();
            this.ext_axis_checkbox = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.num_safez)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_rapidz)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_feedrate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_rapidrate)).BeginInit();
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
            // label_safez
            // 
            this.label_safez.AutoSize = true;
            this.label_safez.Location = new System.Drawing.Point(13, 13);
            this.label_safez.Name = "label_safez";
            this.label_safez.Size = new System.Drawing.Size(50, 17);
            this.label_safez.TabIndex = 2;
            this.label_safez.Text = "Safe Z";
            // 
            // num_safez
            // 
            this.num_safez.DecimalPlaces = 2;
            this.num_safez.Location = new System.Drawing.Point(160, 8);
            this.num_safez.Name = "num_safez";
            this.num_safez.Size = new System.Drawing.Size(120, 22);
            this.num_safez.TabIndex = 3;
            this.num_safez.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // num_rapidz
            // 
            this.num_rapidz.DecimalPlaces = 2;
            this.num_rapidz.Location = new System.Drawing.Point(160, 36);
            this.num_rapidz.Name = "num_rapidz";
            this.num_rapidz.Size = new System.Drawing.Size(120, 22);
            this.num_rapidz.TabIndex = 5;
            this.num_rapidz.Value = new decimal(new int[] {
            20,
            0,
            0,
            0});
            this.num_rapidz.ValueChanged += new System.EventHandler(this.num_rapidz_ValueChanged);
            // 
            // label_rapidz
            // 
            this.label_rapidz.AutoSize = true;
            this.label_rapidz.Location = new System.Drawing.Point(13, 41);
            this.label_rapidz.Name = "label_rapidz";
            this.label_rapidz.Size = new System.Drawing.Size(58, 17);
            this.label_rapidz.TabIndex = 4;
            this.label_rapidz.Text = "Rapid Z";
            // 
            // num_feedrate
            // 
            this.num_feedrate.DecimalPlaces = 2;
            this.num_feedrate.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.num_feedrate.Location = new System.Drawing.Point(160, 64);
            this.num_feedrate.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.num_feedrate.Name = "num_feedrate";
            this.num_feedrate.Size = new System.Drawing.Size(120, 22);
            this.num_feedrate.TabIndex = 7;
            this.num_feedrate.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label_feedrate
            // 
            this.label_feedrate.AutoSize = true;
            this.label_feedrate.Location = new System.Drawing.Point(13, 69);
            this.label_feedrate.Name = "label_feedrate";
            this.label_feedrate.Size = new System.Drawing.Size(69, 17);
            this.label_feedrate.TabIndex = 6;
            this.label_feedrate.Text = "Feed rate";
            // 
            // num_rapidrate
            // 
            this.num_rapidrate.DecimalPlaces = 2;
            this.num_rapidrate.Increment = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.num_rapidrate.Location = new System.Drawing.Point(160, 92);
            this.num_rapidrate.Maximum = new decimal(new int[] {
            5000,
            0,
            0,
            0});
            this.num_rapidrate.Name = "num_rapidrate";
            this.num_rapidrate.Size = new System.Drawing.Size(120, 22);
            this.num_rapidrate.TabIndex = 9;
            this.num_rapidrate.Value = new decimal(new int[] {
            100,
            0,
            0,
            0});
            // 
            // label_rapidrate
            // 
            this.label_rapidrate.AutoSize = true;
            this.label_rapidrate.Location = new System.Drawing.Point(13, 97);
            this.label_rapidrate.Name = "label_rapidrate";
            this.label_rapidrate.Size = new System.Drawing.Size(74, 17);
            this.label_rapidrate.TabIndex = 8;
            this.label_rapidrate.Text = "Rapid rate";
            // 
            // ext_axis_checkbox
            // 
            this.ext_axis_checkbox.AutoSize = true;
            this.ext_axis_checkbox.Location = new System.Drawing.Point(160, 120);
            this.ext_axis_checkbox.Name = "ext_axis_checkbox";
            this.ext_axis_checkbox.Size = new System.Drawing.Size(109, 21);
            this.ext_axis_checkbox.TabIndex = 10;
            this.ext_axis_checkbox.Text = "External axis";
            this.ext_axis_checkbox.UseVisualStyleBackColor = true;
            // 
            // tasTP_Processor2Settings_Form
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(292, 268);
            this.Controls.Add(this.ext_axis_checkbox);
            this.Controls.Add(this.num_rapidrate);
            this.Controls.Add(this.label_rapidrate);
            this.Controls.Add(this.num_feedrate);
            this.Controls.Add(this.label_feedrate);
            this.Controls.Add(this.num_rapidz);
            this.Controls.Add(this.label_rapidz);
            this.Controls.Add(this.num_safez);
            this.Controls.Add(this.label_safez);
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.button_ok);
            this.Name = "tasTP_Processor2Settings_Form";
            this.Text = "Toolpath settings";
            this.Load += new System.EventHandler(this.tasTP_Processor2Settings_Form_Load);
            ((System.ComponentModel.ISupportInitialize)(this.num_safez)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_rapidz)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_feedrate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.num_rapidrate)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button button_ok;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.Label label_safez;
        private System.Windows.Forms.NumericUpDown num_safez;
        private System.Windows.Forms.NumericUpDown num_rapidz;
        private System.Windows.Forms.Label label_rapidz;
        private System.Windows.Forms.NumericUpDown num_feedrate;
        private System.Windows.Forms.Label label_feedrate;
        private System.Windows.Forms.NumericUpDown num_rapidrate;
        private System.Windows.Forms.Label label_rapidrate;
        private System.Windows.Forms.CheckBox ext_axis_checkbox;
    }
}