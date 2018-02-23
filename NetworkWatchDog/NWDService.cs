using System;
using System.Diagnostics;
using System.ServiceProcess;
using System.Net.NetworkInformation;
using System.Threading;

// TODO : INI FILE PARAMS

namespace NetworkWatchDog
{
    public partial class NWDService : ServiceBase
    {
        // Internet ping timeout
        const int DEFAULT_PING_TIMEOUT = 3000;

        // Internet ping host
        const string DEFAULT_PING_HOST = "www.google.com";

        // Minimal ethernet interface speed
        const int DEFAULT_MINIMAL_INTERFACE_SPEED = 1000;

        // Check interval timeout
        const int DEFAULT_CHECK_TIMEOUT = 5000;

        // Timeout to wait after DHCP renew
        const int DEFAULT_RENEW_WAIT_TIMEOUT = 10000;

        // Timeout to wait after change interface connection state
        const int DEFAULT_STATE_WAIT_TIMEOUT = 10000;

        // Timeout to wait after all restart actions done before new checks
        const int DEFAULT_RESET_WAIT_TIMEOUT = 60000;

        // Thread
        private System.Threading.Thread workThread;

        // Thread process parameter
        public static bool isRunThread = true;

        /// <summary>
        /// Service constructor
        /// </summary>
        public NWDService()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Check is internet connected. 
        /// TODO: Implement ini file settings
        /// </summary>
        /// <returns></returns>
        protected static bool isInternetConnected()
        {
            const int timeout = DEFAULT_PING_TIMEOUT;
            const string host = DEFAULT_PING_HOST;

            var ping = new Ping();
            var buffer = new byte[32];
            var pingOptions = new PingOptions();

            try
            {
                var reply = ping.Send(host, timeout, buffer, pingOptions);
                return (reply != null && reply.Status == IPStatus.Success);
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Check is network connection available
        /// </summary>
        /// <param name="owner">Owner service</param>
        /// <param name="minimumSpeed">Minimum interface speed to check</param>
        /// <returns></returns>
        protected static bool isNetworkAvailable(NWDService owner, long minimumSpeed)
        {
            if (!NetworkInterface.GetIsNetworkAvailable())
                return false;

            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                // discard because of standard reasons
                if ((ni.OperationalStatus != OperationalStatus.Up) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
                    (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                    continue;

                // this allow to filter modems, serial, etc.
                // I use 10000000 as a minimum speed for most cases
                if (ni.Speed < minimumSpeed)
                    continue;

                // discard virtual cards (virtual box, virtual pc, etc.)
                if ((ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
                    (ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0))
                    continue;

                // discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
                if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
                    continue;

                return NWDService.isInternetConnected();
            }
            return false;
        }

        /// <summary>
        /// Process network adapter restart
        /// </summary>
        /// <param name="owner"></param>
        protected static void restartNetworkAdapter(NWDService owner)
        {
            System.Diagnostics.ProcessStartInfo psi;
            System.Diagnostics.Process p;

            owner.EventLog.WriteEntry("Resetting ipconfig", EventLogEntryType.Information);
            psi = new System.Diagnostics.ProcessStartInfo("ipconfig", "/renew");
            p = new System.Diagnostics.Process();
            p.StartInfo = psi;
            p.Start();

            Thread.Sleep(DEFAULT_RENEW_WAIT_TIMEOUT);
            if (!NWDService.isNetworkAvailable(owner, DEFAULT_MINIMAL_INTERFACE_SPEED))
            {
                owner.EventLog.WriteEntry("Network is still unavailable", EventLogEntryType.Warning);
                foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
                {

                    // discard because of standard reasons
                    if ((ni.OperationalStatus != OperationalStatus.Up) ||
                        (ni.NetworkInterfaceType == NetworkInterfaceType.Loopback) ||
                        (ni.NetworkInterfaceType == NetworkInterfaceType.Tunnel))
                        continue;

                    // discard virtual cards (virtual box, virtual pc, etc.)
                    if ((ni.Description.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0) ||
                        (ni.Name.IndexOf("virtual", StringComparison.OrdinalIgnoreCase) >= 0))
                        continue;

                    // discard "Microsoft Loopback Adapter", it will not show as NetworkInterfaceType.Loopback but as Ethernet Card.
                    if (ni.Description.Equals("Microsoft Loopback Adapter", StringComparison.OrdinalIgnoreCase))
                        continue;

                    owner.EventLog.WriteEntry("Resetting network adapter " + ni.Name, EventLogEntryType.Information);
                    psi = new System.Diagnostics.ProcessStartInfo("netsh", "interface set interface \"" + ni.Name + "\" disable");
                    p = new System.Diagnostics.Process();
                    p.StartInfo = psi;
                    p.Start();

                    Thread.Sleep(DEFAULT_STATE_WAIT_TIMEOUT);

                    psi = new System.Diagnostics.ProcessStartInfo("netsh", "interface set interface \"" + ni.Name + "\" enable");
                    p = new System.Diagnostics.Process();
                    p.StartInfo = psi;
                    p.Start();
                }
            }
            Thread.Sleep(DEFAULT_RESET_WAIT_TIMEOUT);
        }

        /// <summary>
        /// Work thread. Check ethernet interfaces for internet access. If its down - restart interface.
        /// </summary>
        /// <param name="owner">Owner service</param>
        protected static void doWork(object owner)
        {
            NWDService ownerObject = owner as NWDService;
            ownerObject.EventLog.WriteEntry("Network watch dog service thread started", EventLogEntryType.Information);
            while (NWDService.isRunThread)
            {
                Thread.Sleep(DEFAULT_CHECK_TIMEOUT);

                if (!NWDService.isNetworkAvailable(ownerObject, DEFAULT_MINIMAL_INTERFACE_SPEED))
                {
                    ownerObject.EventLog.WriteEntry("Network is unavailable", EventLogEntryType.Warning);
                    NWDService.restartNetworkAdapter(ownerObject);
                }
            }
            ownerObject.EventLog.WriteEntry("Network watch dog service thread ended", EventLogEntryType.Information);
        }

        /// <summary>
        /// On service start handler (create work thread)
        /// </summary>
        /// <param name="args"></param>
        protected override void OnStart(string[] args)
        {
            this.EventLog.WriteEntry("Starting network watch dog service...", EventLogEntryType.Information);
            NWDService.isRunThread = false;
            if (workThread == null)
            {
                workThread = new Thread(NWDService.doWork);
            }
            if (workThread.IsAlive)
            {
                workThread.Abort();
            }
            NWDService.isRunThread = true;
            workThread.Start(this);
        }

        /// <summary>
        /// On service stop handler (destory thread)
        /// </summary>
        protected override void OnStop()
        {
            this.EventLog.WriteEntry("Stopping network watch dog service...", EventLogEntryType.Information);
            NWDService.isRunThread = false;
            if (workThread != null)
            {
                if (workThread.IsAlive)
                {
                    workThread.Abort();
                }
            }
        }
    }
}
