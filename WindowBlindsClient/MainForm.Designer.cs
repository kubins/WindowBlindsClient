﻿namespace WindowBlindsClient
{
    partial class MainForm
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
          System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
          this.notifyIcon = new System.Windows.Forms.NotifyIcon(this.components);
          this.timerRefresh = new System.Windows.Forms.Timer(this.components);
          this.SuspendLayout();
          // 
          // notifyIcon
          // 
          this.notifyIcon.BalloonTipIcon = System.Windows.Forms.ToolTipIcon.Info;
          this.notifyIcon.BalloonTipText = "Show the app by double click";
          this.notifyIcon.BalloonTipTitle = "Window Blinds Client";
          this.notifyIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon.Icon")));
          this.notifyIcon.Text = "Window Blinds Client";
          this.notifyIcon.DoubleClick += new System.EventHandler(this.notifyIcon_DoubleClick);
          // 
          // timerRefresh
          // 
          this.timerRefresh.Enabled = true;
          this.timerRefresh.Tick += new System.EventHandler(this.timerRefresh_Tick);
          // 
          // MainForm
          // 
          this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
          this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
          this.ClientSize = new System.Drawing.Size(294, 272);
          this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
          this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
          this.MaximizeBox = false;
          this.Name = "MainForm";
          this.Text = "Window Blinds Client";
          this.Shown += new System.EventHandler(this.MainForm_Shown);
          this.Resize += new System.EventHandler(this.MainForm_Resize);
          this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.NotifyIcon notifyIcon;
        private System.Windows.Forms.Timer timerRefresh;

    }
}

