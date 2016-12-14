using HelloService;
using Microsoft.Samples.XmlRpc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.ServiceModel;
using System.Text;
using System.Threading;

namespace Host
{
    public class Client
    {

        IRequestProcessor node;
        private string socketAddress;
        private bool status;
        private int ID = 0;
        private int priority = 0;
  
        private bool isMasterNode = false;
        private List<string> addressTable = new List<string>();
        private string coordinatorIP;

        //instance variable to be made true, when one of the nodes
        //in the network want to start distributed read and write operations
        //start message will be propagated to all nodes in the network
        private bool canStartDistributedReadWriteOperations = false;


        /// <summary>
        /// Contains IP address, and node. 
        /// </summary>
        public Dictionary<string,IRequestProcessor> nodeList = new Dictionary<string,IRequestProcessor>();

        /// <summary>
        /// Contains ID and IP address.
        /// </summary>
        public Dictionary<int, string> clientListWithID = new Dictionary<int, string>();

        //master string that will be used during distributed read and write operations
        private volatile StringBuilder masterString = new StringBuilder("");

        //Set to remember words written by client to the master string
        private List<string> writtenWords = new List<string>();

        //shows whether the client(node) is in the critical section
        //initially false
        private volatile bool usinG = false;

        //shows whether this client(node) wants to enter critical section
        //initially false this client currently does not want to access critical section
        private volatile bool wanted = false;

        //OK count variable for counting number of OKs received by
        //receiving sites in Ricart&Agrawala algorithm, initially 0
        private volatile int OKCount = 0;


        //timestamp variable that will be used during Ricart&Agrawala algorithm
        //initially zero
        private volatile int timestamp = 0;


        //in Ricart and Agrawala each node maintains a request queue
        private  LinkedList<string> requestQueue = new LinkedList<string>();

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

        /// <summary>
        /// Setter method for the same variable
        /// </summary>
        /// <param name="canStartDistributedReadWriteOperations"></param>
        public void setCanStartDistributedReadWriteOperations(bool canStartDistributedReadWriteOperations)
        {
            this.canStartDistributedReadWriteOperations = canStartDistributedReadWriteOperations;
        }



        /// <summary>
        /// Getter method for master string
        /// </summary>
        /// <returns></returns>
        public string getMasterString()
        {
            return masterString.ToString();
        }


        /// <summary>
        ///  Getter method for written words
        /// </summary>
        /// <returns></returns>
        public List<string> getWrittenWords()
        {
            return writtenWords;
        }


        /// <summary>
        /// Helper method for the client to remember what has been written.
        /// </summary>
        /// <param name="word">Random english wordk</param>
        public void rememberWord(string word)
        {
            writtenWords.Add(word);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public string receiveMessage(string message, string requesterIP)
        {
            
            // if the received message is "request"
            if (message.Equals("request", StringComparison.InvariantCultureIgnoreCase))
            {
                //if critical section is empty, access has to be granted
                if (Server.getInstance().isCriticalSectionEmpty())
                    {
                    // so send "GRANTED"
                    Server.getInstance().assignToCriticalSection(requesterIP);

                    return "GRANTED";
                    }
                else // someone is in the critical section
                {
                    // queue the request, return DENIED
                    Server.getInstance().queueRequest(requesterIP);
                    return "DENIED";
                }

            }
            else if (message.Equals("release", StringComparison.InvariantCultureIgnoreCase))
            {
                // requester is done with critical section
                // remove the requesterIP from critical section

                Server.getInstance().removeFromCriticalSection(requesterIP);

                // if queue is not empty, dequeue first and reply "GRANTED"
                if (!Server.getInstance().isQueueEmpty())
                {
                    string newRequesterIP = Server.getInstance().dequeue();

                    //assign critical section new enterer ip
                    Server.getInstance().assignToCriticalSection(newRequesterIP);
                    
                    //print out who is the new enterer
                    Console.WriteLine("New Critical Section Enterer IP: " + newRequesterIP);

                    Client client = Server.getInstance().getClientByIP(newRequesterIP);
                    // get the random English word
                    string randomEnglishWord = Server.getInstance()
                            .generateRandomEnglishWord();

                    // method to write this generated word to
                    // client's
                    // buffer to check it later whether or not
                    // written
                    // word exists in the resulting master string
                    client.rememberWord(randomEnglishWord);

                    try
                    {
                        node = client.nodeList.Values.Last();
                        object reply = node.enterCS(client.getIP(),randomEnglishWord);
                        Console.WriteLine((string)reply);
                        
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception while calling method"
                                    + " RequestProcessor.enterCS\n");
                        
                    }

                    //to prevent deadlock, because the same thread can
                    //access the sendMessage again
                    ThreadStart threadDelegate = new ThreadStart(run);
                    Thread tempThread = new Thread(threadDelegate);
                    tempThread.Start();
                    return "GRANTED";
                }
                else // queue is already empty, so reply GRANTED
                {
                    return "GRANTED";
                }

            }
            else
            {
                return "GRANTED";
            }
        }

        public void run()
        {
            // if everything in critical section was OK
            // send "release" message to the coordinator

            Client client = new Client(Server.getInstance().getSocketAddress(), Utils.getDefaultSocketAddress());
            try
            {
                node = client.nodeList.Values.Last();
                node.sendMessage("release", client.getIP());
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception while calling method"
                                                + " RequestProcessor.sendMessage "
                                                + "with message RELEASE\n");
            }
        }


        /// <summary>
        /// method to write to the master string
        /// </summary>
        /// <param name="randomEnglishWord">Word to write master string</param>
        /// <returns></returns>
        public string writeToMasterString(string randomEnglishWord)
        {
            return masterString.Append(randomEnglishWord).ToString();
        }


        public string toString()
        {
            return this.getIP();
        }

        /// <summary>
        /// Shows whether the node is in the critical section.
        /// </summary>
        /// <param name="usinG"></param>
        public void setUsing(bool usinG) 
        {
            this.usinG = usinG;
	    }

        /// <summary>
        /// Shows whether this node wants to enter critical section.
        /// </summary>
        /// <param name="wanted"></param>
        public void setWanted(bool wanted)
        {
            this.wanted = wanted;
        }


        /// <summary>
        /// if node is done with critical section, it resets OK count to 0.
        /// </summary>
        public void resetOKCount()
        {
            OKCount = 0;
        }

        
        /// <summary>
        /// At each interation of 20 second loop all nodes will start to access critical section.
        /// So, clock has to be reset.
        /// </summary>
        public void resetClock()
        {
            timestamp = 0;
        }

        /// <summary>
        /// Clear requestqueue of node.
        /// </summary>
        public void emptyRequestQueue()
        {
            requestQueue.Clear();
        }

        //to find maximum of sender' clock and receiver's own clock
        private int max(int recieverTimestamp, int requesterTimestamp)
        {
            return Math.Max(recieverTimestamp, requesterTimestamp);
        }

        public string getCoordinatorIP()
        {
            return coordinatorIP;
        }

        //gets the address table of this client
        public List<string> getAddressTable()
        {
            return addressTable;
        }

        /// <summary>
        /// Each node will update its logical clock at request sending
        /// </summary>
        public void updateClock()
        {
            timestamp++;
        }

        //update the clock depending on requester's timestamp
        public void updateClock(int requesterTimestamp)
        {
            //in lamport clock for the recieve event
            //the following happens
            //LC <- MAX( LC, LC_sender ) + 1
            timestamp = max(getTimestamp(), requesterTimestamp) + 1;
        }

        /// <summary>
        /// Getter method to get the logical clock reading.
        /// </summary>
        /// <returns></returns>
        public int getClock()
        {
            return getTimestamp();
        }

        public bool isUsing()
        {
            return usinG;
        }

        //getter
        public bool isWanted()
        {
            return wanted;
        }

        /// <summary>
        /// Getter method for timestamp, will be used internally.
        /// </summary>
        /// <returns></returns>
        private int getTimestamp()
        {
            return timestamp;
        }

        //method to for queueing the request
        private void queueRequest(string requesterIP)
        {
            requestQueue.AddLast(requesterIP);
        }

        //get Request Queue
        public LinkedList<string> getRequestQueue()
        {
            return requestQueue;
        }

        //get the request queue head
        public string getRequestQueueHead()
        {
            return requestQueue.First();
        }

        //increment OK count for each "OK" message by message receivers
        public void incrementOKCount()
        {
            OKCount++;
        }

        //for updating OK count before entering critical section
        public int getOKCount()
        {
            return OKCount;
        }

        //receive request method for client which can send "OK" or "DEFERRED" messages
        //depending on a situation
        [MethodImpl(MethodImplOptions.Synchronized)]
        public string receiveMessage(string message, string requesterIP, int requesterTimestamp)
        {
            if(message.Equals("request", StringComparison.InvariantCultureIgnoreCase))
            {
                updateClock(requesterTimestamp);
                Console.WriteLine("\nReceiver " + getIP() + " with timestamp "
                    + getTimestamp() + " received \"request\" from requester "
                    + requesterIP);

                // if not accessing the resource and do not want to access it send
                // OK

                if (!isUsing() && !isWanted())
                {
                    Console.WriteLine(" replied \"OK\"");
                    return "OK" + "," + getClock(); ;
                }
                else if (isUsing()) // currently using the resource
                {
                    queueRequest(requesterIP);
                    Console.WriteLine(" replied \"DEFERRED\"");
                    return "DEFERRED";

                }
                else if(isWanted()) // wants to access a resource too
                {
                    if (requesterTimestamp < getTimestamp()
                        || (requesterTimestamp == getTimestamp() && requesterIP.CompareTo(getIP()) < 0))
                    {
                        Console.WriteLine(" replied \"OK\"");
                        return "OK" + "," + getClock(); ;
                    }
                    else
                    {
                        queueRequest(requesterIP);

                        Console.WriteLine("Client " + requesterIP
                                + " has timestamp " + requesterTimestamp + " > "
                                + getTimestamp() + " timestamp" + " of receiver "
                                + getIP());
                        Console.WriteLine("Adding to " + getIP() + "'s queue: "
                                + requesterIP);
                        Console.WriteLine(getIP() + "'s Queue: "
                                + getRequestQueue());
                        Console.WriteLine("Head of " + getIP() + "'s queue: "
                                + getRequestQueueHead());


                        Console.WriteLine(" replied \"DEFERRED\"");
                        return "DEFERRED";
                    }
                }
                else
                {
                    Console.WriteLine(" replied \"null\"");
                    return null;
                }

            }
            //else if this node has received OK from the other node, increment ok count
            else if (message.Equals("OK", StringComparison.InvariantCultureIgnoreCase))
            {
                //update clock if the OK message have been received
                updateClock(requesterTimestamp);
                incrementOKCount();
                return null;
            }
            else
            {
                return null;
            }
        }
    }
}
