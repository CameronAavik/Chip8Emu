namespace Chip8Emulator
{
    partial class Chip8EmuForm
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
            this.SuspendLayout();
            // 
            // Chip8EmuForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(624, 281);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Chip8EmuForm";
            this.Text = "Chip8EmuForm";
            this.Load += new System.EventHandler(this.Chip8EmuForm_Load);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Chip8EmuForm_Paint);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Chip8EmuForm_KeyDown);
            this.KeyUp += new System.Windows.Forms.KeyEventHandler(this.Chip8EmuForm_KeyUp);
            this.ResumeLayout(false);

        }

        #endregion
    }
}

