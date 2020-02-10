using System.Text;
using CommandLine;

namespace IpUtil
{
    public class Options
    {
        [Option('v', "verbose", Required = false, HelpText = "Set output to verbose.")]
        public bool Verbose { get; set; }

        [Option("help", Required = false, HelpText = "Displays help.")]
        public bool ShowHelp { get; set; }

        [Option('R', "resolve", Required = false, HelpText = "hostname to resolve.")]
        public string Hostname { get; set; }
        
        [Option('h', "hostname", Required = false, HelpText = "hostname to resolve.")]
        public string IP { get; set; }

        [Option('r', "remote", Required = false, HelpText = "Get remote IP")]
        public bool RemoteIp { get; set; }

        [Option('l', "local", Required = false, HelpText = "Get local IP")]
        public bool LocalIp { get; set; }
        [Option('f', "force", Required = false, HelpText = "ignores the cache")]
        public bool Force { get; set; }
        public static string DisplayHelp()
        {
            var sb = new StringBuilder();
            sb.AppendLine("getip [options]");
            sb.AppendLine("ex. getip -r -h -a");
            sb.AppendLine("[Options]");
            sb.AppendLine("  -l | --local         get local ip");
            sb.AppendLine("  -r | --remote        get public ip");
            sb.AppendLine("  -h | --hostname      resolve hostnames from ip");
            sb.AppendLine("  -v | --verbose       verbose mode");
            sb.AppendLine("  -f | --force   bypasses cache file and queries hardware / servers again");
            sb.AppendLine("Parameterized Options:");
            sb.AppendLine("  -R [domain] | --resolve [domain]  returns IP of domain.");
            return sb.ToString();
        }
    }
}