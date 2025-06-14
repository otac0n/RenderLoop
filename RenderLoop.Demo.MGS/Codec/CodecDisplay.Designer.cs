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
            this.updateTimer = new System.Windows.Forms.Timer(this.components);
            this.captionLabel = new System.Windows.Forms.Label();
            this.nameLabel = new System.Windows.Forms.Label();
            this.inputsPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.speechBox = new System.Windows.Forms.TextBox();
            this.sayButton = new System.Windows.Forms.Button();
            this.display = new System.Windows.Forms.PictureBox();
            this.inputsPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)this.display).BeginInit();
            this.SuspendLayout();
            // 
            // updateTimer
            // 
            this.updateTimer.Enabled = true;
            this.updateTimer.Interval = 33;
            // 
            // captionLabel
            // 
            this.captionLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.captionLabel.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
            this.captionLabel.ForeColor = System.Drawing.Color.White;
            this.captionLabel.Location = new System.Drawing.Point(29, 282);
            this.captionLabel.Margin = new System.Windows.Forms.Padding(20);
            this.captionLabel.Name = "captionLabel";
            this.captionLabel.Size = new System.Drawing.Size(420, 133);
            this.captionLabel.TabIndex = 0;
            // 
            // nameLabel
            // 
            this.nameLabel.AutoSize = true;
            this.nameLabel.Location = new System.Drawing.Point(168, 103);
            this.nameLabel.Name = "nameLabel";
            this.nameLabel.Size = new System.Drawing.Size(121, 25);
            this.nameLabel.TabIndex = 1;
            this.nameLabel.Text = "Hal Emmerich";
            // 
            // inputsPanel
            // 
            this.inputsPanel.AutoSize = true;
            this.inputsPanel.Controls.Add(this.speechBox);
            this.inputsPanel.Controls.Add(this.sayButton);
            this.inputsPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.inputsPanel.Location = new System.Drawing.Point(0, 0);
            this.inputsPanel.Name = "inputsPanel";
            this.inputsPanel.Size = new System.Drawing.Size(478, 43);
            this.inputsPanel.TabIndex = 2;
            this.inputsPanel.WrapContents = false;
            // 
            // speechBox
            // 
            this.speechBox.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
            this.speechBox.BackColor = System.Drawing.Color.Black;
            this.speechBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.speechBox.ForeColor = System.Drawing.Color.White;
            this.speechBox.Location = new System.Drawing.Point(3, 3);
            this.speechBox.Multiline = true;
            this.speechBox.Name = "speechBox";
            this.speechBox.Size = new System.Drawing.Size(373, 37);
            this.speechBox.TabIndex = 0;
            this.speechBox.Text = "Hey, snake! Get your head in the game.";
            // 
            // sayButton
            // 
            this.sayButton.AutoSize = true;
            this.sayButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.sayButton.Location = new System.Drawing.Point(382, 3);
            this.sayButton.Name = "sayButton";
            this.sayButton.Size = new System.Drawing.Size(54, 37);
            this.sayButton.TabIndex = 1;
            this.sayButton.Text = "Say";
            this.sayButton.UseVisualStyleBackColor = true;
            this.sayButton.Click += this.SayButton_Click;
            // 
            // display
            // 
            this.display.Location = new System.Drawing.Point(12, 47);
            this.display.Name = "display";
            this.display.Size = new System.Drawing.Size(150, 212);
            this.display.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.display.TabIndex = 3;
            this.display.TabStop = false;
            // 
            // CodecDisplay
            // 
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(478, 444);
            this.Controls.Add(this.display);
            this.Controls.Add(this.inputsPanel);
            this.Controls.Add(this.nameLabel);
            this.Controls.Add(this.captionLabel);
            this.ForeColor = System.Drawing.Color.Green;
            this.Name = "CodecDisplay";
            this.inputsPanel.ResumeLayout(false);
            this.inputsPanel.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)this.display).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Timer updateTimer;
        private System.Windows.Forms.Label captionLabel;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.FlowLayoutPanel inputsPanel;
        private System.Windows.Forms.TextBox speechBox;
        private System.Windows.Forms.Button sayButton;
        private System.Windows.Forms.PictureBox display;
    }
}
