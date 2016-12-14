using HelloService;
using Microsoft.Samples.XmlRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Runtime.CompilerServices;
using System.Collections.Concurrent;

namespace Host
{
    public class Server
    {
        /// <summary>
        /// Class variable to hold the singleton.
        /// </summary>
        private static Server serverInstance = null;

        private string IP;
        private int port;

        private int nextPriority = 1;

        //data structure to hold the ips which is in critical section//, get concurrent set
        private ConcurrentQueue<string> criticalSection = new ConcurrentQueue<string>();

        private ServiceHost host;

        /// <summary>
        /// List named 'network' to hold ip address of node's in the net.
        /// </summary>
        private List<string> network = new List<string>();

        /// <summary>
        /// List that hold nodes in network.
        /// </summary>
        private List<Client> clients = new List<Client>();


        //data structure to hold the ips which is in the waiting queue for the desired resource
        private Queue<string> waitingQueue = new Queue<string>();

        /// <summary>
        /// Factory method.
        /// </summary>
        /// <returns>Server with port '8080' </returns>
        public static Server getInstance()
        {
            if (serverInstance == null)
            {
                serverInstance = new Server();
            }
            
            return serverInstance;
        }

        /// <summary>
        /// This method returns server singleton with specified port.
        /// </summary>
        /// <param name="socketAddress">IP:PORT that user enters</param>
        /// <returns>Server singleton with specified port</returns>
        public static Server getInstance(string socketAddress)
        {
            if (serverInstance == null)
            {
                serverInstance = new Server(socketAddress);
            }
            return serverInstance;
        }


        /// <summary>
        /// Default constructor. IP address will be "localhost", PORT "8080".
        /// </summary>
        private Server()
        {
            // this(Utils.toSocketAddress("localhost", 8080));
            new Server(Utils.toSocketAddress("localhost", 8080));

        }

        /// <summary>
        /// Constructor method.
        /// </summary>
        /// <param name="socketAddress">IP:PORT address that user enters</param>
        private Server(string socketAddress)
        {
            IP = Utils.getIPFromSocketAddress(socketAddress);
            port = Utils.getPortFromSocketAddress(socketAddress);
            network.Add(IP);
            host = new ServiceHost(typeof(RequestProcessor));
            
            Uri baseAddress = new Uri("http://" + socketAddress);

            var epXmlRpc = host.AddServiceEndpoint(typeof(IRequestProcessor), new WebHttpBinding(WebHttpSecurityMode.None), "/xmlrpc");
            epXmlRpc.Behaviors.Add(new XmlRpcEndpointBehavior());

                // Enable metadata exchange.
            ServiceMetadataBehavior smb = new ServiceMetadataBehavior();
            smb.HttpGetEnabled = true;
        //    host.Description.Behaviors.Add(smb);
            
        }


        /// <summary>
        /// This method starts the server.
        /// </summary>
        public void start()
        {

            try
            {
                host.Open();
                Console.WriteLine("Server started successfully @ " + DateTime.Now.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine("Server has already been started, proceed with operations.");
            }
        }


        /// <summary>
        /// This method stops the server.
        /// </summary>
        public void stop()
        {
            try
            {
                host.Close();
                Console.WriteLine("Server stopped successfully.......");
                Console.ReadLine();
            }
            catch(Exception e)
            {
                Console.WriteLine("Server stopped successfully.......");
                Console.ReadLine();
            }
        }

        /// <summary>
        /// This method adds IP address to 'network' list.
        /// </summary>
        /// <param name="IP">Address that user enters</param>
        public void addToNetwork(string IP)
        {
            network.Add(IP);
        }

        /// <summary>
        /// This method checks if server is in the network. 
        /// </summary>
        /// <returns>a bool value</returns>
        public bool isNetworkAvailable()
        {

            return network[0] != null;
        }

        /// <summary>
        /// This method checks whether or not there is at least one host.
        /// It will happen when the network size is bigger than or equal to 2.
        /// In case of 2, we will have one service and one node
        /// </summary>
        /// <returns>a bool value</returns>
        public bool atLeastOneHostAvailable()
        {
            return network.Count >= 2;
        }

        
        /// <summary>
        /// This method checks whether new joining node is in the network.
        /// </summary>
        /// <param name="IP">Address that user enters</param>
        /// <returns>a bool value</returns>
        public bool checkHostEligibility(string IP)
        {
            return network.Contains(IP);
        }
        

        /// <summary>
        /// This method returns socket address of server.
        /// </summary>
        /// <returns>Socket address of server</returns>
        public string getSocketAddress()
        {
            return Utils.toSocketAddress(IP, port);
        }


        /// <summary>
        /// Method for adding node to network.
        /// </summary>
        /// <param name="socketAddress">Address that user enters</param>
        public void addHost(string socketAddress)
        {
            string ip = Utils.getIPFromSocketAddress(socketAddress);
            network.Add(ip);
        }


        /// <summary>
        /// Method to remove node from network.
        /// </summary>
        /// <param name="nodeIP">IP address of node</param>
        public void removeHost(string nodeIP)
        {
            network.Remove(nodeIP);
        }


        /// <summary>
        /// This method returns "network" list that contains IP addresses of nodes in network.
        /// </summary>
        /// <returns>"network" list that contains IP addresses of nodes in network.</returns>
        public List<string> getNetwork()
        {
            return network;
        }


        /// <summary>
        /// Getter method that will be used by client instance when it initializes itself.
        /// </summary>
        /// <returns></returns>
        public int getNextPriority()
        {
            return nextPriority++;
        }


        /// <summary>
        /// This method is for updating 'clients' list.
        /// </summary>
        /// <param name="clientSocketAddress">Socket address of client</param>
        public void updateClientList(string clientSocketAddress)
        {
            try
            {
                Client client = new Client(getSocketAddress(), clientSocketAddress);
                client.setPriority(Server.getInstance().getNextPriority());
                clients.Add(client);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception in updating client's list on the server."
                        + "\nClient's list will not be changed");
            } 
        }


        /// <summary>
        /// Helper method to enumerate the priorities for clients.
        /// </summary>
        public void enumerateClientsPriorities()
        {
            //priorities are something like one based indices 
            int startingPriority = 1;
            foreach (Client client in clients)
            {
                client.setPriority(startingPriority);
                startingPriority++;
            } 
        } 
        

        /// <summary>
        /// This method is for removing client by ip from 'clients' list.
        /// </summary>
        /// <param name="IP">IP address of client</param>
        public void removeClientByIP(string IP)
        {
            foreach (Client client in clients)
            {
                // if we find the client with the given ip we remove if from the list and 
                // break the loop
                if (client.getIP().Equals(IP))
                {
                    clients.Remove(client);
                    break;
                } 
            } 
        }


        /// <summary>
        /// Method to get the client by IP
        /// </summary>
        /// <param name="IP">IP address of client</param>
        /// <returns></returns>
        public Client getClientByIP(string IP)
        {
            Client clientToBeReturned = null;

            //we will loop through all the clients
            //find the corresponding client with specified IP
            //and return it back
            foreach (Client client in clients)
            {
                if (client.getIP().Equals(IP))
                {
                    clientToBeReturned = client;
                }
            } 

            //in case the client not found, null will be returned
            return clientToBeReturned;
        }


        /// <summary>
        /// Method to get the master node.
        /// </summary>
        /// <returns>Master node in the network. If no master node, returns 'null'.</returns>
        public Client getMasterNode()
        {
            //get the master by looping through all the clients in the network
            foreach (Client client in clients)
            {
                if (client.MasterNode())
                    return client;
            } 

            //if this return is reached it means, there is no master node, we return null
            return null;
        }


        /// <summary>
        /// This method gets the nodes currently in the network.
        /// </summary>
        /// <returns>The nodes currently in the network.</returns>
        public List<Client> getClients()
        {
            return clients;
        }

        
        /// <summary>
        /// This method convert list to array.
        /// </summary>
        /// <returns></returns>
        public string[] getNodeArray()
        {
            return network.ToArray();
            
            
        }

 
        /// <summary>
        /// This method do the same broadcasting thing for clients.
        /// </summary>
        /// <returns>The clients to broadcast messages.</returns>
        public Client[] getBroadcastClients()
        {
            //last added client won't be broadcasted.
            Client[] broadCastClients = new Client[clients.Count - 1];
            for (int index = 0; index < broadCastClients.Length; index++)
            {
                broadCastClients[index] = clients[index];
            } 

            return broadCastClients;
        }


        /// <summary>
        /// This method returns first client in 'clients' list.
        /// </summary>
        /// <returns></returns>
        public Client getFirstRemainingClient()
        {
            return clients[0]; ;
        }


        /// <summary>
        /// This method initializes 'nextPriority' to '1' in case all clients signed off from the network.
        /// </summary>
        public void resetNextPriority()
        {
            nextPriority = 1;
        }



        //method to reset nodes for coordinator election
        //this will be called during join and signOff operations
        //to reset each clients isMasterNode string to false
        //and in coordinator election process one of these clients
        //will be set as master node again
        /// <summary>
        /// Method to reset nodes for coordinator election
        /// </summary>
        public void resetNodesForCoordinatorElection()
        {
            //enumerate clients to reset their isMasterNode string to false
            foreach (Client client in clients)
            {
                client.setMasterNode(false);
            } 
        }

        /// <summary>
        /// This method gets the clients from "clients" list, makes a shallow copy.
        /// </summary>
        /// <returns>clients list</returns>
        public List<Client> getShallowCopyClients()
        {
            //original client list does not change
            List<Client> clientList = new List<Client>();

            foreach (Client client in clients)
            {
                clientList.Add(client);
            } 

            return clientList;
        }



        /// <summary>
        /// Helper method to create a corpus of words.
        /// </summary>
        /// <returns></returns>
        private string[] getCorpus()
        {
            return new string[]
                    {
                "when", "in", "the", "course", "of", "human", "events", "it", "becomes",
                "necessary", "book", "shelf", "paper", "phone",
                "as", "gregor", "samsa", "awoke", "one", "morning", "from", "uneasy",
                "dreams", "he", "they", "you", "imagine", "web", "window",
                "found", "himself", "transformed",
                "his", "bed", "into", "a", "gigantic", "insect",
                "far", "out", "uncharted", "backwaters", "unfashionable",
                "end", "of", "western", "spiral", "arm", "of", "galaxy", "lies",
                "small", "regarded", "yellow", "sun",
                 "truth", "universally", "acknowledged",
                 "not", "nasty", "dirty", "wet", "hole", "filled", "with", "ends", "of", "worms",
                 "an", "oozy", "smell", "nor", "yet", "dry", "bare", "sandy", "hole",
                 "with", "nothing", "in", "it", "to", "sitdown", "on", "or", "to",
                 "eat", "was", "hobbit", "hole", "that", "means", "comfort"
                    };
        }


        /// <summary>
        /// Method to generate a random word
        /// </summary>
        /// <returns></returns>
        public string generateRandomEnglishWord()
        {
            //random to generate random index
            Random random = new Random();

            //get the corpus of words
            string[] corpus = getCorpus();
            //randomize the array
            corpus = corpus.OrderBy(x => random.Next()).ToArray();

            List<string> wordList = new List<string>();
            wordList = corpus.ToList();

            //get the random index
            int randomIndex = random.Next(wordList.Count());

            return wordList[randomIndex];

        }

        
        /// <summary>
        /// Helper method to return whether or not the critical section is empty.
        /// </summary>
        /// <returns></returns>
        public bool isCriticalSectionEmpty()
        {
            if (criticalSection.Count() == 0)
            {
                return true;
            }
            else
            {
                return false;

            }
        }


        /// <summary>
        /// Helper method to assign cliens with specific ip to critical section
        /// </summary>
        /// <param name="requesterIP"></param>
        public void assignToCriticalSection(string requesterIP)
        {
            criticalSection.Enqueue(requesterIP);
        }

        /// <summary>
        /// This method removes requesterIP from criticalsection.
        /// </summary>
        /// <param name="requesterIP"></param>
        public void removeFromCriticalSection(string requesterIP)
        {
            //criticalSection.remove(requesterIP);
            criticalSection.TryDequeue(out requesterIP);
        }


      
        /// <summary>
        /// Returns criticalsection dictionary.
        /// </summary>
        /// <returns></returns>
        public ConcurrentQueue<string> getCriticalSection()
        {
            return criticalSection;

        }

        /// <summary>
        /// Helper method to queue request which has been denied for a limited amount of time.
        /// Adds requester's IP to waiting queue.
        /// </summary>
        /// <param name="requesterIP">IP address of requester node</param>
        public void queueRequest(string requesterIP)
        {
            waitingQueue.Enqueue(requesterIP);
        }

        /// <summary>
        /// Method to retrieve the waiting queue.
        /// </summary>
        /// <returns></returns>
        public Queue<string> getWaitingQueue()
        {
            return waitingQueue;
        }


        /// <summary>
        ///  Method to get a head of the queue.
        /// </summary>
        /// <returns></returns>
        public string getWaitingQueueHead()
        {
            return waitingQueue.First();
        }


        /// <summary>
        /// Helper method to detect whether the waiting queue is empty.
        /// </summary>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool isQueueEmpty()
        {
            if (waitingQueue.Count() == 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
 
        /// <summary>
        /// Helper method to dequeue (return and remove) first element from the waiting queue
        /// </summary>
        /// <returns></returns>
        public string dequeue()
        {
            return waitingQueue.Dequeue();
        }

        
        /// <summary>
        /// Convenient array representation for getClientsOtherThan method.
        /// </summary>
        /// <param name="thisClient"></param>
        /// <returns></returns>
        public Client[] getClientsArrayOtherThan(Client thisClient)
        {
            return getClientsOtherThan(thisClient).ToArray();
        }

        /// <summary>
        /// This method gets all clients except than this client.
        /// </summary>
        /// <param name="thisClient"></param>
        /// <returns></returns>
        public List<Client> getClientsOtherThan(Client thisClient)
        {
            // get the clients, create a list and different clients
            // other than this client in list
            List<Client> clientList = new List<Client>();

            // for each client add the client to the copy list
            foreach (Client client in clients)
            {
                if (!client.Equals(thisClient))
                {
                    clientList.Add(client);
                } 
            } 

            return clientList;
        }

    }
}
