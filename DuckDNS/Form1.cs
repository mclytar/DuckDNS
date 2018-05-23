using DuckDNS.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Windows.Forms;

namespace DuckDNS
{
    public partial class Form1 : Form
    {
        private DDns ddns = new DDns();
        private bool allowshowdisplay = false;
        private bool canClose = false;
        private Icon icoTray = Resources.tray;
        private Icon icoTrayC = Resources.tray_checking;

        private Image shieldImage = Windows.GetUACIcon();

        ServiceController svcController = null;

        public Form1()
        {
            InitializeComponent();

            notifyIcon.Icon = Icon;

            if (shieldImage == null) shieldImage = SystemIcons.Shield.ToBitmap();

            installServiceToolStripMenuItem.Image = shieldImage;
            uninstallServiceToolStripMenuItem.Image = shieldImage;
            startServiceToolStripMenuItem.Image = shieldImage;
            stopServiceToolStripMenuItem.Image = shieldImage;

            ddns.Load();

            eventLogToolStripMenuItem.Checked = ddns.EnableLog;

            tbDomain.Text = ddns.Domain;
            tbToken.Text = ddns.Token;
            cbInterval.Text = ddns.ToIntervalString();

            RefreshTimer();

            notifyIcon.Icon = icoTray;
            allowshowdisplay = tbDomain.Text.Length == 0 || tbToken.Text.Length == 0;
            if (!allowshowdisplay)
                UpdateDNS();

            FetchService();

            CheckServiceStatus();
        }

        public void FetchService()
        {
            ServiceController[] services = ServiceController.GetServices();
            foreach (ServiceController service in services)
            {
                if (service.ServiceName == "DuckDNS")
                {
                    svcController = service;
                    return;
                }
            }

            svcController = null;
        }

        public void CheckServiceStatus()
        {
            if (svcController == null)
            {
                installServiceToolStripMenuItem.Visible = true;
                uninstallServiceToolStripMenuItem.Visible = false;
                startServiceToolStripMenuItem.Visible = false;
                stopServiceToolStripMenuItem.Visible = false;
            }
            else
            {
                installServiceToolStripMenuItem.Visible = false;
                uninstallServiceToolStripMenuItem.Visible = true;

                if (svcController.Status == ServiceControllerStatus.Running
                    || svcController.Status == ServiceControllerStatus.StartPending
                    || svcController.Status == ServiceControllerStatus.ContinuePending)
                {
                    startServiceToolStripMenuItem.Visible = false;
                    stopServiceToolStripMenuItem.Visible = true;
                }
                else
                {
                    startServiceToolStripMenuItem.Visible = true;
                    stopServiceToolStripMenuItem.Visible = false;
                }
            }
        }

        protected override void SetVisibleCore(bool value)
        {

            if (!allowshowdisplay)
            {
                allowshowdisplay = true;
                if (!IsHandleCreated && value)
                    CreateHandle();
            }
            else
                base.SetVisibleCore(value);
        }

        private void UpdateDNS()
        {
            try
            {
                notifyIcon.Icon = icoTrayC;

                bool update = ddns.Update();

                lblInfo.Text = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " (" + (update ? "OK" : "FAILED") + ")";

                if (!update)
                {
                    MessageBox.Show("Error updating Duck DNS domain", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    Show();
                }
            }
            finally
            {
                notifyIcon.Icon = icoTray;
            }
        }

        private void RefreshTimer()
        {
            timer.Enabled = false;
            timer.Interval = ddns.Interval;
            timer.Enabled = true;
        }

        private void btOk_Click(object sender, EventArgs e)
        {
            if (!ddns.TryParseInterval(cbInterval.Text))
            {
                MessageBox.Show("Invalid interval format!", "Duck DNS", MessageBoxButtons.OK, MessageBoxIcon.Error);
                cbInterval.BackColor = Color.LightPink;
            }
            else
            {
                cbInterval.BackColor = SystemColors.Window;

                ddns.Domain = tbDomain.Text;
                ddns.Token = tbToken.Text;
                ddns.Save();

                Hide();
                UpdateDNS();
                RefreshTimer();
            }
        }

        private void cbInterval_TextChanged(object sender, EventArgs e)
        {

        }

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            Show();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing && !canClose)
            {
                e.Cancel = true;
                Hide();

                // Reset values (discard)
                tbDomain.Text = ddns.Domain;
                tbToken.Text = ddns.Token;
                cbInterval.Text = ddns.ToIntervalString();
            }
        }

        private void timer_Tick(object sender, EventArgs e)
        {
            UpdateDNS();
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            canClose = true;
            Close();
        }

        private void updateNowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UpdateDNS();
        }

        private void installStartupShortcutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string linkPath = Windows.GetStartupPath() + Path.DirectorySeparatorChar + "DuckDNS.lnk";
            WShellLink.CreateLink(linkPath,"Duck DNS Updater",Assembly.GetExecutingAssembly().Location);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FAbout.Execute();
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            icoTray.Dispose();
            icoTrayC.Dispose();
        }

        private void installServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.RunAsAdministrator("--install"))
            {
                DialogResult result = MessageBox.Show("Service installed successfully!\nDo you want to start the service now?", "DuckDNS update service", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result == DialogResult.Yes)
                {
                    if (Program.RunAsAdministrator("--svc-start"))
                    {
                        MessageBox.Show("Service started successfully!", "DuckDNS update service", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        MessageBox.Show("Unable to start!", "DuckDNS update service", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Unable to install!", "DuckDNS update service", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            FetchService();
            CheckServiceStatus();
        }

        private void uninstallServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.RunAsAdministrator("--uninstall"))
            {
                MessageBox.Show("Service uninstalled successfully!", "DuckDNS update service", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Unable to uninstall!", "DuckDNS update service", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            FetchService();
            CheckServiceStatus();
        }

        private void startServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.RunAsAdministrator("--svc-start"))
            {
                MessageBox.Show("Service started successfully!", "DuckDNS update service", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Unable to start!", "DuckDNS update service", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            FetchService();
            CheckServiceStatus();
        }

        private void stopServiceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Program.RunAsAdministrator("--svc-stop"))
            {
                MessageBox.Show("Service stopped successfully!", "DuckDNS update service", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("Unable to stop!", "DuckDNS update service", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            FetchService();
            CheckServiceStatus();
        }

        private void eventLogToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ddns.EnableLog = !ddns.EnableLog;

            ddns.Save();

            eventLogToolStripMenuItem.Checked = ddns.EnableLog;
        }
    }
}
