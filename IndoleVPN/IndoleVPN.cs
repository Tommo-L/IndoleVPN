using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;

namespace IndoleVPN
{
    public partial class IndoleVPN : Form
    {
        [DllImport("wininet.dll")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        public const int INTERNET_OPTION_REFRESH = 37;

        Process indoleExe;

        public IndoleVPN()
        {
            InitializeComponent();
            loadConfig();
            this.StartPosition = FormStartPosition.CenterScreen;
        }

        private void loadConfig()
        {
            XElement configXml = XElement.Load(@"config.xml");
            if (configXml == null) return;

            XElement manager = configXml.Element("Manager");
            if (manager == null) return;

            foreach(var e in manager.Elements())
            {
                if(e.Name == "Plugin" && e.Attribute("name").Value.ToString() == "AESEncodePacket")
                {
                    aesKeyTxt.Text = e.Element("HexKey").Value.ToString();
                }
                if (e.Name == "Plugin" && e.Attribute("name").Value.ToString() == "TCPInterface")
                {
                    remoteProxyTxt.Text = e.Element("Address").Value.ToString();
                }
                if (e.Name == "Control")
                {
                    localProxyPortTxt.Text = e.Element("Address").Value.ToString();
                }
            }
        }

        private XElement buildXML()
        {
            XElement configXml = new XElement("Indole",
                                  new XElement("Manager",
                                          new XElement("Plugin",
                                                    new XAttribute("name", "AESEncodePacket"),
                                                    new XElement("HexKey", aesKeyTxt.Text)),

                                          new XElement("Plugin",
                                                    new XAttribute("name", "PacketToStreamWithAES"),
                                                    new XElement("HexKey", aesKeyTxt.Text)),

                                          new XElement("Plugin",
                                                    new XAttribute("name", "TCPInterface"),
                                                    new XElement("Network","tcp"),
                                                    new XElement("Address", remoteProxyTxt.Text)),

                                         new XElement("Plugin",
                                                    new XAttribute("name", "StreamToPacketWithAES"),
                                                    new XElement("HexKey", aesKeyTxt.Text)),

                                          new XElement("Plugin",
                                                    new XAttribute("name", "AESDecodePacket"),
                                                    new XElement("HexKey", aesKeyTxt.Text)),

                                          new XElement("Connection",
                                                    new XAttribute("x", "0"),
                                                    new XAttribute("y", "1"),
                                                    new XAttribute("size", "8192")),

                                          new XElement("Connection",
                                                    new XAttribute("x", "1"),
                                                    new XAttribute("y", "2"),
                                                    new XAttribute("size", "4096")),

                                          new XElement("Connection",
                                                    new XAttribute("x", "2"),
                                                    new XAttribute("y", "3"),
                                                    new XAttribute("size", "4096")),


                                          new XElement("Connection",
                                                    new XAttribute("x", "3"),
                                                    new XAttribute("y", "4"),
                                                    new XAttribute("size", "8192")),

                                          new XElement("Control",
                                                  new XAttribute("name", "TCPControl"),
                                                  new XElement("Network","tcp"),
                                                  new XElement("Address", localProxyPortTxt.Text),
                                                  new XElement("In", "0"),
                                                  new XElement("Out", "4"),
                                                  new XElement("Size", "4096"))));

            var tmp = configXml.ToString();
            Console.WriteLine(configXml.ToString());

            return configXml;
        }

        private void refreshConfig()
        {
            XElement configXml = buildXML();
            configXml.Save(@"config.xml");
        }

        private void button1_Click(object sender, EventArgs e)
        {
            refreshConfig();
            MessageBox.Show("保存成功", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }



        private void ElephantVPN_Resize(object sender, EventArgs e)
        {
            if(this.WindowState == FormWindowState.Minimized)
            {
                this.Visible = false;
                notifyIcon1.Visible = true;
            }
            else
            {
                notifyIcon1.Visible = false;
            }
        }

        private void notifyIcon1_DoubleClick(object sender, EventArgs e)
        {
            showNormalWindow();
        }


        private void ExitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            exitWindow();
        }


        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string remoteProxy = remoteProxyTxt.Text;
            if(remoteProxy == null || remoteProxy.Trim() == "")
            {
                MessageBox.Show("请配置远程代理地址和端口", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            string aesKey = aesKeyTxt.Text;
            if (aesKey == null || aesKey.Trim() == "")
            {
                MessageBox.Show("请配置AES加密key", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            string localProxy = localProxyPortTxt.Text;
            if (localProxy == null || localProxy.Trim() == "")
            {
                MessageBox.Show("请配置本地代理地址和端口", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            refreshConfig();

            if (indoleExe != null)
            {
                try
                {
                    indoleExe.Kill();
                }
                catch (Exception)
                {
                }
                indoleExe.WaitForExit(3000);
            }

            indoleExe = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = @"indole.exe",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true
                }
            };
            indoleExe.Start();
            indoleExe.StandardInput.Write(buildXML().ToString() + "\n");

            RegistryKey intSettings = Registry.CurrentUser.OpenSubKey("SOFTWARE", true).OpenSubKey(@"Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            intSettings.SetValue("ProxyEnable", 1);
            intSettings.SetValue("ProxyServer", localProxyPortTxt.Text);
            intSettings.Flush();
            intSettings.Close();

            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            MessageBox.Show("启动成功", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void CloseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (indoleExe != null)
            {
                try
                {
                    indoleExe.Kill();
                    indoleExe.WaitForExit(3000);
                }
                catch (Exception)
                {
                }
                indoleExe = null;
            }

            RegistryKey intSettings = Registry.CurrentUser.OpenSubKey("SOFTWARE", true).OpenSubKey(@"Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            intSettings.SetValue("ProxyEnable", 0);
            intSettings.Flush();
            intSettings.Close();

            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            MessageBox.Show("关闭成功", this.Text, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }


        private void ConfigToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showNormalWindow();
        }


        private void showNormalWindow()
        {
            this.Visible = true;
            this.WindowState = FormWindowState.Normal;
            this.Activate();
        }

        private void exitWindow()
        {
            if (indoleExe != null)
            {
                try
                {
                    indoleExe.Kill();
                    indoleExe.WaitForExit(3000);
                }
                catch (Exception)
                {
                }
            }

            RegistryKey intSettings = Registry.CurrentUser.OpenSubKey("SOFTWARE", true).OpenSubKey(@"Microsoft\\Windows\\CurrentVersion\\Internet Settings", true);
            intSettings.SetValue("ProxyEnable", 0);
            intSettings.Flush();
            intSettings.Close();

            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);

            Application.Exit();
        }

        private void ElephantVPN_FormClosing(object sender, FormClosingEventArgs e)
        {
            exitWindow();
        }
    }
}
