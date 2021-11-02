using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;

namespace vivego.core
{
	public static class PortUtils
	{
		public static int FindAvailablePortIncrementally(int port)
		{
			int portAvailable = Enumerable
				.Range(port, 1000)
				.FirstOrDefault(actualPort => new IPEndPoint(IPAddress.Loopback, actualPort).IsAvailable());
			return portAvailable;
		}

		public static bool IsAvailable(this IPEndPoint endPoint)
		{
			if (endPoint is null) throw new ArgumentNullException(nameof(endPoint));
			// Evaluate current system tcp connections. This is the same information provided
			// by the netstat command line application, just in .Net strongly-typed object
			// form.  We will look through the list, and if our port we would like to use
			// in our TcpClient is occupied, we will set isAvailable to false.
			IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
			IPEndPoint[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpListeners();
			foreach (IPEndPoint tcpi in tcpConnInfoArray)
			{
				if (tcpi.Port.Equals(endPoint.Port))
				{
					return false;
				}
			}

			return true;
		}

		public static int FindAvailablePort()
		{
			using Mutex mutex = new(false, "PortUtils.FindAvailablePort");
			try
			{
				mutex.WaitOne();
				IPEndPoint endPoint = new(IPAddress.Loopback, 0);
				using Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				socket.Bind(endPoint);
				if (socket.LocalEndPoint is IPEndPoint ipEndPoint)
				{
					return ipEndPoint.Port;
				}

				return -1;
			}
			finally
			{
				mutex.ReleaseMutex();
			}
		}

		public static IEnumerable<string> GetIpAddresses()
		{
			// Get a list of all network interfaces (usually one per network card, dialup, and VPN connection)     
			NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface network in networkInterfaces)
			{
				if (network.OperationalStatus == OperationalStatus.Up)
				{
					if (network.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
					{
						continue;
					}

					if (network.NetworkInterfaceType == NetworkInterfaceType.Tunnel)
					{
						continue;
					}

					// GatewayIPAddressInformationCollection GATE = network.GetIPProperties().GatewayAddresses;
					// Read the IP configuration for each network   
					IPInterfaceProperties properties = network.GetIPProperties();

					// discard those who do not have a real gateaway 
					foreach (GatewayIPAddressInformation gInfo in properties.GatewayAddresses)
					{
						// not a true gateaway (VmWare Lan)
						if (gInfo.Address.ToString().Equals("0.0.0.0", StringComparison.Ordinal))
						{
							continue;
						}

						foreach (UnicastIPAddressInformation unicastIpAddressInformation in properties.UnicastAddresses)
						{
							if (unicastIpAddressInformation.Address.AddressFamily == AddressFamily.InterNetwork)
							{
								yield return unicastIpAddressInformation.Address.ToString();
							}
						}

						break;
					}
				}
			}
		}
	}
}