using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Diagnostics;
using System.Threading.Tasks;
using DnsClient;
using CommandLine;
using CommandLine.Text;

namespace IpUtil
{
    internal static class Program
    {
        private static Options Options;
        private static readonly string CacheFilePath = "/tmp/getip.cache";
        private static string _ip = "127.0.0.1";
        public static double MaxCacheAgeMinutes { get; private set; } = 60 * 12;

        static void Main(string[] args)
        {
            if(Debugger.IsAttached)
                args = new string[]{"-r"};
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
                   {
                       Options = o;
                       if (o.LocalIp)
                           GetLocalIPv4();
                       if (o.RemoteIp)
                           GetRemoteIPv4();
                       if (!string.IsNullOrWhiteSpace(o.Hostname))
                           ResolveIp();
                       if (!string.IsNullOrEmpty(o.IP))
                           GetHostName();
                   });
        }

        public static void GetLocalIPv4()
        {
            foreach (var item in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (item.OperationalStatus == OperationalStatus.Up)
                {
                    foreach (var ip in item.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily != AddressFamily.InterNetwork)
                            continue;

                        _ip = ip.Address.ToString();
                        Console.WriteLine(_ip);
                    }
                }
            }
        }

        [Command(Args = "-r", Alternative = "--remote")]
        public static void GetRemoteIPv4()
        {
            GetRemoteIpUDP();
            if (Options.Verbose)
                Console.WriteLine("Remote IP mode, using CacheFilePath " + CacheFilePath);
            if (Options.Verbose)
                Console.WriteLine("Setting IP to " + _ip + " until response..");


            if(Options.Verbose)
            {
                Console.WriteLine();
            }            

            if (File.GetLastWriteTime(CacheFilePath).AddMinutes(MaxCacheAgeMinutes) < DateTime.Now || Options.Force)
            {
                try
                {
                    if (Options.Verbose)
                        Console.WriteLine("Cache out of date, getting new IP...");
                    _ip = GetRemoteIpInHouse();
                    //new HttpClient().GetStringAsync("https://wtfismyip.com/text").Result.Trim();
                    if (Options.Verbose)
                        Console.WriteLine("Got new IP (" + _ip + "), writing to cache..");
                    File.WriteAllText(CacheFilePath, _ip);
                    if (Options.Verbose)
                        Console.WriteLine("Cache file updated.");
                }
                catch (Exception e)
                {
                    if (Options.Verbose)
                        Console.WriteLine(e);
                }
            }
            else
            {
                if (Options.Verbose)
                    Console.WriteLine("CacheFilePath age < " + MaxCacheAgeMinutes + "min trying to get cached ip..");
                _ip = File.ReadAllText(CacheFilePath).Trim();
                if (Options.Verbose)
                    Console.WriteLine("Cache file contains " + _ip);
            }
            Console.WriteLine(_ip);
        }

        public static void GetHostName()
        {
            if (Options.Verbose)
                Console.WriteLine("Trying to find the hostname using local resolving...");

            var entry = Dns.GetHostEntry(Options.IP);
            if (!string.IsNullOrEmpty(entry.HostName) && entry.HostName != Options.IP)
            {
                Console.WriteLine(entry.HostName);
                return;
            }

            if (Options.Verbose)
            {
                Console.WriteLine("Local resolving failed!");
                Console.WriteLine("Trying to get hostname from dns servers...");
            }

            var client = new LookupClient();

            var hostName = client.GetHostName(IPAddress.Parse(Options.IP));
            if (string.IsNullOrEmpty(hostName) || hostName == Options.IP)
            {
                Console.WriteLine("No hostname found");
                return;
            }

            Console.WriteLine(hostName);
        }

        [Command(Args = "-R", Alternative = "--resolve")]
        public static void ResolveIp()
        {
            if (Options.Verbose)
                Console.WriteLine("Trying to find the hostname using local resolving...");

            var entry = Dns.GetHostEntry(Options.Hostname);
            if (entry.AddressList.Count() > 0)
            {
                Console.WriteLine(entry.AddressList[0]);
            }
        }
        private static string GetRemoteIpUDP()
        {
            using(var udpSock = new UdpClient("example.com",80))
            {
                Console.WriteLine(udpSock.Client.LocalEndPoint.ToString());
                return udpSock.Client.LocalEndPoint.ToString();
            }
        }
        private static string GetRemoteIpInHouse()
        {
            if (Options.Verbose)
                Console.WriteLine("Trying to get external IP using our in-house api...");
            var buffer = BitConverter.GetBytes(2130706433); // 127.0.0.1

            using (TcpClient c = new TcpClient())
            {
                if (c.ConnectAsync("89.163.230.146", 65535).Wait(1000))
                {
                    c.ReceiveTimeout = 500;
                    if (Options.Verbose)
                        Console.WriteLine("Connected to api!");
                    c.Client.Receive(buffer);
                    if (Options.Verbose)
                        Console.WriteLine("Received 4 bytes...");
                }
            }

            var ip = new IPAddress(BitConverter.ToUInt32(buffer, 0));

            return ip.ToString();
        }
    }
}