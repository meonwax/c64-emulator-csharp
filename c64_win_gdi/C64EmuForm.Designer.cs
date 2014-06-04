namespace c64_win_gdi
{
	partial class C64EmuForm
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
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(C64EmuForm));
			this.panel1 = new System.Windows.Forms.Panel();
			this._toolBar = new System.Windows.Forms.ToolStrip();
			this._tbOpenState = new System.Windows.Forms.ToolStripButton();
			this._tbSaveState = new System.Windows.Forms.ToolStripButton();
			this._tbSwapJoystick = new System.Windows.Forms.ToolStripButton();
			this._tbAttachDiskImage = new System.Windows.Forms.ToolStripButton();
			this._tbRestartEmulator = new System.Windows.Forms.ToolStripButton();
			this._dlgAttachDiskImage = new System.Windows.Forms.OpenFileDialog();
			this._dlgOpenState = new System.Windows.Forms.OpenFileDialog();
			this._dlgSaveState = new System.Windows.Forms.SaveFileDialog();
			this.panel1.SuspendLayout();
			this._toolBar.SuspendLayout();
			this.SuspendLayout();
			// 
			// panel1
			// 
			this.panel1.BackColor = System.Drawing.SystemColors.ButtonHighlight;
			this.panel1.Controls.Add(this._toolBar);
			this.panel1.Location = new System.Drawing.Point(13, 13);
			this.panel1.Name = "panel1";
			this.panel1.Size = new System.Drawing.Size(617, 456);
			this.panel1.TabIndex = 0;
			// 
			// _toolBar
			// 
			this._toolBar.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this._tbOpenState,
            this._tbSaveState,
            this._tbSwapJoystick,
            this._tbAttachDiskImage,
            this._tbRestartEmulator});
			this._toolBar.Location = new System.Drawing.Point(0, 0);
			this._toolBar.Name = "_toolBar";
			this._toolBar.Size = new System.Drawing.Size(617, 25);
			this._toolBar.TabIndex = 0;
			this._toolBar.Text = "toolStrip1";
			// 
			// _tbOpenState
			// 
			this._tbOpenState.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._tbOpenState.Image = ((System.Drawing.Image)(resources.GetObject("_tbOpenState.Image")));
			this._tbOpenState.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._tbOpenState.Name = "_tbOpenState";
			this._tbOpenState.Size = new System.Drawing.Size(23, 22);
			this._tbOpenState.Text = "Open State";
			this._tbOpenState.ToolTipText = "Open Emulator State";
			this._tbOpenState.Click += new System.EventHandler(this._tbLoadState_Click);
			// 
			// _tbSaveState
			// 
			this._tbSaveState.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._tbSaveState.Image = ((System.Drawing.Image)(resources.GetObject("_tbSaveState.Image")));
			this._tbSaveState.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._tbSaveState.Name = "_tbSaveState";
			this._tbSaveState.Size = new System.Drawing.Size(23, 22);
			this._tbSaveState.Text = "Save State";
			this._tbSaveState.ToolTipText = "Save Emulator State";
			this._tbSaveState.Click += new System.EventHandler(this._tbSaveState_Click);
			// 
			// _tbSwapJoystick
			// 
			this._tbSwapJoystick.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._tbSwapJoystick.Image = ((System.Drawing.Image)(resources.GetObject("_tbSwapJoystick.Image")));
			this._tbSwapJoystick.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._tbSwapJoystick.Name = "_tbSwapJoystick";
			this._tbSwapJoystick.Size = new System.Drawing.Size(23, 22);
			this._tbSwapJoystick.Text = "Swap Joystick";
			this._tbSwapJoystick.ToolTipText = "Swap Joystick";
			this._tbSwapJoystick.Click += new System.EventHandler(this._tbSwapJoystick_Click);
			// 
			// _tbAttachDiskImage
			// 
			this._tbAttachDiskImage.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._tbAttachDiskImage.Image = ((System.Drawing.Image)(resources.GetObject("_tbAttachDiskImage.Image")));
			this._tbAttachDiskImage.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._tbAttachDiskImage.Name = "_tbAttachDiskImage";
			this._tbAttachDiskImage.Size = new System.Drawing.Size(23, 22);
			this._tbAttachDiskImage.Text = "Attach Disk Image";
			this._tbAttachDiskImage.ToolTipText = "Attach Disk Image";
			this._tbAttachDiskImage.Click += new System.EventHandler(this._tbAttachDiskImage_Click);
			// 
			// _tbRestartEmulator
			// 
			this._tbRestartEmulator.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
			this._tbRestartEmulator.Image = ((System.Drawing.Image)(resources.GetObject("_tbRestartEmulator.Image")));
			this._tbRestartEmulator.ImageTransparentColor = System.Drawing.Color.Magenta;
			this._tbRestartEmulator.Name = "_tbRestartEmulator";
			this._tbRestartEmulator.Size = new System.Drawing.Size(23, 22);
			this._tbRestartEmulator.Text = "Restart";
			this._tbRestartEmulator.ToolTipText = "Restart Emulator";
			this._tbRestartEmulator.Click += new System.EventHandler(this._tbRestartEmulator_Click);
			// 
			// _dlgAttachDiskImage
			// 
			this._dlgAttachDiskImage.Filter = "CBM 1541-II Disk Image files (*.D64)|*.txt|All files (*.*)|*.*";
			// 
			// _dlgOpenState
			// 
			this._dlgOpenState.Filter = "C64 Emulator State files (*.EMS)|*.txt|All files (*.*)|*.*";
			// 
			// _dlgSaveState
			// 
			this._dlgSaveState.Filter = "C64 Emulator State files (*.EMS)|*.txt|All files (*.*)|*.*";
			// 
			// C64EmuForm
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(642, 481);
			this.Controls.Add(this.panel1);
			this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
			this.KeyPreview = true;
			this.Name = "C64EmuForm";
			this.Text = "Commodore 64 Emulator";
			this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.C64EmuForm_FormClosing);
			this.Load += new System.EventHandler(this.C64EmuForm_Load);
			this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
			this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyUp);
			this.panel1.ResumeLayout(false);
			this.panel1.PerformLayout();
			this._toolBar.ResumeLayout(false);
			this._toolBar.PerformLayout();
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.ToolStrip _toolBar;
		private System.Windows.Forms.ToolStripButton _tbOpenState;
		private System.Windows.Forms.ToolStripButton _tbSaveState;
		private System.Windows.Forms.ToolStripButton _tbSwapJoystick;
		private System.Windows.Forms.ToolStripButton _tbAttachDiskImage;
        private System.Windows.Forms.OpenFileDialog _dlgAttachDiskImage;
        private System.Windows.Forms.OpenFileDialog _dlgOpenState;
        private System.Windows.Forms.SaveFileDialog _dlgSaveState;
		private System.Windows.Forms.ToolStripButton _tbRestartEmulator;
	}
}

