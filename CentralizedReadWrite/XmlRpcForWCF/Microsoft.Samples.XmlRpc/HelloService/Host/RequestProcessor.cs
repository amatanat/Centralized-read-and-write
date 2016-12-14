using HelloService;
using System;
using System.Text;

namespace Host
{
    public class RequestProcessor : IRequestProcessor
    {
        private int[] clientsPriorities;
        private bool[] clientsStatuses;
        private int coordinatorNumber;
        private int numberOfClients;
        private string resultStringToBeReturned;



        /// <summary>
        /// This method does distributed join in the network.
        /// </summary>
        /// <param name="nodeSocketAddress">Socket address of node</param>
        /// <returns></returns>
        public string joinNode(string nodeSocketAddress)
        {

            StringBuilder result = new StringBuilder("");
            result.Append(Utils.logTimestamp(0) + nodeSocketAddress + " wants to join the network"+ "\n");
            Server.getInstance().addHost(nodeSocketAddress);
        //    Console.WriteLine("Node added to the network list");
            Server.getInstance().updateClientList(nodeSocketAddress);
          //  Console.WriteLine("clients list has updated");
            Server.getInstance().resetNodesForCoordinatorElection();
            Server.getInstance().enumerateClientsPriorities();
         //   Console.WriteLine("Client's priorities has setup");
            result.Append(Utils.logTimestamp(0) + "\n");
            Array.ForEach(Server.getInstance().getNodeArray(), s => result.AppendLine(s)); 
            return result.ToString();
          
        }


        /// <summary>
        /// This method propagates join request.
        /// </summary>
        /// <param name="nodeSocketAddress">Socket address of node</param>
        /// <returns></returns>
        public string propagateJoinRequest(string nodeSocketAddress)
        {
            StringBuilder result = new StringBuilder("");
            foreach (Client client in Server.getInstance().getClients())
            {
                client.updateAddressTable();
            }
            result.Append("New host is joined to network. Hosts in network:" + "\n");
            Array.ForEach(Server.getInstance().getNodeArray(), s => result.AppendLine(s));
            result.Append("Join message propagated and address table of each client updated" + "\n");
            return result.ToString();
        }


        /// <summary>
        /// Master node election.
        /// </summary>
        /// <param name="actionStarterIPBundle"></param>
        /// <returns></returns>
        public string startElection(string actionStarterIPBundle)
        {
            resultStringToBeReturned = "";

            Client starterClient = null;

            //split the string 
            string[] components = actionStarterIPBundle.Split(',');
            string action = components[0];
            string starterIP = components[1];

            try
            {
                // we will handle master election in two cases.
                if (action.Equals("join", StringComparison.InvariantCultureIgnoreCase))
                {
                    starterClient = Server.getInstance().getClientByIP(starterIP);
                }
                else if (action.Equals("signOff", StringComparison.InvariantCultureIgnoreCase))
                {
                    starterIP = Server.getInstance().getFirstRemainingClient().getIP();
                    starterClient = Server.getInstance().getClientByIP(starterIP);
                }
            }
            catch (ArgumentOutOfRangeException ex)
            {

                resultStringToBeReturned += "There is no client in the network "
                        + "please join at least one" + "\n";
            }
            
            clientsPriorities = new int[Server.getInstance().getClients().Count];
            clientsStatuses = new bool[Server.getInstance().getClients().Count];
            numberOfClients = Server.getInstance().getClients().Count;

            int index = 0;
            //now assign clientsPriorities and clientStatuses
            foreach (Client client in Server.getInstance().getClients())
            {
                clientsPriorities[index] = client.getPriority();
                clientsStatuses[index] = client.isStatus();
                index++;
            }

            if (starterClient != null)
            {
                //priority is the same as the index + 1 of client in 'clients' list
                elect(starterClient.getPriority());

                //set this coordinator as master node
                //coordinator number starts from 1, so we need index it to start from zero
                Client masterClient = Server.getInstance().getClients()[coordinatorNumber - 1]; // CHECK - done!
                masterClient.setMasterNode(true);
                resultStringToBeReturned += "Final coordinator is node " + coordinatorNumber + " with IP " + masterClient.getIP() + "\n";
                return resultStringToBeReturned;
            }
            else
            {
                resultStringToBeReturned += "There is no starter client with given"
                        + " properties to start master election" + "\n";


                return resultStringToBeReturned;
            }
        }

        /// <summary>
        /// This method implements Bully algorithm.
        /// </summary>
        /// <param name="elector"></param>
        private void elect(int elector)
        {
            elector = elector - 1;
            coordinatorNumber = elector + 1;

            for (int i = 0; i < numberOfClients; i++)
            {
                if (clientsPriorities[elector] < clientsPriorities[i])
                {
                    resultStringToBeReturned += "Election message is sent from node " + (elector + 1) + " to node " + (i + 1) + "\n";
                    if (clientsStatuses[i])
                        elect(i + 1);
                }
            }
        }

        /// <summary>
        /// Method to propagate coordinator message on the network.
        /// </summary>
        /// <param name="starterIP"></param>
        /// <returns></returns>
        public string propagateCoordinatorMessage(string starterIP)
        {
            StringBuilder result = new StringBuilder("");

            //boolean variable to ensure whether the election has happened
            //if happened we will have at least one client in the list
            bool isElectionHappened = false;

            //for each client update their coordinator ips
            foreach (Client client in Server.getInstance().getClients())
            {
                client.updateCoordinator();
                isElectionHappened = true;
            }

            //check whether election has happened print out the coordinator success message
            if (isElectionHappened)
            {
                result.Append("Coordinator message propagated and now each node knows about the new coordinator" + "\n");
            }

            return result.ToString();
        }

        /// <summary>
        /// Method to do distributed sign off operation from the network.
        /// </summary>
        /// <param name="nodeID">ID of node that should be removed from the network</param>
        /// <returns></returns>
        public string signOffNode(int nodeID)
        {
            StringBuilder result = new StringBuilder("");  
            string nodeIPToBeSignedOff = Server.getInstance().getNetwork()[nodeID];

            result.Append(Utils.logTimestamp(0) + nodeIPToBeSignedOff
                    + " wants to sign off  from the network" + "\n");

            //Server.getInstance() will return the singleton that were created before
            //removeHost will remove the host from network with given ip
            Server.getInstance().removeHost(nodeIPToBeSignedOff);

            //now it is time to remove the client with given ip from clients list
            Server.getInstance().removeClientByIP(nodeIPToBeSignedOff);


            Server.getInstance().resetNodesForCoordinatorElection();
            //if all clients has been signed off from the network reset the next priority
            //for new joining clients in the future

            if (Server.getInstance().getClients().Count == 0)
            {
                Server.getInstance().resetNextPriority();
            }

            // helper method which help to enumerate clients' priorities
            // in the case of 1, 2, 4 to 1, 2, 3 because 3rd node has been signed off
            Server.getInstance().enumerateClientsPriorities();

            result.Append(Utils.logTimestamp(0) + "\n" );
            Array.ForEach(Server.getInstance().getNodeArray(), s => result.AppendLine(s));
            return result.ToString();

        }

        /// <summary>
        /// This method propagates 'signOff' request in the network. 
        /// </summary>
        /// <param name="nodeID">ID of node that signed off</param>
        /// <returns></returns>
        public string propagateSignOffRequest(int nodeID)
        {
            StringBuilder result = new StringBuilder("");

            // all remaining clients will update their address table
            foreach (Client client in Server.getInstance().getClients())
            {
                // by the address table client knows about other clients
                client.updateAddressTable();
            }

            result.Append("Host is removed from network " + "\n");
            Array.ForEach(Server.getInstance().getNodeArray(), s => result.AppendLine(s));
            result.Append("SignOff message propagated and address table of each client updated" + "\n");
          
            return result.ToString();
        }


        /// <summary>
        /// This method returns server IP and list of clients' IP with IDs in network.
        /// </summary>
        /// <returns>Server IP and list of clients' IP with ID in network</returns>
        public string showNetworkState()
        {
            StringBuilder result = new StringBuilder("");

            int nodeId = 0;
            string[] nodes = Server.getInstance().getNodeArray();

            foreach (string nodeIP in nodes)
            {
                if (nodeId == 0)
                {
                    result.Append("Server " + nodeId + " - " + nodeIP + "\n");
                    nodeId++;
                }
                else
                {
                    if (Server.getInstance().getMasterNode().getIP().Equals(nodeIP, StringComparison.InvariantCultureIgnoreCase))
                    {
                        result.Append("Host " + nodeId + " - " + nodeIP + " [COORDINATOR]" + "\n");
                    }
                    else
                    {
                        result.Append("Host " + nodeId + " - " + nodeIP + "\n");
                        
                    }
                    nodeId++;
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// This method returns true or false depending if new joining client is in the network or not.
        /// </summary>
        /// <param name="IP">IP address of host</param>
        /// <returns>bool value</returns>
        public bool checkHostEligibility(string IP)
        {
            return Server.getInstance().checkHostEligibility(IP);
        }

        /// <summary>
        /// This method checks if there at least one host in network.
        /// </summary>
        /// <returns></returns>
        public bool atLeastOneHostAvailable()
        {
            return Server.getInstance().atLeastOneHostAvailable();
        }


        /*
        private string getNodesString()
        {
            // return a result as a lot of ip:port delimited by comma
            // do this by creating an empty string builder and appending nodes to it
            StringBuilder result = new StringBuilder("");

            // update the string for each node
            foreach (string node in Server.getInstance().getNodeArray())
            {
                result.Append(node + "," );
            } // for

            // we will not take the first element of string because it is the server
            // we only want to return nodes' ip addresses
            return result.ToString();
        }*/
    }
}
