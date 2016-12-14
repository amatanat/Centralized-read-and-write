using System;
using System.Threading;

namespace Host
{
    public class Utils
    {
        
        
        /// <summary>
        /// This method returns IP:PORT.
        /// </summary>
        /// <param name="IP">IP address that user enters</param>
        /// <param name="port">PORT that user enters</param>
        /// <returns>IP:PORT</returns>
        public static string toSocketAddress(string IP, int port)
        {
            return IP + ":" + port;
        }

        
        /// <summary>
        /// This method splits entered socket address into IP and PORT.Then gets IP.
        /// </summary>
        /// <param name="socketAddress">IP:PORT that user enters</param>
        /// <returns>IP from entered address</returns>
        public static string getIPFromSocketAddress(string socketAddress)
        {
            string[] addressComponents = socketAddress.Split(':');
            string IP = addressComponents[0];
            return IP;
        }

        /// <summary>
        /// This method splits socket address into IP and PORT.Then gets PORT.
        /// </summary>
        /// <param name="socketAddress">IP:PORT that user enters</param>
        /// <returns>PORT from entered address</returns>
        //split the socket address and returns port
        public static int getPortFromSocketAddress(string socketAddress)
        {
            string[] addressComponents = socketAddress.Split(':');
            int port = int.Parse(addressComponents[1]);
            return port;
        }

        /// <summary>
        /// This method checks if entered IP address is a valid IPV4 address or not.
        /// </summary>
        /// <param name="socketAddress">IP address that user enters</param>
        /// <returns>a bool value</returns>
        public static bool isValidSocketAddress(string socketAddress)
        { 
            bool status = true; 
            string[] part1 = socketAddress.Split(':');
            string[] parts = socketAddress.Split('.');
            if (parts.Length < 4)
            {
                Console.WriteLine("Not a IPv4 address. Please enter correct address: ");
                status = false;
            }
            else
            {
                foreach (string part in parts)
                {
                    byte checkPart = 0;
                    if (!byte.TryParse(part, out checkPart))
                    {
                        Console.WriteLine("Not a valid IPv4 address. Please enter correct address: ");
                        status = false;
                    }
                }
                status = true;
             }
            return status; 
                       
        }


        /// <summary>
        /// Method that generate  random socket address for client
        /// </summary>
        /// <returns>Socket address for client</returns>
        public static string getDefaultSocketAddress()
        {
            return "localhost" + ":" + 8080;
        }

        /// <summary>
        /// Method that generate  random ip address for  client
        /// </summary>
        /// <returns></returns>
        public static string getDefaultIP()
        {
            return "localhost";
        }

        /// <summary>
        /// for logging.
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string logTimestamp(int time)
        {
            int threadID = Thread.CurrentThread.ManagedThreadId;
            return "Thread" + threadID + "with timemstamp = " + time + " tacks: ";
        }


    }
}
