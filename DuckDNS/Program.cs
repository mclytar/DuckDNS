using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Principal;
using System.ServiceProcess;
using System.Threading;
using System.Windows.Forms;

namespace DuckDNS
{
    [RunInstaller(true)]
    public class Program : Installer
    {
        public Program()
        {
            ServiceProcessInstaller svcProcInstaller = new ServiceProcessInstaller();
            ServiceInstaller svcInstaller = new ServiceInstaller();

            svcProcInstaller.Account = ServiceAccount.LocalSystem;

            svcInstaller.ServiceName = "DuckDNS";
            svcInstaller.DisplayName = "DuckDNS update service";
            svcInstaller.StartType = ServiceStartMode.Automatic;

            this.Installers.Add(svcProcInstaller);
            this.Installers.Add(svcInstaller);
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            if (Environment.UserInteractive)
            {
                string[] argv = Environment.GetCommandLineArgs();

                foreach (string arg in argv)
                {
                    if (arg.ToLower() == "--install")
                    {
                        ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        return;
                    }

                    if (arg.ToLower() == "--uninstall")
                    {
                        ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        return;
                    }

                    if (arg.ToLower() == "--svc-start")
                    {
                        ServiceStart();
                        return;
                    }

                    if (arg.ToLower() == "--svc-stop")
                    {
                        ServiceStop();
                        return;
                    }

                    if (arg.ToLower() == "--svc-restart")
                    {
                        ServiceRestart();
                        return;
                    }
                }

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new Form1());
            }
            else
            {
                ServiceBase.Run(new DuckDNSService());
            }
        }

        public static void ServiceStart()
        {
            ServiceController service = new ServiceController("DuckDNS");

            service.Start(new string[] { Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\DuckDNS.cfg" });
            service.WaitForStatus(ServiceControllerStatus.Running);
        }

        public static void ServiceRestart()
        {
            ServiceController service = new ServiceController("DuckDNS");

            service.Stop();
            service.Refresh();
            service.WaitForStatus(ServiceControllerStatus.Stopped);

            service.Start(new string[] { Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + @"\DuckDNS.cfg" });
            service.Refresh();
            service.WaitForStatus(ServiceControllerStatus.Running);
        }

        public static void ServiceStop()
        {
            ServiceController service = new ServiceController("DuckDNS");

            service.Stop();
            service.WaitForStatus(ServiceControllerStatus.Stopped);
        }

        public static bool IsAdministrator()
        {
            WindowsIdentity identity = WindowsIdentity.GetCurrent();

            WindowsPrincipal principal = new WindowsPrincipal(identity);

            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static bool RunAsAdministrator(string args)
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process adminProcess = new Process();

            // Acquire current process data.
            adminProcess.StartInfo = currentProcess.StartInfo;
            adminProcess.StartInfo.FileName = currentProcess.MainModule.FileName;
            adminProcess.StartInfo.WorkingDirectory = Path.GetDirectoryName(currentProcess.MainModule.FileName);

            // Set new process data.
            adminProcess.StartInfo.UseShellExecute = true;
            adminProcess.StartInfo.Verb = "runas";
            adminProcess.StartInfo.Arguments = args;

            try
            {
                if (!adminProcess.Start())
                    return false;
            }
            catch (Win32Exception e)
            {
                // ERROR_CANCELLED: The operation was cancelled by the user.
                if (e.NativeErrorCode == 1223)
                    return false;
                throw;
            };
            return true;
        }
    }
}
