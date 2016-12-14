using HelloService;
using Microsoft.Samples.XmlRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;

namespace Host
{
    public class Client
    {
        private string socketAddress;
        private bool status;
        private int ID = 0;
        private int priority = 0;
       // private string masterString = "";
        private bool isMasterNode = false;
        private List<string> addressTable = new List<string>();
        private string coordinatorIP;

        /// <summary>
        /// Contains IP address, and node. 
        /// </summary>
        public Dictionary<string,IRequestProcessor> nodeList = new Dictionary<string,IRequestProcessor>();

        /// <summary>
        /// Contains ID and IP address.
        /// </summary>
        public Dictionary<int, string> clientListWithID = new Dictionary<int, string>();

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="ownSocketAddress">Socket Address that user enters</param>
        public Client(string ownSocketAddress)
        {

            //this(Utils.toSocketAddress("localhost", 8080), ownSocketAddress);
             new Client(Utils.toSocketAddress("localhost", 8080), ownSocketAddress);
        }



        /// <summary>
        /// Constructor method
        /// </summary>
        /// <param name="serverSocketAddress">Server's socket address</param>
        /// <param name="ownSocketAddress">Host's socket address</param>
        public Client(string serverSocketAddress,string ownSocketAddress)
        {
            status = true;
            socketAddress = ownSocketAddress;
            ChannelFactory<IRequestProcessor> clientAPIFactory = new ChannelFactory<IRequestProcessor>(new WebHttpBinding(WebHttpSecurityMode.None), "http://" + serverSocketAddress + "/xmlrpc");
            clientAPIFactory.Endpoint.Behaviors.Add(new XmlRpcEndpointBehavior());
            IRequestProcessor node = clientAPIFactory.CreateChannel();
            nodeList.Add(Utils.getIPFromSocketAddress(ownSocketAddress),node);
        }


        /// <summary>
        /// When new node joins to network, this node will have new assinged ID. 
        /// </summary>
        /// <returns>ID for new joined node</returns>
        public int getID()
        {
            return ID ++;
        }

        /// <summary>
        /// This method returns socket address of client.
        /// </summary>
        /// <returns>Socket address of client</returns>
        public string getSocketAddress()
        {
            return socketAddress;
        }

        /// <summary>
        /// This method gets IP address from IP:PORT.
        /// </summary>
        /// <returns>IP address</returns>
        public string getIP()
        {
            return Utils.getIPFromSocketAddress(socketAddress);

        }

        /// <summary>
        /// This method will set the priority for node.
        /// </summary>
        /// <param name="priority"></param>
        public void setPriority(int priority)
        {
            this.priority = priority;
        }

        /// <summary>
        /// Getter method for priority
        /// </summary>
        /// <returns>Priority</returns>
        public int getPriority()
        {
            return priority;
        }

        /// <summary>
        /// Gets the status of client.
        /// </summary>
        /// <returns>The status of client.</returns>
        public bool isStatus()
        {
            return status;
        }

        
        /// <summary>
        /// Sets this node as a master node.
        /// </summary>
        /// <param name="isMasterNode"></param>
        public void setMasterNode(bool isMasterNode)
        {
            this.isMasterNode = isMasterNode;
        }

        /// <summary>
        /// This method returns is node a master node.
        /// </summary>
        /// <returns>Is node a master node</returns>
        public bool MasterNode()
        {
            return isMasterNode;
        }

        /// <summary>
        /// Updates address table with the ip addresses of the hosts in the network
        /// </summary>
        public void updateAddressTable()
        {
            List<string> network = Server.getInstance().getNetwork();
            addressTable.Clear();
            foreach (string IP in network.Skip(1).Take(network.Count))   //network.GetRange(0, network.Count))
            {
                addressTable.Add(IP);
            }
            /*
            Console.WriteLine("Address table of hosts in the network:");
            foreach(string a in addressTable)
            {
                Console.WriteLine(a);
            }*/
            
        }
        

        /// <summary>
        /// This method is for making each node in the network to know about the coordinator ip
        /// </summary>
        public void updateCoordinator()
        {
            if (Server.getInstance().getMasterNode() != null)
            {
                coordinatorIP = Server.getInstance().getMasterNode().getIP();
            } 
            else
            {
                Console.WriteLine("Master node has not been assigned yet, so coordinator ip is null");
                coordinatorIP = null;
            } 

        }


    }
}
