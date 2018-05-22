using System.ComponentModel;
using System.Configuration.Install;
using System.IO;
using System.ServiceProcess;
using System.Timers;

namespace DuckDNS
{
    class DuckDNSService : ServiceBase
    {
        Timer timer;
        DDns ddns;

        public DuckDNSService()
        {
            ServiceName = "DuckDNS";
            EventLog.Log = "Application";

            CanHandlePowerEvent = false;
            CanHandleSessionChangeEvent = false;
            CanPauseAndContinue = false;
            CanShutdown = false;
            CanStop = true;

            ddns = new DDns();

            timer = new Timer();
            timer.Elapsed += Update;
        }

        public void Update(object sender, ElapsedEventArgs e)
        {
            try
            {
                ddns.Update();
            }
            catch { } // Silent error, for now.
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            if (args != null && args.Length > 0)
            {
                ddns.Load(args[0]);
            }
            else
            {
                ddns.Load();
            }

            timer.Interval = ddns.Interval;

            timer.Start();
        }

        protected override void OnStop()
        {
            base.OnStop();

            timer.Stop();
        }
    }
}