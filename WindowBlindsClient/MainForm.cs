using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Gma.UserActivityMonitor;
using Microsoft.Win32;
using System.Net.Sockets;
using System.Net;

namespace WindowBlindsClient
{
  public partial class MainForm : Form
  {
    enum Loading
    {
      None,
      Global,
      Connection,
      Blinds
    }

    struct Blind
    {
      public int m_nX;
      public int m_nY;
      public string m_strName;
      public int m_nID;
      public string m_strShortcutUp;
      public string m_strShortcutDown;

      public void Clear()
      {
        m_nX = -1;
        m_nY = -1;
        m_strName = "";
        m_nID = -1;
        m_strShortcutUp = "";
        m_strShortcutDown = "";
      }
    }

    struct Configuration
    {
      public bool m_bAutoRun;
      public bool m_bHideWhenMinimize;
      public string m_strServerAddress;
      public List<Blind> m_arrBlinds;
    }

    public class Pair<F, S>
    {
      public Pair() { }
      public Pair(F First, S Second)
      {
        m_First = First;
        m_Second = Second;
      }

      public F m_First { get; set; }
      public S m_Second { get; set; }
    };

    public class StateObject
    {
      // client socket
      public Socket m_Socket = null;
      // size of receive buffer
      public const int m_nBufferSize = 256;
      // receive buffer
      public byte[] m_nBuffer = new byte[m_nBufferSize];
      // received data string
      public List<string> m_arrReceivedMessages;
    }

    private Configuration g_Configuration;
    private List<WindowBlind> g_arrWindowBlinds;
    private List<int> g_arrKeys;
    private List<Pair<String, int>> g_arrKeyCodes;
    private bool g_bShowBalloon = false;
    private IPEndPoint g_IPEndPoint;
    private UdpClient g_UdpSocketForSending;
    private Socket g_UdpSocketForReceiving;
    private StateObject m_StateObjectForReceiving;

    public MainForm()
    {
      InitializeComponent();
      LoadConfiguration();
      LoadGraphics();
      // zobrazíme verzi
      Text = "Window Blinds Client " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
      // globální zachytávání událostí
      g_arrKeys = new List<int>();
      LoadKeyCodes();
      HookManager.KeyDown += HookManager_KeyDown;
      HookManager.KeyUp += HookManager_KeyUp;
      // nastavení "po spuštění"
      RegistryKey RunAtStartupKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
      if (g_Configuration.m_bAutoRun)
      {
        RunAtStartupKey.SetValue("WindowBlindsClient", Application.ExecutablePath.ToString());
      }
      else
      {
        RunAtStartupKey.DeleteValue("WindowBlindsClient", false);
      }
      // nastavení komunikace se serverem
      try
      {
        g_IPEndPoint = new IPEndPoint(IPAddress.Parse(g_Configuration.m_strServerAddress), 5674);
        g_UdpSocketForSending = new UdpClient();
        g_UdpSocketForSending.Connect(g_IPEndPoint);
        g_UdpSocketForReceiving = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        for(int nPortOffset = 0; nPortOffset < 10; nPortOffset++)
        {
          try
          {
            g_UdpSocketForReceiving.Bind(new IPEndPoint(IPAddress.Any, 5675 + nPortOffset));
            break;
          }
          catch (Exception ex)
          {
            Console.WriteLine(ex.Message);
          }
        }
        if (!g_UdpSocketForReceiving.IsBound)
        {
          throw new Exception("the socket cannot be bound");
        }
        m_StateObjectForReceiving = new StateObject();
        m_StateObjectForReceiving.m_arrReceivedMessages = new List<string>();
        m_StateObjectForReceiving.m_Socket = g_UdpSocketForReceiving;
        g_UdpSocketForReceiving.BeginReceive(m_StateObjectForReceiving.m_nBuffer, 0, StateObject.m_nBufferSize, 0, new AsyncCallback(ReceiveCallback), m_StateObjectForReceiving);
        // it registers the client to the server
        Byte[] arrBytes = Encoding.ASCII.GetBytes("register;" + ((IPEndPoint)g_UdpSocketForReceiving.LocalEndPoint).Port.ToString() + "#");
        g_UdpSocketForSending.Send(arrBytes, arrBytes.Length);
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Configuration loading error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void ReceiveCallback(IAsyncResult AsyncResult)
    {
      try
      {
        StateObject State = (StateObject)AsyncResult.AsyncState;
        State.m_arrReceivedMessages.AddRange(Encoding.ASCII.GetString(State.m_nBuffer, 0, State.m_Socket.EndReceive(AsyncResult)).Split('#'));
        State.m_Socket.BeginReceive(State.m_nBuffer, 0, StateObject.m_nBufferSize, 0, new AsyncCallback(ReceiveCallback), State);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }
    }

    private void timerRefresh_Tick(object sender, EventArgs e)
    {
      int nCount = m_StateObjectForReceiving.m_arrReceivedMessages.Count;
      for(int nIndex = 0; nIndex < nCount; nIndex++)
      {
        try
        {
          string strMessage = m_StateObjectForReceiving.m_arrReceivedMessages[nIndex];
          string[] arr_strMessageData = strMessage.Split(';');
          if (arr_strMessageData.Length >= 1)
          {
            if (arr_strMessageData[0] == "blind_position")
            {
              if (arr_strMessageData.Length >= 3)
              {
                int nBlindIndex = int.Parse(arr_strMessageData[1]) - 1;
                int nBlindPosition = 100 - int.Parse(arr_strMessageData[2]);
                if(nBlindIndex >= 0 && nBlindIndex < g_arrWindowBlinds.Count)
                {
                  g_arrWindowBlinds[nBlindIndex].SetValue(nBlindPosition, true);
                }
              }
            }
          }
        }
        catch (Exception ex)
        {
          Console.WriteLine(ex.Message);
        }
      }
      m_StateObjectForReceiving.m_arrReceivedMessages.RemoveRange(0, nCount);
    }

    private void LoadKeyCodes()
    {
      g_arrKeyCodes = new List<Pair<string, int>>();
      g_arrKeyCodes.Add(new Pair<string, int>("SHIFT", 160));
      g_arrKeyCodes.Add(new Pair<string, int>("SHIFT", 161));
      g_arrKeyCodes.Add(new Pair<string, int>("CTRL", 162));
      g_arrKeyCodes.Add(new Pair<string, int>("CTRL", 163));
      g_arrKeyCodes.Add(new Pair<string, int>("ALT", 164));
      g_arrKeyCodes.Add(new Pair<string, int>("ALT", 165));
      g_arrKeyCodes.Add(new Pair<string, int>("UP", 38));
      g_arrKeyCodes.Add(new Pair<string, int>("LEFT", 39));
      g_arrKeyCodes.Add(new Pair<string, int>("DOWN", 40));
      g_arrKeyCodes.Add(new Pair<string, int>("RIGHT", 37));
      g_arrKeyCodes.Add(new Pair<string, int>("A", 65));
      g_arrKeyCodes.Add(new Pair<string, int>("B", 66));
      g_arrKeyCodes.Add(new Pair<string, int>("C", 67));
      g_arrKeyCodes.Add(new Pair<string, int>("D", 68));
      g_arrKeyCodes.Add(new Pair<string, int>("E", 69));
      g_arrKeyCodes.Add(new Pair<string, int>("F", 70));
      g_arrKeyCodes.Add(new Pair<string, int>("G", 71));
      g_arrKeyCodes.Add(new Pair<string, int>("H", 72));
      g_arrKeyCodes.Add(new Pair<string, int>("I", 73));
      g_arrKeyCodes.Add(new Pair<string, int>("J", 74));
      g_arrKeyCodes.Add(new Pair<string, int>("K", 75));
      g_arrKeyCodes.Add(new Pair<string, int>("L", 76));
      g_arrKeyCodes.Add(new Pair<string, int>("M", 77));
      g_arrKeyCodes.Add(new Pair<string, int>("N", 78));
      g_arrKeyCodes.Add(new Pair<string, int>("O", 79));
      g_arrKeyCodes.Add(new Pair<string, int>("P", 80));
      g_arrKeyCodes.Add(new Pair<string, int>("Q", 81));
      g_arrKeyCodes.Add(new Pair<string, int>("R", 82));
      g_arrKeyCodes.Add(new Pair<string, int>("S", 83));
      g_arrKeyCodes.Add(new Pair<string, int>("T", 84));
      g_arrKeyCodes.Add(new Pair<string, int>("U", 85));
      g_arrKeyCodes.Add(new Pair<string, int>("V", 86));
      g_arrKeyCodes.Add(new Pair<string, int>("W", 87));
      g_arrKeyCodes.Add(new Pair<string, int>("X", 88));
      g_arrKeyCodes.Add(new Pair<string, int>("Y", 89));
      g_arrKeyCodes.Add(new Pair<string, int>("Z", 90));
      g_arrKeyCodes.Add(new Pair<string, int>("0", 96));
      g_arrKeyCodes.Add(new Pair<string, int>("1", 97));
      g_arrKeyCodes.Add(new Pair<string, int>("2", 98));
      g_arrKeyCodes.Add(new Pair<string, int>("3", 99));
      g_arrKeyCodes.Add(new Pair<string, int>("4", 100));
      g_arrKeyCodes.Add(new Pair<string, int>("5", 101));
      g_arrKeyCodes.Add(new Pair<string, int>("6", 102));
      g_arrKeyCodes.Add(new Pair<string, int>("7", 103));
      g_arrKeyCodes.Add(new Pair<string, int>("8", 104));
      g_arrKeyCodes.Add(new Pair<string, int>("9", 105));
      g_arrKeyCodes.Add(new Pair<string, int>("/", 111));
      g_arrKeyCodes.Add(new Pair<string, int>("*", 106));
      g_arrKeyCodes.Add(new Pair<string, int>("-", 109));
      g_arrKeyCodes.Add(new Pair<string, int>("+", 107));
      g_arrKeyCodes.Add(new Pair<string, int>(",", 188));
      g_arrKeyCodes.Add(new Pair<string, int>(".", 190));
    }

    private void LoadConfiguration()
    {
      try
      {
        using (StreamReader FileStreamReader = new StreamReader("config.ini", Encoding.GetEncoding("Windows-1250")))
        {
          List<string> arrFileLines = FileStreamReader.ReadToEnd().Split(new string[] { Environment.NewLine }, StringSplitOptions.None).ToList();
          Loading eLoading = Loading.None;
          int nBlindIndexToLoading = -1;
          Blind LoadingBlind = new Blind();
          foreach (string strLine in arrFileLines)
          {
            if (strLine == "[GLOBAL]")
            {
              eLoading = Loading.Global;
            }
            if (strLine == "[CONNECTION]")
            {
              eLoading = Loading.Connection;
            }
            if (strLine == "[BLINDS]")
            {
              eLoading = Loading.Blinds;
            }
            switch (eLoading)
            {
              case Loading.Global:
                if (strLine.IndexOf("AutoRun=") != -1)
                {
                  g_Configuration.m_bAutoRun = strLine.Substring(strLine.IndexOf("=") + 1) == "1";
                }
                if (strLine.IndexOf("HideWhenMinimize=") != -1)
                {
                  g_Configuration.m_bHideWhenMinimize = strLine.Substring(strLine.IndexOf("=") + 1) == "1";
                }
                break;
              case Loading.Connection:
                if (strLine.IndexOf("ServerAddress=") != -1)
                {
                  g_Configuration.m_strServerAddress = strLine.Substring(strLine.IndexOf("=") + 1);
                }
                break;
              case Loading.Blinds:
                if (nBlindIndexToLoading == -1)
                {
                  if (strLine.IndexOf("Count=") != -1)
                  {
                    g_Configuration.m_arrBlinds = new List<Blind>(int.Parse(strLine.Substring(strLine.IndexOf("=") + 1)));
                    nBlindIndexToLoading = 0;
                  }
                }
                else
                {
                  if (strLine.IndexOf("[BLIND" + (nBlindIndexToLoading + 1).ToString() + "]") != -1)
                  {
                    if (nBlindIndexToLoading > 0)
                    {
                      g_Configuration.m_arrBlinds.Add(LoadingBlind);
                    }
                    LoadingBlind.Clear();
                    nBlindIndexToLoading++;
                  }
                  if (strLine.IndexOf("Position=") != -1)
                  {
                    List<string> arrPositions = strLine.Substring(strLine.IndexOf("=") + 1).Split(new string[] { "," }, StringSplitOptions.None).ToList();
                    LoadingBlind.m_nX = int.Parse(arrPositions[0]);
                    LoadingBlind.m_nY = int.Parse(arrPositions[1]);
                  }
                  if (strLine.IndexOf("Name=") != -1)
                  {
                    LoadingBlind.m_strName = strLine.Substring(strLine.IndexOf("=") + 1);
                  }
                  if (strLine.IndexOf("ID=") != -1)
                  {
                    LoadingBlind.m_nID = int.Parse(strLine.Substring(strLine.IndexOf("=") + 1));
                  }
                  if (strLine.IndexOf("ShortcutUp=") != -1)
                  {
                    LoadingBlind.m_strShortcutUp = strLine.Substring(strLine.IndexOf("=") + 1);
                  }
                  if (strLine.IndexOf("ShortcutDown=") != -1)
                  {
                    LoadingBlind.m_strShortcutDown = strLine.Substring(strLine.IndexOf("=") + 1);
                  }
                }
                break;
            }
          }
          if (nBlindIndexToLoading > 0)
          {
            g_Configuration.m_arrBlinds.Add(LoadingBlind);
          }
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show(ex.Message, "Configuration loading error", MessageBoxButtons.OK, MessageBoxIcon.Error);
      }
    }

    private void LoadGraphics()
    {
      g_arrWindowBlinds = new List<WindowBlind>(g_Configuration.m_arrBlinds.Count());
      bool bCheckFirstPosition = true;
      Size WindowBlindSize = new Size(0, 0);
      Size WindowSize = new Size(0, 0);
      foreach (Blind BlindItem in g_Configuration.m_arrBlinds)
      {
        Point Position = new Point(20, 20);
        if (bCheckFirstPosition)
        {
          if (BlindItem.m_nX != 1 || BlindItem.m_nY != 1)
          {
            MessageBox.Show("The first blind configuraton must have position 1,1.", "Configuration loading error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            break;
          }
          bCheckFirstPosition = false;
          // vytvoříme grafiku
          g_arrWindowBlinds.Add(new WindowBlind(this, Position, BlindItem.m_strName, BlindItem.m_strShortcutUp, BlindItem.m_strShortcutDown, ref WindowBlindSize));
          WindowBlindSize += new Size(25, 5);
        }
        else
        {
          // vytvoříme grafiku
          Position.X += (BlindItem.m_nX - 1) * WindowBlindSize.Width;
          Position.Y += (BlindItem.m_nY - 1) * WindowBlindSize.Height;
          g_arrWindowBlinds.Add(new WindowBlind(this, Position, BlindItem.m_strName, BlindItem.m_strShortcutUp, BlindItem.m_strShortcutDown));
        }
        Size ActualWindowSize = new Size(Position.X + WindowBlindSize.Width + 5, Position.Y + WindowBlindSize.Height + 35);
        // nastavíme zachytávání událostí změny
        g_arrWindowBlinds[g_arrWindowBlinds.Count - 1].OnWindowBlindChanged += OnWindowBlindChanged;
        // nastavíme maximum
        if (WindowSize.Width < ActualWindowSize.Width)
        {
          WindowSize.Width = ActualWindowSize.Width;
        }
        if (WindowSize.Height < ActualWindowSize.Height)
        {
          WindowSize.Height = ActualWindowSize.Height;
        }
      }
      if (!bCheckFirstPosition)
      {
        Size = WindowSize;
      }
    }

    public void OnWindowBlindChanged(object Sender, int nValue)
    {
      if ((Control.ModifierKeys & Keys.Shift) != 0)
      {
        for (int nWindowBlindIndex = 0; nWindowBlindIndex < g_arrWindowBlinds.Count; nWindowBlindIndex++)
        {
          if (g_arrWindowBlinds[nWindowBlindIndex] != Sender)
          {
            g_arrWindowBlinds[nWindowBlindIndex].SetValue(nValue);
          }
        }
      }
      int nBlindIndex = g_arrWindowBlinds.IndexOf((WindowBlind)Sender);
      if (nBlindIndex != -1)
      {
        SendMovementRequest(g_Configuration.m_arrBlinds[nBlindIndex].m_nID, 100 - nValue);
      }
    }

    private void SendMovementRequest(int nID, int nValue)
    {
      try
      {
        Byte[] arrBytes = Encoding.ASCII.GetBytes("set_blind;" + nID.ToString() + ";" + nValue.ToString() + ";" + ((IPEndPoint)g_UdpSocketForReceiving.LocalEndPoint).Port.ToString() + "#");
        g_UdpSocketForSending.Send(arrBytes, arrBytes.Length);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex.Message);
      }
    }

    private void notifyIcon_DoubleClick(object sender, EventArgs e)
    {
      ShowHide(false);
    }

    private void MainForm_Resize(object sender, EventArgs e)
    {
      switch (WindowState)
      {
        case FormWindowState.Minimized:
          ShowHide(true);
          break;
      }
    }

    private void ShowHide(bool bHide)
    {
      if (bHide)
      {
        if (g_Configuration.m_bHideWhenMinimize)
        {
          notifyIcon.Visible = true;
          if (g_bShowBalloon)
          {
            g_bShowBalloon = false;
            notifyIcon.ShowBalloonTip(500);
          }
          Hide();
        }
      }
      else
      {
        notifyIcon.Visible = false;
        Show();
        WindowState = FormWindowState.Normal;
        // následující kód hodí okno nahoru
        TopMost = true;
        TopMost = false;
      }
    }

    private void MainForm_Shown(object sender, EventArgs e)
    {
      if (g_Configuration.m_bAutoRun)
      {
        g_bShowBalloon = true;
        WindowState = FormWindowState.Minimized;
      }
    }

    private void HookManager_KeyDown(object sender, KeyEventArgs e)
    {
      if (g_arrKeys.IndexOf(e.KeyValue) == -1)
      {
        g_arrKeys.Add(e.KeyValue);
      }
      ProcessShortcuts();
    }

    private void HookManager_KeyUp(object sender, KeyEventArgs e)
    {
      g_arrKeys.RemoveAll(x => x == e.KeyValue);
    }

    private void ProcessShortcuts()
    {
      for (int nBlindIndex = 0; nBlindIndex < g_Configuration.m_arrBlinds.Count; nBlindIndex++)
      {
        Blind BlindItem = g_Configuration.m_arrBlinds[nBlindIndex];
        List<string> arrShortcutUp = BlindItem.m_strShortcutUp.Split(new string[] { "," }, StringSplitOptions.None).ToList();
        List<string> arrShortcutDown = BlindItem.m_strShortcutDown.Split(new string[] { "," }, StringSplitOptions.None).ToList();
        bool bShortcutUp = arrShortcutUp.Count > 0;
        bool bShortcutDown = arrShortcutDown.Count > 0;
        foreach (string strKey in arrShortcutUp)
        {
          bool bFound = false;
          foreach (Pair<String, int> PairItem in g_arrKeyCodes)
          {
            if (PairItem.m_First == strKey.ToUpper())
            {
              if (g_arrKeys.IndexOf(PairItem.m_Second) != -1)
              {
                bFound = true;
                break;
              }
            }
          }
          if (!bFound)
          {
            bShortcutUp = false;
            break;
          }
        }
        foreach (string strKey in arrShortcutDown)
        {
          bool bFound = false;
          foreach (Pair<String, int> PairItem in g_arrKeyCodes)
          {
            if (PairItem.m_First == strKey.ToUpper())
            {
              if (g_arrKeys.IndexOf(PairItem.m_Second) != -1)
              {
                bFound = true;
                break;
              }
            }
          }
          if (!bFound)
          {
            bShortcutDown = false;
            break;
          }
        }
        if (bShortcutUp)
        {
          g_arrWindowBlinds[nBlindIndex].Increment();
        }
        if (bShortcutDown)
        {
          g_arrWindowBlinds[nBlindIndex].Decrement();
        }
      }
    }
  }
}
