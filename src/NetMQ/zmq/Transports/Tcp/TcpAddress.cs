/*
    Copyright (c) 2009-2011 250bpm s.r.o.
    Copyright (c) 2007-2009 iMatix Corporation
    Copyright (c) 2007-2011 Other contributors as noted in the AUTHORS file

    This file is part of 0MQ.

    0MQ is free software; you can redistribute it and/or modify it under
    the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation; either version 3 of the License, or
    (at your option) any later version.

    0MQ is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace NetMQ.zmq.Transports.Tcp
{
    /// <summary>
    /// A TcpAddress implements IZAddress, and contains an IPEndPoint (the Address property)
    /// and a Protocol property.
    /// </summary>
    internal class TcpAddress : Address.IZAddress
    {
        public class TcpAddressMask : TcpAddress
        {
            public bool MatchAddress(IPEndPoint addr)
            {
                return Address.Equals(addr);
            }
        }

        public override string ToString()
        {
            if (Address == null)
                return string.Empty;

            var endpoint = Address;

            return endpoint.AddressFamily == AddressFamily.InterNetworkV6 
                ? Protocol + "://[" + endpoint.AddressFamily.ToString() + "]:" + endpoint.Port 
                : Protocol + "://" + endpoint.Address.ToString() + ":" + endpoint.Port;
        }

        public void Resolve(string name, bool ip4Only)
        {
            //  Find the ':' at end that separates address from the port number.
            int delimiter = name.LastIndexOf(':');
            if (delimiter < 0)
                throw new InvalidException(string.Format("TcpAddress.Resolve, delimiter ({0}) must be non-negative.", delimiter));

            //  Separate the address/port.
            string addrStr = name.Substring(0, delimiter);
            string portStr = name.Substring(delimiter + 1);

            //  Remove square brackets around the address, if any.
            if (addrStr.Length >= 2 && addrStr[0] == '[' && addrStr[addrStr.Length - 1] == ']')
                addrStr = addrStr.Substring(1, addrStr.Length - 2);

            int port;
            //  Allow 0 specifically, to detect invalid port error in atoi if not
            if (portStr == "*" || portStr == "0")
            {
                //  Resolve wildcard to 0 to allow auto-selection of port
                port = 0;
            }
            else
            {
                //  Parse the port number (0 is not a valid port).
                port = Convert.ToInt32(portStr);
                if (port == 0)
                {
                    throw new InvalidException(string.Format("TcpAddress.Resolve, port ({0}) must be a valid nonzero integer.", portStr));
                }
            }

            IPAddress ipAddress;

            if (addrStr == "*")
            {
                ipAddress = ip4Only 
                    ? IPAddress.Any 
                    : IPAddress.IPv6Any;
            }            
            else if (!IPAddress.TryParse(addrStr, out ipAddress))
            {
                var availableAddresses = Dns.GetHostEntry(addrStr).AddressList;

                ipAddress = ip4Only 
                    ? availableAddresses.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork) 
                    : Dns.GetHostEntry(addrStr).AddressList.FirstOrDefault(
                        ip => ip.AddressFamily == AddressFamily.InterNetwork ||
                              ip.AddressFamily == AddressFamily.InterNetworkV6);

                if (ipAddress == null)
                    throw new InvalidException(string.Format("TcpAddress.Resolve, unable to find an IP address for {0}", name));
            }

            Address = new IPEndPoint(ipAddress, port);             
        }

        /// <summary>
        /// Get the Address implementation - which here is an IPEndPoint,
        /// which contains Address, AddressFamily, and Port properties.
        /// </summary>
        public IPEndPoint Address { get; private set; }

        /// <summary>
        /// Get the textual-representation of the communication protocol implied by this TcpAddress,
        /// which here is simply "tcp".
        /// </summary>
        public string Protocol
        {
            get { return zmq.Address.TcpProtocol; }
        }
    }
}
