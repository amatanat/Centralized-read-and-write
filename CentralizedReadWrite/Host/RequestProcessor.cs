using HelloService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

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
        
            Server.getInstance().updateClientList(nodeSocketAddress);
          
            Server.getInstance().resetNodesForCoordinatorElection();
            Server.getInstance().enumerateClientsPriorities();
         
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

        /// <summary>
        /// This method propagates start message in the network for distributed read write operation.
        /// </summary>
        /// <returns></returns>
        public string startDistributedReadWrite()
        {
            foreach (Client client in Server.getInstance().getClients())
            {
                client.setCanStartDistributedReadWriteOperations(true);
            } 

            return "Start message propagated and nodes can start distributed read"
                    + " and write operations\n";
        }

       // public static int index;

        /// <summary>
        /// This method implements Centralized Mutual Exclusion Algortihm.
        /// </summary>
        /// <returns></returns>
        public string doCeMutEx()
        {
            StringBuilder result = new StringBuilder("");

            //random instance to generate random amount of waiting time.
            Random rand = new Random();

            //Gets the number of milliseconds elapsed since the system started.
            int startTime = Environment.TickCount;
            int timeStamp = 0;

            while (Environment.TickCount - startTime <= 20 * 1000)
            {
                //sleep a little bit
                int timeToSleep = rand.Next(2) + 1;

                try
                {
                    result.Append(Utils.logTimestamp(timeStamp) + "sleep for "
                            + timeToSleep + " seconds.\n");
                    Console.WriteLine(Utils.logTimestamp(timeStamp) + "sleep for "
                            + timeToSleep + " seconds.\n");
                    //Suspends the current thread for the specified number of milliseconds.
                    Thread.Sleep(timeToSleep * 1000);
                } 
                catch (Exception e)
                {
                    Console.WriteLine("Exception on calling Thread.Sleep method");
                }

                //get the clients
                List<Client> clientList = Server.getInstance().getShallowCopyClients();
                Client[] clients = clientList.ToArray();
                Thread[] threads = new Thread[clients.Length];
                for (int i = 0; i < clients.Length; i++)
                {
                    int index = i;
                    threads[i] = new Thread(delegate() { run(index); });     //captures "index" and uses that when the delegate is executed
                }

                //now start the threads concurrently
                foreach (Thread thread in threads)
                {
                    thread.Start();
                } 

                // Now everything's running - join all the threads
                // started to all the threads you want to run in parallel,
                // then call join on them all
                // this will prevent threads run sequentially.
                foreach (Thread thread in threads)
                {
                    try
                    {
                        thread.Join();
                    }
                    catch (Exception e)
                    {
                       Console.WriteLine("Exception in join call");
                    } 
                } 

                timeStamp++;
            }

            // sleep here for 10 seconds before all threads finish the job
            try
            {
                Thread.Sleep(10 * 1000);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception in Thread.sleep call");
            }

            return result.ToString();
        }

        public void run(int index)
        {
            
            StringBuilder result = new StringBuilder("");
            
            List<Client> clientList = Server.getInstance().getShallowCopyClients();
            Client[] clients = clientList.ToArray();

            IRequestProcessor node;
            Client client = new Client(Server.getInstance().getSocketAddress(), Utils.getDefaultSocketAddress());

            // boolean variable to achieve consistency in the code
            bool isMessageSendOk = false;
            
            node = client.nodeList.Values.Last(); 

            // reply will contains the string GRANTED or DENIED
            object reply = null;
            
            try
            {
                reply = node.sendMessage("request",clients[index].getIP());
                isMessageSendOk = true;
                
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("Exception in executing the "
                   + "RequestProcessor.sendMessage method\n");
                result.Append("Exception in executing the "
                + "RequestProcessor.sendMessage method\n");
            }

            if (isMessageSendOk)
            {
                string replyString = (string)reply;

                // if reply was GRANTED
                if (replyString.Equals("GRANTED", StringComparison.InvariantCultureIgnoreCase))
                {
                    string randomEnglishWord = Server.getInstance().generateRandomEnglishWord();

                    clients[index].rememberWord(randomEnglishWord);

                    // boolean variable to hold whether
                    // criticalSection entrance was OK
                    bool isCriticalSectionSuccess = false;

                    try
                    {
                        reply = node.enterCS(clients[index].getIP(),
                                        randomEnglishWord);
                                            
                        // reply could be like
                        // "Node with ip has written some word"
                        Console.WriteLine((string)reply);
                        result.Append((string)reply);
                        isCriticalSectionSuccess = true;
                    }
                    catch(Exception e)
                    {
                        Console.Error.WriteLine("Exception while calling method"
                         + " RequestProcessor.enterCS\n");
                        result.Append("Exception while calling method"
                                + " RequestProcessor.enterCS\n");
                    }

                    // if everything in critical section was OK
                    // send "release" message to the coordinator
                    if (isCriticalSectionSuccess)
                    {
                        Console.WriteLine(clients[index].getIP()
                                            + " sending \"release\" message\n");
                        result.Append(clients[index].getIP()
                                + " sending \"release\" message\n");

                        try
                        {
                            node.sendMessage("release",clients[index].getIP());
                        }
                        catch(Exception e)
                        {
                            Console.WriteLine("Exception while calling method"
                            + " RequestProcessor.sendMessage with message RELEASE\n");
                            result.Append("Exception while calling method"
                            + " RequestProcessor.sendMessage with message RELEASE\n");
                        }
                    }
                }
                   
                    
            }
        }

        /// <summary>
        /// Helper method for message send to the coordinator.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="requesterIP"></param>
        /// <returns></returns>
        public string sendMessage(string message, string requesterIP)
        {
            //call the receiveMessage method in coordinator
            return Server.getInstance().getMasterNode().receiveMessage(message, requesterIP);
        }


        /// <summary>
        /// Helper method that each node will read final string at the end and write it to the screen.
        /// Will also check the words they have added exist in the final string.
        /// </summary>
        /// <returns></returns>
        public string readFinalString()
        {
            StringBuilder result = new StringBuilder("");
            string finalString = null;

            //each client will read the final string from master node and 
            //check whether their words that have been written exists in it
            foreach (Client client in Server.getInstance().getClients())
            {
                Console.WriteLine(getConvenientOutput());
                result.Append(getConvenientOutput());

                //first get master string from master node
                finalString = Server.getInstance().getMasterNode().getMasterString();

                //output what has been read
                Console.WriteLine("Client " + client.getIP() + " read final string: "
                        + finalString + "\n");
                result.Append("Client " + client.getIP() + " read final string: "
                        + finalString + "\n");

                //now output what client has been written to final string
               Console.WriteLine("Client " + client.getIP() + " has written the following words:\n");
                result.Append("Client " + client.getIP() + " has written the following words:\n");

                int wordId = 1;
                //enumerate words
                foreach (string word in client.getWrittenWords())
                {
                    Console.WriteLine(wordId + " " + "\"" + word + "\""
                            + " exists in final string: " + finalString.Contains(word) + "\n");
                    result.Append(wordId + " " + "\"" + word + "\""
                            + " exists in final string: " + finalString.Contains(word) + "\n");
                    wordId++;
                }
                getConvenientOutput();
            } 

            return result.ToString();
        }

        private string getConvenientOutput()
        {
            return "--------------------------------------------------------------\n";
        }


        /// <summary>
        /// Helper method to simulate the entrance to critical section
        /// </summary>
        /// <param name="entererIP"></param>
        /// <param name="word"></param>
        /// <returns></returns>
        public string enterCS(string entererIP, string word)
        {
            string newMasterStirng = Server.getInstance().getMasterNode().writeToMasterString(word);
            return "Node " + entererIP + " has written " + "\"" + word + "\""
                    + " to master string: " + newMasterStirng + "\n";
        }

        //helper method for request sending to all nodes other than this
        //Used by ------- Ricart & Agrawala Algorithm -------
        public string sendmessage(string message, string requesterIP,
                int requesterTimestamp, string receiverIP)
        {
            Client client = Server.getInstance().getClientByIP(receiverIP);
            return client.receiveMessage(message, requesterIP, requesterTimestamp);
        }

        private static IRequestProcessor node;

        public string doRicartAgrawala()
        {
            StringBuilder result = new StringBuilder("");

            //random instance to generate random amount of waiting time.
            Random rand = new Random();

            int startTime = Environment.TickCount;
            int timeStamp = 0;

            while (Environment.TickCount - startTime <= 20 * 1000)
            {
                //sleep a little bit
                int timeToSleep = rand.Next(2) + 1;

                try
                {
                    result.Append(Utils.logTimestamp(timeStamp) + "sleep for "
                            + timeToSleep + " seconds.\n");
                    Console.WriteLine(Utils.logTimestamp(timeStamp) + "sleep for "
                            + timeToSleep + " seconds.\n");
                    //Suspends the current thread for the specified number of milliseconds.
                    Thread.Sleep(timeToSleep * 1000);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception on calling Thread.Sleep method");
                }

                //get the clients
                List<Client> clientList = Server.getInstance().getShallowCopyClients();
                Client[] clients = clientList.ToArray();
                Thread[] threads = new Thread[clients.Length];
    

                // performed by each client at initialization
                //that state_Pi:= RELEASED
                foreach (Client client in clientList)
                {
                    client.setUsing(false);
                    client.setWanted(false);
                    client.resetOKCount();
                    client.resetClock();
                    client.emptyRequestQueue();
                }


                for (int i = 0; i < clients.Length; i++)
                {
                    int index1 = i;
                    threads[i] = new Thread(delegate () { ruN(index1); }); //captures "index1" and uses that when the delegate is executed
                }

                // now start the threads concurrently
                foreach (Thread thread in threads)
                {
                    thread.Start();
                }

                foreach (Thread thread in threads)
                {
                    try
                    {
                        thread.Join();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Interrupted exception in join call");
                    }
                }

                timeStamp++;
            }
            return result.ToString();
        }

        public void ruN(int index1)
        {
            StringBuilder result = new StringBuilder("");
            List<Client> clientList = Server.getInstance().getShallowCopyClients();
            Client[] clients = clientList.ToArray();

            Client[] clientsOtherThanThis = Server.getInstance()
                                .getClientsArrayOtherThan(clients[index1]);


            //set the state of this client to wanted, because
            //it wants to access critical section
            clients[index1].setWanted(true);


            //and before submitting request, the client must update its
            //logical clock, so
            //update clock, which will increment default timestamp
            //basically Ricart&Agrawala is using extended Lamport clock
            //in extended lamport clock, for each send event, the node
            //has to update its clock (increment timestamp)
            clients[index1].updateClock();

            Client clienT = new Client(Server.getInstance().getSocketAddress(), Utils.getDefaultSocketAddress());
            object reply = null;
            node = clienT.nodeList.Values.Last();
            foreach( Client client in clientsOtherThanThis)
            {
                bool isRequestSendingOK = false;
                try
                {
                    Console.WriteLine("Client " + clients[index1].getIP()
                                        + " with timeStamp " + clients[index1].getClock()
                                        + " sending \"request\" to client " + client.getIP()
                                        + "\n");
                    result.Append("Client " + clients[index1].getIP()
                            + " with timeStamp " + clients[index1].getClock()
                            + " sending \"request\" to client " + client.getIP()
                            + "\n");
                    reply = node.sendmessage("request", clients[index1].getIP(), clients[index1].getClock(), client.getIP());
                    //if we have reached here request sending has been successful
                    isRequestSendingOK = true;
                }
                catch(Exception e)
                {
                    Console.WriteLine("Exception while executing the "
                                        + "RequestProcessor.sendMessage method for"
                                        + " each client other than this client");
                    result.Append("Exception while executing the "
                                        + "RequestProcessor.sendMessage method for"
                                        + " each client other than this client\n");
                }

                if (isRequestSendingOK)
                {
                    //cast reply to string
                    string replyString = (string)reply;


                    if (replyString.Contains(","))
                    {
                        //now split and get OK sender's timestamp
                        string[] splitComponents = replyString.Split(',');

                        if (splitComponents[0].Equals("OK", StringComparison.InvariantCultureIgnoreCase))
                        {
                            //update the logical clock, when received OK
                            //from the other node, because it is receive event
                            clients[index1].updateClock(int.Parse(splitComponents[1]));

                            //increment the OK count for this client
                            clients[index1].incrementOKCount();
                        } 

                    }

                }
            }

            //so,  number of OKCount is the same as number of clients
            //other than this. In other words, if received "OK" from
            //all clients that this client has sent the request to

            while (clients[index1].getOKCount() != clientsOtherThanThis.Length)
            {
                //wait
            }


            if (clients[index1].getOKCount() == clientsOtherThanThis.Length)
            {                
                //now this client can enter the critical section
                    //set the using flag for this client
                clients[index1].setUsing(true);


                Server.getInstance().assignToCriticalSection(clients[index1].getIP());
                // get the random English word
                string randomEnglishWord = Server.getInstance().generateRandomEnglishWord();

                    // method to write this generated word to
                    // client's
                    // buffer to check it later whether or not
                    // written
                    // word exists in the resulting master string
                clients[index1].rememberWord(randomEnglishWord);

                

                // now enter the critical section where the word
                // will be written
                // to the coordinator's master string

                // boolean variable to hold whether
                // criticalSection entrance was OK
                bool isCriticalSectionSuccess = false;

                    try
                    {
                        // params will contain the client's ip who
                        // will enter
                        // the critical section and and the
                        // randomEnglishWord
                        reply = node.enterCS(clients[index1].getIP(), randomEnglishWord);

                        // reply could be like
                        // "Node with ip has written some word"
                        Console.WriteLine((string)reply);
                        result.Append((string)reply);

                        //if we have reached here critical section completed successfully
                        isCriticalSectionSuccess = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception while calling method"
                                            + " RequestProcessor.enterCS\n");
                    }

                    if (isCriticalSectionSuccess)
                    {

                         // update clock when sending OK
                        clients[index1].updateClock();
                        int oksSent = 0;
                        int numberOfNodesOkToBeSent = clients[index1].getRequestQueue().Count();
                        //get the queue, send OK to all processes in own queue
                        //and empty queue.
                        foreach (string IP in clients[index1].getRequestQueue())
                        {
                            try
                            {
                                result.Append("Client " + clients[index1].getIP()
                                                    + " with timeStamp " + clients[index1].getClock()
                                                    + " sending \"OK\" to client " + IP
                                                    + "\n");
                                node.sendmessage("OK", clients[index1].getIP(), clients[index1].getClock(), IP);
                                oksSent++;
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Exception while executing the "
                                                    + "RequestProcessor.sendMessage method for"
                                                    + " each client other than this client");
                            }
                        
                        
                        }

                        //if all oks sent successfully
                        if (oksSent == numberOfNodesOkToBeSent)
                        {

                            Server.getInstance().removeFromCriticalSection(clients[index1].getIP());
                        //now empty queue, set using and wanted flags to false
                        //and resetOKCount to 0.
                            clients[index1].emptyRequestQueue();
                            clients[index1].setUsing(false);
                            clients[index1].setWanted(false);
                            clients[index1].resetOKCount();
                        }
                    }
             }
          
        }

        public string checkAddressTables()
        {
            StringBuilder sb = new StringBuilder("");

            // check whether the clients' address table has been updated
            foreach (Client cl in Server.getInstance().getClients())
            {

                sb.Append(getConvenientOutput());
                sb.Append("Client " + cl.getIP() + " address table:\n");

                foreach (string address in cl.getAddressTable())
                {
                    
                    if (cl.getCoordinatorIP().Equals(address))
                    {
                        sb.Append(address + " *COORDINATOR*" + "\n");
                    } 
                    else
                    {
                        sb.Append(address + "\n");
                    } 
                } 

                sb.Append(getConvenientOutput());
            } 

            return sb.ToString();

        }


    }
}
