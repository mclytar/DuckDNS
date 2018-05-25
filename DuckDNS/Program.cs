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

        public enum ExitCode
        {
            Ok = 0,
            AlreadyDone = 1,
            NotRunning = 2,
            NotAdministrator = 3,
            OperationCancelledByUser = 4,
            Unknown = -1
        }

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
                        if (!IsAdministrator()) Environment.Exit((int)ExitCode.NotAdministrator);

                        try
                        {
                            ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                        }
                        catch (Exception ex)
                        {
                            if (ex.InnerException.GetType() == typeof(Win32Exception))
                            {
                                Win32Exception win32Ex = (Win32Exception)ex.InnerException;
                                switch (win32Ex.NativeErrorCode)
                                {
                                    // The specified service already exists.
                                    case 1073:
                                        Environment.Exit((int)ExitCode.AlreadyDone);
                                        break;
                                }
                            }

                            Environment.Exit((int)ExitCode.Unknown);
                        }
                        return;
                    }

                    if (arg.ToLower() == "--uninstall")
                    {
                        if (!IsAdministrator()) Environment.Exit((int)ExitCode.NotAdministrator);

                        try
                        {
                            ManagedInstallerClass.InstallHelper(new string[] { "/u", Assembly.GetExecutingAssembly().Location });
                        }
                        catch (Exception ex)
                        {
                            if (ex.InnerException.GetType() == typeof(Win32Exception))
                            {
                                Win32Exception win32Ex = (Win32Exception)ex.InnerException;
                                switch (win32Ex.NativeErrorCode)
                                {
                                    // The specified service does not exist as an installed service.
                                    case 1060:
                                        Environment.Exit((int)ExitCode.AlreadyDone);
                                        break;
                                }
                            }

                            Environment.Exit((int)ExitCode.Unknown);
                        }
                        return;
                    }

                    if (arg.ToLower() == "--svc-start")
                    {
                        if (!IsAdministrator()) Environment.Exit((int)ExitCode.NotAdministrator);

                        try
                        {
                            ServiceStart();
                        }
                        catch (Exception ex)
                        {
                            if (ex.InnerException.GetType() == typeof(Win32Exception))
                            {
                                Win32Exception win32Ex = (Win32Exception)ex.InnerException;
                                switch (win32Ex.NativeErrorCode)
                                {
                                    // The specified service is already running.
                                    case 1056:
                                        Environment.Exit((int)ExitCode.AlreadyDone);
                                        break;
                                }
                            }

                            Environment.Exit((int)ExitCode.Unknown);
                        }
                        return;
                    }

                    if (arg.ToLower() == "--svc-stop")
                    {
                        if (!IsAdministrator()) Environment.Exit((int)ExitCode.NotAdministrator);

                        try
                        {
                            ServiceStop();
                        }
                        catch (Exception ex)
                        {
                            if (ex.InnerException.GetType() == typeof(Win32Exception))
                            {
                                Win32Exception win32Ex = (Win32Exception)ex.InnerException;
                                switch (win32Ex.NativeErrorCode)
                                {
                                    // The specified service is not running.
                                    case 1062:
                                        Environment.Exit((int)ExitCode.AlreadyDone);
                                        break;
                                }
                            }

                            Environment.Exit((int)ExitCode.Unknown);
                        }
                        return;
                    }

                    if (arg.ToLower() == "--svc-restart")
                    {
                        if (!IsAdministrator()) Environment.Exit((int)ExitCode.NotAdministrator);

                        try
                        {
                            ServiceRestart();
                        }
                        catch (Exception ex)
                        {
                            if (ex.InnerException.GetType() == typeof(Win32Exception))
                            {
                                Win32Exception win32Ex = (Win32Exception)ex.InnerException;
                                switch (win32Ex.NativeErrorCode)
                                {
                                    // The specified service is not running.
                                    case 1062:
                                        Environment.Exit((int)ExitCode.NotRunning);
                                        break;
                                }
                            }

                            Environment.Exit((int)ExitCode.Unknown);
                        }
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

        private static ExitCode GetExitCode(Process process)
        {
            try
            {
                return (ExitCode)process.ExitCode;
            }
            catch
            {
                return ExitCode.Unknown;
            }
        }

        public static ExitCode RunAsAdministrator(string args)
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
                    return ExitCode.Unknown;

                adminProcess.WaitForExit();
                return GetExitCode(adminProcess);
            }
            catch (Win32Exception e)
            {
                // ERROR_CANCELLED: The operation was cancelled by the user.
                if (e.NativeErrorCode == 1223)
                    return ExitCode.OperationCancelledByUser;

                return ExitCode.Unknown;
            };
        }
    }
}
