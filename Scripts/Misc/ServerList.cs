#region Header
// **********
// RpiUO - ServerList.cs
// Last Edit: 2015/12/24
// Look for Rpi comment
// **********
#endregion

#region References
using System;
using System.IO;
using System.Net;
//using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
#endregion

namespace Server.Misc
{
    public class ServerList
	{
		/* 
        * The default setting for Address, a value of 'null', will use your local IP address. If all of your local IP addresses
        * are private network addresses and AutoDetect is 'true' then ServUO will attempt to discover your public IP address
        * for you automatically.
        *
        * If you do not plan on allowing clients outside of your LAN to connect, you can set AutoDetect to 'false' and leave
        * Address set to 'null'.
        * 
        * If your public IP address cannot be determined, you must change the value of Address to your public IP address
        * manually to allow clients outside of your LAN to connect to your server. Address can be either an IP address or
        * a hostname that will be resolved when ServUO starts.
        * 
        * If you want players outside your LAN to be able to connect to your server and you are behind a router, you must also
        * forward TCP port 2593 to your private IP address. The procedure for doing this varies by manufacturer but generally
        * involves configuration of the router through your web browser.
        *
        * ServerList will direct connecting clients depending on both the address they are connecting from and the address and
        * port they are connecting to. If it is determined that both ends of a connection are private IP addresses, ServerList
        * will direct the client to the local private IP address. If a client is connecting to a local public IP address, they
        * will be directed to whichever address and port they initially connected to. This allows multihomed servers to function
        * properly and fully supports listening on multiple ports. If a client with a public IP address is connecting to a
        * locally private address, the server will direct the client to either the AutoDetected IP address or the manually entered
        * IP address or hostname, whichever is applicable. Loopback clients will be directed to loopback.
        * 
        * If you would like to listen on additional ports (i.e. 22, 23, 80, for clients behind highly restrictive egress
        * firewalls) or specific IP adddresses you can do so by modifying the file SocketOptions.cs found in this directory.
        */

		public static readonly string Address = null;
		public static readonly bool AutoDetect = true;

		public static string ServerName = "My Shard";

		private static IPAddress _PublicAddress;

		private static readonly Regex _AddressPattern = new Regex(@"([0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3})");

		public static void Initialize()
		{
			if (ServerName == "My Shard")
			{
				//ServerName = NameList.RandomName("daemon");
			}

			if (Address == null)
			{
				if (AutoDetect)
				{
					AutoDetection();
				}
			}
			else
			{
				Resolve(Address, out _PublicAddress);
			}

			EventSink.ServerList += EventSink_ServerList;
		}

		private static void EventSink_ServerList(ServerListEventArgs e)
		{
			try
			{
				var ns = e.State;
				var s = ns.Socket;

				var ipep = (IPEndPoint)s.LocalEndPoint;

				var localAddress = ipep.Address;
				var localPort = ipep.Port;

				if (IsPrivateNetwork(localAddress))
				{
					ipep = (IPEndPoint)s.RemoteEndPoint;
					if (!IsPrivateNetwork(ipep.Address) && _PublicAddress != null)
					{
						localAddress = _PublicAddress;
					}
				}

				e.AddServer(ServerName, new IPEndPoint(localAddress, localPort));
			}
			catch
			{
				e.Rejected = true;
			}
		}

		private static void AutoDetection()
		{
			if (!HasPublicIPAddress())
			{
				Console.Write("ServerList: Auto-detecting public IP address...");
				_PublicAddress = FindPublicAddress();

				if (_PublicAddress != null)
				{
					Console.WriteLine("done ({0})", _PublicAddress);
				}
				else
				{
					Console.WriteLine("failed");
				}
			}
		}

		private static void Resolve(string addr, out IPAddress outValue)
		{
			if (IPAddress.TryParse(addr, out outValue))
			{
				return;
			}

			try
			{
				var iphe = Dns.GetHostEntry(addr);

				if (iphe.AddressList.Length > 0)
				{
					outValue = iphe.AddressList[iphe.AddressList.Length - 1];
				}
			}
			catch
			{ }
		}

		private static bool HasPublicIPAddress()
		{
            //Rpi - Replaces the linq code below
            NetworkInterface[] adapters = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface adapter in adapters)
            {
                IPInterfaceProperties properties = adapter.GetIPProperties();

                foreach (IPAddressInformation unicast in properties.UnicastAddresses)
                {
                    IPAddress ip = unicast.Address;

                    if (!IPAddress.IsLoopback(ip) && ip.AddressFamily != AddressFamily.InterNetworkV6 && !IsPrivateNetwork(ip))
                        return true;
                }
            }

            return false;

            //Rpi - We are back to the old version of this function to avoid Linq
            /*
            IEnumerable<IPAddress> ip;
			var uips = adapters.Select(a => a.GetIPProperties())
							   .SelectMany(p => p.UnicastAddresses.Cast<IPAddressInformation>(), (p, u) => u.Address);

			return
				uips.Any(
					ip => !IPAddress.IsLoopback(ip) && ip.AddressFamily != AddressFamily.InterNetworkV6 && !IsPrivateNetwork(ip));
                    */
		}

		private static bool IsPrivateNetwork(IPAddress ip)
		{
			// 10.0.0.0/8
			// 172.16.0.0/12
			// 192.168.0.0/16
			// 169.254.0.0/16
			// 100.64.0.0/10 RFC 6598

			if (ip.AddressFamily == AddressFamily.InterNetworkV6)
			{
				return false;
			}

			if (Utility.IPMatch("192.168.*", ip))
			{
				return true;
			}

			if (Utility.IPMatch("10.*", ip))
			{
				return true;
			}

			if (Utility.IPMatch("172.16-31.*", ip))
			{
				return true;
			}

			if (Utility.IPMatch("169.254.*", ip))
			{
				return true;
			}

			if (Utility.IPMatch("100.64-127.*", ip))
			{
				return true;
			}

			return false;
		}

		public static IPAddress FindPublicAddress()
		{
			string _data = String.Empty;

            try
            {
                //Rpi - Lets try to get the IP two times, from different places
                WebRequest _webRequest1 = WebRequest.Create("http://api.ipify.org");

                using (WebResponse _webResponse = _webRequest1.GetResponse())
                {
                    Stream _stream = _webResponse.GetResponseStream();

                    if (_stream != null)
                    {
                        using (StreamReader _streamReader = new StreamReader(_stream))
                        {
                            _data = _streamReader.ReadToEnd();
                        }
                    }
                }
            }
            catch { }

            Match _match = _AddressPattern.Match(_data);

            try
            {
                if (_match.Success)
                {
                    //The server discovered its public ip
                    return IPAddress.Parse(_match.Value);
                }

                _data = String.Empty;
                WebRequest _webRequest2 = WebRequest.Create("http://checkip.dyndns.org/");

                using (WebResponse _webResponse = _webRequest2.GetResponse())
                {
                    Stream _stream = _webResponse.GetResponseStream();

                    if (_stream != null)
                    {
                        using (StreamReader _streamReader = new StreamReader(_stream))
                        {
                            _data = _streamReader.ReadToEnd();
                        }
                    }
                }
            }
            catch { }

            _match = _AddressPattern.Match(_data);

            if (_match.Success)
            {
                //The server discovered its public ip
                return IPAddress.Parse(_match.Value);
            }
            else
            {
                Console.Write(" Unable to verify using web services... ");
                return null;
            }
		}
	}
}
