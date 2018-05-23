using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;

namespace DuckDNS
{
    class DDns
    {
        public string Filename;
        public bool EnableLog = false;
        public string LogFilename;
        public string Domain;
        public string Token;
        public int Interval;
        private WebClient cli = new WebClient();

        public DDns()
        {
            Filename = "DuckDNS.cfg";
            LogFilename = "DuckDNS.log";
            Interval = 1800000;
        }

        public DDns(string filename)
        {
            Filename = filename;
            LogFilename = Path.GetDirectoryName(filename) + "\\" + Path.GetFileNameWithoutExtension(filename) + ".log";
            Interval = 1800000;
        }

        public DDns(string filename, string logFilename)
        {
            Filename = filename;
            LogFilename = logFilename;
            Interval = 1800000;
        }

        public void ParseInterval(string value)
        {
            int parsed = 0;

            if (value.Length == 0 || !int.TryParse(value.Substring(0, value.Length - 1), out parsed))
            {
                throw new FormatException("Duration format not recognized.");
            }
            else
            {
                switch (value[value.Length - 1])
                {
                    case 's':
                        parsed *= 1000;
                        break;
                    case 'm':
                        parsed *= 60000;
                        break;
                    case 'h':
                        parsed *= 3600000;
                        break;
                    case 'd':
                        parsed *= 86400000;
                        break;
                    default:
                        throw new FormatException("Duration format not recognized.");
                }
            }

            if (parsed == 0) throw new InvalidDataException("Duration can't be zero.");

            this.Interval = parsed;
        }

        public bool TryParseInterval(string value)
        {
            int parsed = 0;

            if (value.Length == 0 || !int.TryParse(value.Substring(0, value.Length - 1), out parsed))
            {
                return false;
            }
            else
            {
                switch (value[value.Length - 1])
                {
                    case 's':
                        parsed *= 1000;
                        break;
                    case 'm':
                        parsed *= 60000;
                        break;
                    case 'h':
                        parsed *= 3600000;
                        break;
                    case 'd':
                        parsed *= 86400000;
                        break;
                    default:
                        return false;
                }
            }

            if (parsed == 0) return false;

            Interval = parsed;
            return true;
        }

        public string ToIntervalString()
        {
            string suffix = "";
            int divider;

            if (Interval % 86400000 == 0)
            {
                suffix = "d";
                divider = 86400000;
            }
            else if (Interval % 3600000 == 0)
            {
                suffix = "h";
                divider = 3600000;
            }
            else if (Interval % 60000 == 0)
            {
                suffix = "m";
                divider = 60000;
            }
            else
            {
                suffix = "s";
                divider = 1000;
            }

            return (Interval / divider).ToString() + suffix;
        }

        public bool Update()
        {
            string url = "https://www.duckdns.org/update?domains=" + Domain + "&token=" + Token;
            string data = cli.DownloadString(url);
            bool result = data == "OK";

            if (result) Log("Domain name updated successfully.");
            else Log("Error updating domain name.");

            return result;
        }

        public void Load()
        {
            string[] data = null;

            if (File.Exists(Filename))
            {
                try
                {
                    data = File.ReadAllLines(Filename);
                    Log("Configuration file loaded.");
                }
                catch
                {
                    Log("Can't load configuration file.");
                };
            }

            Domain = data != null && data.Length > 0 ? data[0] : "";
            Token = data != null && data.Length > 1 ? CharSwitch(data[1]) : "";

            if (!TryParseInterval(data != null && data.Length > 2 ? data[2] : "30m"))
            {
                Interval = 1800000;
            }

            EnableLog = data != null && data.Length > 3 && data[3] == "log";
        }

        public void Save()
        {
            string[] data = { Domain, CharSwitch(Token), ToIntervalString(), EnableLog ? "log" : "" };
            try
            {
                File.WriteAllLines(Filename, data);
                Log("Configuration file saved.");
            }
            catch
            {
                Log("Can't save configuration file.");
            };
        }

        private string CharSwitch(string str)
        {
            // Super basic, but more than nothing
            string a = "abcdef0123456789";
            string b = "f9031ace7d86524b";
            StringBuilder sb = new StringBuilder(str);
            for (int i = 0; i < sb.Length; i++)
            {
                int chi = a.IndexOf(sb[i]);
                if (chi >= 0)
                    sb[i] = b[chi];
            }
            return sb.ToString();
        }

        public void Log(string str)
        {
            if (EnableLog) File.AppendAllText(LogFilename, "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + str + "\n");
        }
    }
}
