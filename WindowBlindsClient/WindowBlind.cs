using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Windows.Forms;

namespace WindowBlindsClient
{
  public class TrackBarWithoutFocus : TrackBar
  {
    private const int WM_SETFOCUS = 0x0007;

    protected override void WndProc(ref Message m)
    {
      if (m.Msg == WM_SETFOCUS)
      {
        return;
      }

      base.WndProc(ref m);
    }
  }

  class WindowBlind
  {
    public delegate void WindowBlindChanged(object Sender, int nValue);
    public event WindowBlindChanged OnWindowBlindChanged;

    private TrackBarWithoutFocus trackBar;
    private PictureBox pictureBox;
    private Label labelName;
    private Label labelShortcutUpDown;

    private static Image[] arrImages =
        {
            WindowBlindsClient.Properties.Resources.blind_13,
            WindowBlindsClient.Properties.Resources.blind_12,
            WindowBlindsClient.Properties.Resources.blind_11,
            WindowBlindsClient.Properties.Resources.blind_10,
            WindowBlindsClient.Properties.Resources.blind_9,
            WindowBlindsClient.Properties.Resources.blind_8,
            WindowBlindsClient.Properties.Resources.blind_7,
            WindowBlindsClient.Properties.Resources.blind_6,
            WindowBlindsClient.Properties.Resources.blind_5,
            WindowBlindsClient.Properties.Resources.blind_4,
            WindowBlindsClient.Properties.Resources.blind_3,
            WindowBlindsClient.Properties.Resources.blind_2,
            WindowBlindsClient.Properties.Resources.blind_1,
            WindowBlindsClient.Properties.Resources.blind_0
        };

    public WindowBlind(Form TargetForm, Point Position, string strName, string strShortcutUp, string strShortcutDown)
    {
      Size DumpSize = new Size(0, 0);
      Init(TargetForm, Position, strName, strShortcutUp, strShortcutDown, ref DumpSize);
    }

    public WindowBlind(Form TargetForm, Point Position, string strName, string strShortcutUp, string strShortcutDown, ref Size Size)
    {
      Init(TargetForm, Position, strName, strShortcutUp, strShortcutDown, ref Size);
    }

    public void SetValue(int nValue)
    {
      if (trackBar.Value != nValue)
      {
        trackBar.Value = nValue;
      }
    }

    public void Increment()
    {
      int nStep = (trackBar.Maximum - trackBar.Minimum) / 10;
      trackBar.Value = Math.Min(trackBar.Value + nStep, trackBar.Maximum);
    }

    public void Decrement()
    {
      int nStep = (trackBar.Maximum - trackBar.Minimum) / 10;
      trackBar.Value = Math.Max(trackBar.Value - nStep, trackBar.Minimum);
    }

    private void Init(Form TargetForm, Point Position, string strName, string strShortcutUp, string strShortcutDown, ref Size Size)
    {
      labelName = new Label();
      pictureBox = new PictureBox();
      trackBar = new TrackBarWithoutFocus();
      labelShortcutUpDown = new Label();
      ((System.ComponentModel.ISupportInitialize)(pictureBox)).BeginInit();
      ((System.ComponentModel.ISupportInitialize)(trackBar)).BeginInit();
      TargetForm.SuspendLayout();

      labelName.AutoSize = true;
      labelName.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(238)));
      labelShortcutUpDown.MaximumSize = new Size(150, 0);
      labelName.Location = Position;
      labelName.Name = "labelName";
      labelName.TabIndex = 0;
      labelName.Text = strName;

      pictureBox.ErrorImage = null;
      pictureBox.InitialImage = null;
      pictureBox.Image = global::WindowBlindsClient.Properties.Resources.blind_13;
      pictureBox.Location = new Point(Position.X, Position.Y + labelName.Size.Height);
      pictureBox.Size = new System.Drawing.Size(labelShortcutUpDown.MaximumSize.Width, 190);
      pictureBox.Name = "pictureBox";
      pictureBox.TabIndex = 0;
      pictureBox.TabStop = false;

      trackBar.LargeChange = 10;
      trackBar.SmallChange = 5;
      trackBar.Location = new Point(pictureBox.Location.X + pictureBox.Size.Width, pictureBox.Location.Y);
      trackBar.Size = new Size(20, pictureBox.Size.Height);
      trackBar.Maximum = 100;
      trackBar.Minimum = 0;
      trackBar.Name = "trackBar";
      trackBar.Orientation = System.Windows.Forms.Orientation.Vertical;
      trackBar.TabIndex = 0;
      trackBar.TickFrequency = 10;
      trackBar.TabStop = false;
      trackBar.ValueChanged += new System.EventHandler(trackBar_ValueChanged);

      labelShortcutUpDown.AutoSize = true;
      labelShortcutUpDown.Location = new Point(Position.X, pictureBox.Location.Y + pictureBox.Height);
      labelShortcutUpDown.MaximumSize = new Size(pictureBox.Size.Width, 0);
      labelShortcutUpDown.Name = "labelShortcutUp";
      labelShortcutUpDown.TabIndex = 0;
      labelShortcutUpDown.Text = "shortcut ▲ = " + strShortcutUp + ", ▼ = " + strShortcutDown;

      TargetForm.Controls.Add(labelName);
      TargetForm.Controls.Add(pictureBox);
      TargetForm.Controls.Add(trackBar);
      TargetForm.Controls.Add(labelShortcutUpDown);

      ((System.ComponentModel.ISupportInitialize)(pictureBox)).EndInit();
      ((System.ComponentModel.ISupportInitialize)(trackBar)).EndInit();
      TargetForm.ResumeLayout(false);
      TargetForm.PerformLayout();

      // nastavíme aktuální velikost
      Size = new Size(trackBar.Location.X + trackBar.Size.Width - Position.X, labelShortcutUpDown.Location.Y + labelShortcutUpDown.Size.Height - Position.Y);
    }

    private void trackBar_ValueChanged(object sender, EventArgs e)
    {
      // nastavíme grafiku
      pictureBox.Image = arrImages[Math.Min((arrImages.Length * (int)trackBar.Value) / trackBar.Maximum, arrImages.Length - 1)];

      if (OnWindowBlindChanged != null)
      {
        // pokud někdo poslouchá, odešleme informaci o změně
        OnWindowBlindChanged(this, (int)trackBar.Value);
      }
    }
  }
}
