using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;

using PacketDotNet;
using PacketDotNet.Lldp;

using SharpPcap;
using SharpPcap.LibPcap;

using AddressFamily = System.Net.Sockets.AddressFamily;

namespace LLDPConsoleApp
{
    class Program
    {
        public static readonly PhysicalAddress LldpDestinationMacAddress = new PhysicalAddress(
            new byte[] { 0x01, 0x80, 0xc2, 0x00, 0x00, 0x0e }
        );
        static void Main(string[] args)
        {

            List<NetworkInterface> adapters = NetworkInterface.GetAllNetworkInterfaces()
            .Where(
                x =>

                        // Link is up
                        x.OperationalStatus == OperationalStatus.Up

                        // Not loopback (127.0.0.1 / ::1)
                        && x.NetworkInterfaceType != NetworkInterfaceType.Loopback

                        // Not tunnel
                        && x.NetworkInterfaceType != NetworkInterfaceType.Tunnel

                        // Supports IPv4 or IPv6
                        && (x.Supports(NetworkInterfaceComponent.IPv4) || x.Supports(NetworkInterfaceComponent.IPv6)))
            .ToList();

            CaptureDeviceList devices = CaptureDeviceList.Instance;

            foreach (NetworkInterface adapter in adapters)
            {
                Console.WriteLine(adapter.Name);

                LldpPacket test = new LldpPacket();

                test.TlvCollection.Add(new ChassisIdTlv(ChassisSubType.MacAddress, adapter.GetPhysicalAddress()));

                test.TlvCollection.Add(new PortIdTlv(PortSubType.LocallyAssigned, Encoding.UTF8.GetBytes(adapter.Name)));

                test.TlvCollection.Add(new TimeToLiveTlv(120));

                test.TlvCollection.Add(new SystemNameTlv(Environment.MachineName));

                test.TlvCollection.Add(new EndOfLldpduTlv());

                Packet testPacket = new EthernetPacket(adapter.GetPhysicalAddress(), LldpDestinationMacAddress, EthernetType.Lldp)
                {
                    PayloadData = test.Bytes
                };

                LibPcapLiveDevice nic = devices.FirstOrDefault(device => device.Name.ToLower().Contains(adapter.Id.ToLower())) as LibPcapLiveDevice;

                nic.Open(DeviceModes.Promiscuous, 500);

                if (nic.Opened)
                {
                    nic.SendPacket(testPacket);
                    Console.WriteLine("LLDP Packet Sent via \"" + adapter.Name + "\"");
                    nic.Close();
                }
                else
                {
                    Console.WriteLine("NOT SENT!");
                }


            }

            

            


        }
    }
}
