namespace RenderLoop.Demo.MGS.MGS2.Otacon
{
    partial class OtaconDisplay
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
            this.components = new System.ComponentModel.Container();
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // updateTimer
            // 
            this.updateTimer.Enabled = true;
            this.updateTimer.Interval = 33;
            this.updateTimer.Tick += this.UpdateTimer_Tick;
            // 
            // OtaconDisplay
            // 
            this.BackColor = System.Drawing.Color.Magenta;
            this.ClientSize = new System.Drawing.Size(207, 279);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "OtaconDisplay";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.TopMost = true;
            this.TransparencyKey = System.Drawing.Color.Magenta;
            this.Load += this.Form_Load;
            this.Paint += this.Form_Paint;
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Timer updateTimer;
    }
}
