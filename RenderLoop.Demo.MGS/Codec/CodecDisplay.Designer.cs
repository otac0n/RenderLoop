namespace RenderLoop.Demo.MGS.Codec
{
    partial class CodecDisplay
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
            System.Windows.Forms.Label border1;
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.nameLabel = new System.Windows.Forms.Label();
            this.speechBox = new System.Windows.Forms.TextBox();
            this.sayButton = new System.Windows.Forms.Button();
            this.display = new System.Windows.Forms.PictureBox();
            this.volumeMeter = new System.Windows.Forms.PictureBox();
            this.inputsPanel = new System.Windows.Forms.Panel();
            this.closeButton = new System.Windows.Forms.Button();
            this.captionLabel = new System.Windows.Forms.TextBox();
            border1 = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)this.display).BeginInit();
            ((System.ComponentModel.ISupportInitialize)this.volumeMeter).BeginInit();
            this.inputsPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // border1
            // 
            border1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            border1.Location = new System.Drawing.Point(194, 99);
            border1.Name = "border1";
            border1.Size = new System.Drawing.Size(181, 113);
            border1.TabIndex = 3;
            // 
            // updateTimer
            // 
            this.updateTimer.Enabled = true;
            this.updateTimer.Interval = 33;
            // 
            // nameLabel
            // 
            this.nameLabel.AutoSize = true;
            this.nameLabel.BackColor = System.Drawing.Color.Transparent;
            this.nameLabel.Location = new System.Drawing.Point(49, 298);
            this.nameLabel.Margin = new System.Windows.Forms.Padding(3, 20, 3, 0);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(121, 21);
            this.nameLabel.TabIndex = 1;
            this.nameLabel.Text = "Hal Emmerich";
            // 
            // speechBox
            // 
            this.speechBox.AcceptsReturn = true;
            this.speechBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.speechBox.BackColor = System.Drawing.Color.Black;
            this.speechBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.speechBox.ForeColor = System.Drawing.Color.White;
            this.speechBox.Location = new System.Drawing.Point(20, 14);
            this.speechBox.Margin = new System.Windows.Forms.Padding(20, 5, 5, 5);
            this.speechBox.Multiline = true;
            this.speechBox.Name = "speechBox";
            this.speechBox.Size = new System.Drawing.Size(324, 37);
            this.speechBox.TabIndex = 0;
            this.speechBox.KeyPress += this.SpeechBox_KeyPress;
            // 
            // sayButton
            // 
            this.sayButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.sayButton.AutoSize = true;
            this.sayButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.sayButton.Location = new System.Drawing.Point(354, 14);
            this.sayButton.Margin = new System.Windows.Forms.Padding(5);
            this.sayButton.Name = "sayButton";
            this.sayButton.Size = new System.Drawing.Size(58, 37);
            this.sayButton.TabIndex = 1;
            this.sayButton.Text = "PTT";
            this.sayButton.UseVisualStyleBackColor = true;
            this.sayButton.Click += this.SayButton_Click;
            // 
            // display
            // 
            this.display.Location = new System.Drawing.Point(20, 61);
            this.display.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.display.Name = "display";
            this.display.Size = new System.Drawing.Size(127, 212);
            this.display.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.display.TabIndex = 3;
            this.display.TabStop = false;
            // 
            // volumeMeter
            // 
            this.volumeMeter.Location = new System.Drawing.Point(185, 105);
            this.volumeMeter.Name = "volumeMeter";
            this.volumeMeter.Size = new System.Drawing.Size(200, 100);
            this.volumeMeter.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.volumeMeter.TabIndex = 4;
            this.volumeMeter.TabStop = false;
            // 
            // inputsPanel
            // 
            this.inputsPanel.Controls.Add(this.speechBox);
            this.inputsPanel.Controls.Add(this.sayButton);
            this.inputsPanel.Controls.Add(this.closeButton);
            this.inputsPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.inputsPanel.Location = new System.Drawing.Point(0, 0);
            this.inputsPanel.Name = "inputsPanel";
            this.inputsPanel.Size = new System.Drawing.Size(478, 53);
            this.inputsPanel.TabIndex = 2;
            // 
            // closeButton
            // 
            this.closeButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right;
            this.closeButton.AutoSize = true;
            this.closeButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.closeButton.ForeColor = System.Drawing.Color.Firebrick;
            this.closeButton.Location = new System.Drawing.Point(422, 14);
            this.closeButton.Margin = new System.Windows.Forms.Padding(5, 5, 20, 5);
            this.closeButton.Name = "closeButton";
            this.closeButton.Size = new System.Drawing.Size(36, 37);
            this.closeButton.TabIndex = 2;
            this.closeButton.TabStop = false;
            this.closeButton.Text = "×";
            this.closeButton.UseVisualStyleBackColor = true;
            this.closeButton.Click += this.CloseButton_Click;
            // 
            // captionLabel
            // 
            this.captionLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.captionLabel.BackColor = System.Drawing.Color.Black;
            this.captionLabel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.captionLabel.ForeColor = System.Drawing.Color.White;
            this.captionLabel.Location = new System.Drawing.Point(49, 323);
            this.captionLabel.Margin = new System.Windows.Forms.Padding(40, 0, 40, 20);
            this.captionLabel.Multiline = true;
            this.captionLabel.Name = "captionLabel";
            this.captionLabel.ReadOnly = true;
            this.captionLabel.Size = new System.Drawing.Size(380, 106);
            this.captionLabel.TabIndex = 5;
            this.captionLabel.TabStop = false;
            // 
            // CodecDisplay
            // 
            this.BackColor = System.Drawing.Color.Black;
            this.CancelButton = this.closeButton;
            this.ClientSize = new System.Drawing.Size(478, 458);
            this.Controls.Add(this.captionLabel);
            this.Controls.Add(this.display);
            this.Controls.Add(this.nameLabel);
            this.Controls.Add(this.volumeMeter);
            this.Controls.Add(this.inputsPanel);
            this.Controls.Add(border1);
            this.Font = new System.Drawing.Font("Arial", 9F);
            this.ForeColor = System.Drawing.Color.Green;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximizeBox = false;
            this.Name = "CodecDisplay";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            ((System.ComponentModel.ISupportInitialize)this.display).EndInit();
            ((System.ComponentModel.ISupportInitialize)this.volumeMeter).EndInit();
            this.inputsPanel.ResumeLayout(false);
            this.inputsPanel.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Timer updateTimer;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TextBox speechBox;
        private System.Windows.Forms.Button sayButton;
        private System.Windows.Forms.PictureBox display;
        private System.Windows.Forms.PictureBox volumeMeter;
        private System.Windows.Forms.Panel inputsPanel;
        private System.Windows.Forms.TextBox captionLabel;
        private System.Windows.Forms.Button closeButton;
        private System.Windows.Forms.Label border1;
    }
}
