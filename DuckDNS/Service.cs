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
            this.ServiceName = "DuckDNS";
            this.EventLog.Log = "Application";

            this.CanHandlePowerEvent = false;
            this.CanHandleSessionChangeEvent = false;
            this.CanPauseAndContinue = false;
            this.CanShutdown = false;
            this.CanStop = true;

            // this.timer.Elapsed += this.Update;
        }

        public void Start()
        {
            this.OnStart(null);
        }

        public void Update(object sender, ElapsedEventArgs e)
        {
            // this.ddns.Update();
        }

        protected override void OnStart(string[] args)
        {
            base.OnStart(args);
            
            // this.timer.Start();
        }

        protected override void OnStop()
        {
            base.OnStop();

            // this.timer.Stop();
        }
    }
}