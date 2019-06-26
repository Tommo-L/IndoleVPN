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

            XElement tcpaes = configXml.Element("tcpaes");
            if (tcpaes == null) return;

            localProxyPortTxt.Text = tcpaes.Attribute("address").Value.ToString();

            XElement encode = tcpaes.Element("encode");
            if (encode != null)
            {
                XElement aesenc = encode.Element("aesenc");
                aesKeyTxt.Text = aesenc.Attribute("hex_key").Value.ToString();
            }

            XElement decode = tcpaes.Element("decode");
            if (decode != null)
            {
                XElement aesdec = decode.Element("aesdec");
                aesKeyTxt.Text = aesdec.Attribute("hex_key").Value.ToString();
            }

            XElement tcp = tcpaes.Element("tcp");
            if (tcp == null) return;

            remoteProxyTxt.Text = tcp.Attribute("address").Value.ToString();
        }

        private XElement buildXML()
        {
            XElement configXml = new XElement("indole",
                                  new XElement("tcpaes",
                                          new XAttribute("network", "tcp"),
                                          new XAttribute("address", localProxyPortTxt.Text),
                                          new XAttribute("bufsize", "1024"),

                                          new XElement("encode",
                                                      new XElement("aesenc",
                                                                  new XAttribute("queue_size", "1024"),
                                                                  new XAttribute("hex_key", aesKeyTxt.Text))),
                                          new XElement("decode",
                                                  new XElement("aesdec",
                                                                  new XAttribute("queue_size", "1024"),
                                                                  new XAttribute("hex_key", aesKeyTxt.Text),
                                                                  new XAttribute("buf_size", "65536"))),
                                          new XElement("tcp",
                                                  new XAttribute("network", "tcp"),
                                                  new XAttribute("address", remoteProxyTxt.Text))));
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
