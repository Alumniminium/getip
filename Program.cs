using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net.NetworkInformation;
using DnsClient;
using CommandLine;

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
            if (Debugger.IsAttached)
                args = new string[] { "-r" };

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
                if (item.OperationalStatus != OperationalStatus.Up)
                    continue;

                foreach (var ip in item.GetIPProperties().UnicastAddresses)
                {
                    if (ip.Address.AddressFamily != AddressFamily.InterNetwork)
                        continue;

                    _ip = ip.Address.ToString();
                    Console.WriteLine(_ip);
                }
            }
        }
        public static void GetRemoteIPv4()
        {
            if (Options.Verbose)
                Console.WriteLine("Remote IP mode, using CacheFilePath " + CacheFilePath);

            var cacheExpireTime = File.GetLastWriteTime(CacheFilePath).AddMinutes(MaxCacheAgeMinutes);

            if (cacheExpireTime < DateTime.Now || Options.Force)
            {
                if (Options.Verbose)
                    Console.WriteLine("Cache out of date, getting new IP...");

                try
                {
                    _ip = GetRemoteIpUDP();

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
        public static void ResolveIp()
        {
            if (Options.Verbose)
                Console.WriteLine("Trying to find the hostname using local resolving...");

            var entry = Dns.GetHostEntry(Options.Hostname);
            Console.WriteLine("Hostname: " + entry.HostName);
            for (int i = 0; i < entry.AddressList.Length; i++)
                Console.WriteLine($"IP {i}: {entry.AddressList[i]}");
        }
        private static string GetRemoteIpUDP()
        {
            using (var udpSock = new UdpClient("example.com", 80))
            {
                Console.WriteLine(udpSock.Client.LocalEndPoint.ToString());
                return udpSock.Client.LocalEndPoint.ToString();
            }
        }
    }
}