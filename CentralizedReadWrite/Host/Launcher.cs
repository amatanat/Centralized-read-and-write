using HelloService;
using System;
using System.IO;
using System.Linq;
namespace Host

{
    public class Launcher
    {
        static void Main(string[] args)
        {
            Client client = null;
            IRequestProcessor node;

            bool joinOK = false;
            bool signOffOK = false;

            Console.WriteLine("Enter server's socket address - IP:PORT ");

            string serverSocketAddress = Console.ReadLine();

            while(true) 
            {
                if (Utils.isValidSocketAddress(Utils.getIPFromSocketAddress(serverSocketAddress)))
                {
                    break;
                }
                else
                {
                    serverSocketAddress = Console.ReadLine();
                }
            }

            Server server = Server.getInstance(serverSocketAddress);
            server.start();


            while (true)
                {
                    Console.WriteLine("Press ENTER to continue...");

                    try
                    {
                        Console.ReadLine();
                        showCommands();
                    }
                    catch (IOException e)
                    {
                        server.stop();
                        break;
                    }

                int command = int.Parse(Console.ReadLine());

                switch (command)
                {
                    case 1:
                        {
                            if (server.isNetworkAvailable())
                            {
                                Console.WriteLine("Enter socket address of new host in form IP:PORT, that you want to join: ");
                                string socketAddress = Console.ReadLine();

                                while (true)
                                {
                                    if (Utils.isValidSocketAddress(Utils.getIPFromSocketAddress(socketAddress)))
                                    {

                                        if (checkHostEligibility(Utils.getIPFromSocketAddress(socketAddress)))
                                        {
                                            Console.WriteLine("Host is already in the network! Type in another IP:PORT ");
                                            socketAddress = Console.ReadLine();
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        socketAddress = Console.ReadLine();
                                }
                                }
                                try
                                {
                                    client = new Client(server.getSocketAddress(), socketAddress);
                                    
                                }
                                catch (Exception e)
                                {
                                    Console.Error.WriteLine("Error Setting the server url for this client.");
                                    client = null;
                                }


                                if (client != null)
                                {
                                    object response = null;
                                    beautifyOutput();
                                    try
                                    {
                                       
                                        node = client.nodeList.Values.Last();
                                        response = node.joinNode(client.getSocketAddress());
                                        Console.WriteLine(response);
                                        
                                        try
                                        {
                                           // propagatemessagetoall in java 
                                            response = node.propagateJoinRequest(client.getSocketAddress());
                                            Console.WriteLine(response);
                                            checkClients();
                                            beautifyOutput();
                                            joinOK = true;

                                        }
                                        catch (Exception e)
                                        {
                                            Console.Error.WriteLine("Failed to broadcast join request");
                                        }

                                    }
                                    catch (Exception e)
                                    {
                                        Console.Error.WriteLine("Error executing join request, response is null");
                                    }
                                    

                                    if (joinOK)
                                    {
                                        Console.WriteLine("Master node Election ");

                                        try
                                        {

                                            node = client.nodeList.Values.Last();
                                            response = node.startElection("join" + "," + client.getIP());

                                            if (response != null)
                                            {
                                                Console.WriteLine(response);

                                                //propagatemessagetoall in java
                                                response = node.propagateCoordinatorMessage(client.getIP());

                                                Console.WriteLine(response);
                                                beautifyOutput();
                                            }

                                            // Master election is done. because of it joinOK is false now.
                                            joinOK = false;
                                        }
                                        catch (Exception e)
                                        {
                                            Console.Error.WriteLine("Failed to broadcast coordinator message");
                                        }
                                    }

                                }
                                else
                                {
                                    Console.WriteLine("Try to correct server's socket address"
                                        + " or clients' socket address");
                                    break;
                                }
                            }
                            else
                            {
                                Console.WriteLine("Please first add your server to the network");
                            }
                            break;
                        }
                    case 2:
                        {
                            if(server.isNetworkAvailable())
                            {
                                bool isOneHostAvailable = false;

                                try
                                {
                                    client = new Client(server.getSocketAddress(),
                                            Utils.getDefaultSocketAddress());

                                    node = client.nodeList.Values.Last();
                                    object response = node.atLeastOneHostAvailable();
                                    isOneHostAvailable = (bool) response;
                                }
                                catch (Exception e1)
                                {
                                    Console.WriteLine("Exception in executing method "
                                            + "RequestProcessor.atLeastOneHostAvailable");
                                } 

                                if (isOneHostAvailable)
                                {
                                    showNetwork();
                                    Console.WriteLine("Enter the node ID to sign it off from the network:");
                                    int nodeIdToSignOff = int.Parse(Console.ReadLine());

                                    while (nodeIdToSignOff == 0)
                                    {
                                        Console.WriteLine("Server cannot be signed off from the network!"+ "\n");
                                        Console.WriteLine("Enter the node ID to sign it off from the network: ");
                                        nodeIdToSignOff = int.Parse(Console.ReadLine());
                                    }

                                    try
                                    { 
                                        client = new Client(server.getSocketAddress(),
                                                Utils.getDefaultSocketAddress());
                                    } 
                                    catch (Exception e)
                                    {
                                        Console.Error.WriteLine("Error Setting the server url for this client.");
                                        client = null;
                                    }

                                    if(client != null)
                                    {
                                        object response = null;

                                        beautifyOutput();

                                        try
                                        {
                                            node = client.nodeList.Values.Last();
                                            response = node.signOffNode(nodeIdToSignOff);
                                            Console.WriteLine(response);

                                            try
                                            {
                                                response = node.propagateSignOffRequest(nodeIdToSignOff);
                                                Console.WriteLine(response);

                                                beautifyOutput();
                                                signOffOK = true;
                                            }
                                            catch(Exception e)
                                            {
                                                Console.WriteLine("Failed to broadcast signoff request");
                                            }
                                        }
                                        catch(Exception e)
                                        {
                                            Console.WriteLine("Error executing signoff request, response is null");

                                        }

                                        if(signOffOK)
                                        {
                                            Console.WriteLine("Master Node Election: ");

                                            try
                                            { 
                                                node = client.nodeList.Values.Last();
                                                response = node.startElection("signOff" + "," + Utils.getDefaultIP());

                                                if (response != null)
                                                {
                                                    Console.WriteLine(response);
                                                    response = node.propagateCoordinatorMessage(Utils.getDefaultIP());
                                                    Console.WriteLine(response);

                                                    beautifyOutput();
                                                }

                                                signOffOK = false;

                                            }
                                            catch(Exception e)
                                            {
                                                Console.WriteLine("Exception in master election process ");
                                            }
                                        } 
                                    }
                                    else
                                    {
                                        Console.Error.WriteLine("Try to correct servers' socket address"
                                            + " or clients' socket address");
                                        break;
                                    }

                                }
                                else
                                {
                                    Console.WriteLine("There is no host in the network, try to join at least one");
                                }

                            }
                            else
                            {
                                Console.WriteLine("Please first add your server to the network");
                            }
                            break;
                        }
                    case 3:
                        {
                            showNetwork();
                            Console.WriteLine("Do you want see address table of each client(Y/N): ");
                            string prompt = Console.ReadLine();

                            //check correctness of prompt
                            while (!(prompt.Equals("Y", StringComparison.InvariantCultureIgnoreCase) || prompt.Equals("N", StringComparison.InvariantCultureIgnoreCase)))
                            {
                                Console.WriteLine("Enter just Y or N: ");
                                prompt = Console.ReadLine();
                            } 

                            if (prompt.Equals("Y", StringComparison.InvariantCultureIgnoreCase))
                            {
                                checkAddressTables();
                            }

                            break;
                        }
                    case 4:
                        {
                            if(server.isNetworkAvailable())
                            {
                                bool isOneHostAvailable = false;

                                try
                                {
                                    client = new Client(server.getSocketAddress(),
                                    Utils.getDefaultSocketAddress());

                                    node = client.nodeList.Values.Last();
                                    object response = node.atLeastOneHostAvailable() ;
                                    isOneHostAvailable = (bool) response;
                                }
                                catch(Exception e)
                                {
                                    Console.Error.WriteLine("Exception in executing method "
                                + "RequestProcessor.atLeastOneHostAvailable");
                                }

                                if(isOneHostAvailable) // then propagate start message to all nodes.
                                {

                                    object response = null;
                                    node = client.nodeList.Values.Last();
                                    try
                                    {
                                        response = node.startDistributedReadWrite();
                                        string reply = (string)response;
                                        beautifyOutput();
                                        Console.WriteLine(reply);
                                    }
                                    catch(Exception e)
                                    {
                                        Console.Error.WriteLine("Exception in executing method "
                                    + "RequestProcessor.startDistributedReadWrite");
                                    }

                                    showAlgorithms();

                                    Console.WriteLine("Enter synchronization algorithm ID to start: ");
                                    int algorithmID = int.Parse(Console.ReadLine());


                                    switch(algorithmID)
                                    {
                                        case 1:
                                            {
                                                //boolean variable to hold whether or not the algorithm worked
                                                bool isCeMutExOK = false;

                                                // we will start rpc call to do Centralized Mutual Exclusion method.
                                                //create temporarily client.
                                                try
                                                {
                                                    node = client.nodeList.Values.Last();
                                                    response = node.doCeMutEx();
                                                    //print out what is returned
                                                    //Console.WriteLine((string)response);
                                                    isCeMutExOK = true;
                                                }
                                                catch(Exception e)
                                                {
                                                    Console.Error.WriteLine("Exception in execution of RequestProcessor.doCeMutEx() method");
                                                }

                                                //After process has ended all the nodes read the final string from
                                                //master node and write it to the screen
                                                //Moreover they check if all words they added to the string
                                                //are present in the final string. The result of this check
                                                //is also written to the screen.

                                                if (isCeMutExOK)
                                                {
                                                    // we will need a temporary client to 
                                                    // execute readFinalString method
                                                    try
                                                    {
                                                        //execute readFinalString method on server's handler
                                                        response = node.readFinalString();
                                                        Console.WriteLine((string)response);

                                                    }
                                                    catch (Exception e)
                                                    {

                                                        Console.Error.WriteLine("Exception in execution of "
                                                                + "remote readFinalString() method");
                                                    } 
                                                }
                                                break;
                                            }
                                        case 2:
                                            { 
                                                //boolean variable to hold whether or not the algorithm worked
                                                bool isRicartAgrawalaOK = false;

                                                try
                                                {
                                                    node = client.nodeList.Values.Last();
                                                    response = node.doRicartAgrawala();
                                                    //Console.WriteLine((string)response);
                                                    isRicartAgrawalaOK = true;
                                                }
                                                catch(Exception e)
                                                {
                                                    Console.Error.WriteLine("Exception in execution of remote "
                                                            + "doRicartAgrawala() method");
                                                }

                                                //After process has ended all the nodes read the final string from
                                                //master node and write it to the screen
                                                //Moreover they check if all words they added to the string
                                                //are present in the final string. The result of this check
                                                //is also written to the screen.

                                                if (isRicartAgrawalaOK)
                                                {
                                                    try
                                                    {
                                                        response = node.readFinalString();
                                                        Console.WriteLine((string)response);
                                                    }
                                                    catch(Exception e)
                                                    {
                                                        Console.Error.WriteLine("Exception in execution of "
                                                            + "remote readFinalString() method");
                                                    }
                                                }
                                                    break;
                                            }
                                        default:
                                            {
                                                Console.WriteLine("Please enter correct algorithm id ");
                                                break;
                                            }
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("There is no Host in the network, try to join at least one. ");
                                }

                            }
                            else
                            {
                                Console.WriteLine("Please first add your server to the network. ");
                            }

                            break;
                        }
                    case 5:
                        {
                            server.stop();
                            Console.WriteLine("DONE");
                            Console.ReadLine();
                            return;
                        }
                        default:
                            {
                                break;
                            }
                    }
                }
            }
            
        

        private static void showCommands()
        {
            Console.WriteLine("Enter 1 - to join the virtual network, you will be asked for host address and port");
            Console.WriteLine("Enter 2 - to sign off from virtual network");
            Console.WriteLine("Enter 3 - to show your virtual network status");
            Console.WriteLine("Enter 4 - to start distributed read write operations, can be executed only if there is a host in the network");
            Console.WriteLine("Enter 5 - to exit and stop the program");
        }

        /// <summary>
        /// This method shows available synchronization algorithms.
        /// </summary>
        private static void showAlgorithms()
        {
            beautifyOutput();
            Console.WriteLine("1 Centralized Mutual Exclusion ");
            Console.WriteLine("2 Ricart & Agrawala");
            beautifyOutput();
        }



        private static void beautifyOutput()
        {
           Console.WriteLine("--------------------------------------------------------------");
        }

        /// <summary>
        /// Shows each node with IP address.
        /// </summary>
        private static void checkClients()
        {
            foreach (Client cl in Server.getInstance().getClients())
            {
               Console.WriteLine("Client " + cl.getIP());
            } 
        }

        
        /// <summary>
        /// This method shows all the nodes in the network with specified ids.
        /// </summary>
        public static void showNetwork()
        {
            object response = null;
            try
            {
                //temporary client for executing showNetworkState message
                Client client = new Client(Server.getInstance().getSocketAddress(),
                        Utils.getDefaultSocketAddress());

                //execute showNetworkState method on server's handler
                IRequestProcessor cl = client.nodeList.Values.Last();
                response = cl.showNetworkState();

                if (response != null)
                {
                    Console.WriteLine(response);
                } 

            }
            catch (Exception e)
            {

                Console.Error.WriteLine("Exception in execution of remote showNetworkState() method");
            } 
            beautifyOutput();
        }


        /// <summary>
        /// This methoc checks host's ip address's eligibility
        /// </summary>
        /// <param name="IP">Address that user enters</param>
        /// <returns></returns>
        private static bool checkHostEligibility(string IP)
        {
            // check whether or not there is at least one host in the
            // network
            bool isHostWithGivenIPEligible = false;

            // create the temporary client with server socket address and
            // its own socket address and do remote procedure call
            try
            {
                Client client = new Client(Server.getInstance().getSocketAddress(),
                        Utils.getDefaultSocketAddress());

                //send the given ip to request processor to check whether
                //the host with given ip already exists, if exists
                //response will false

                IRequestProcessor node = client.nodeList.Values.Last();
                object response = node.checkHostEligibility(IP);
                isHostWithGivenIPEligible = (bool)response;
            }
            catch (Exception e1)
            {
                Console.WriteLine("Exception in executing method "
                        + "RequestProcessor.checkHostEligibility");
            } 
            return isHostWithGivenIPEligible;

        }

        /// <summary>
        /// Helper method to detect whether the address tables have been updated.
        /// </summary>
        private static void checkAddressTables()
        {

            // create the temporary client with server socket address and
            // its own socket address and do remote procedure call
            try
            {
                Client client = new Client(Server.getInstance().getSocketAddress(),
                        Utils.getDefaultSocketAddress());
                IRequestProcessor node = client.nodeList.Values.Last();

                // send the given ip to request processor to check whether
                // the host with given ip already exists, if exists
                // response will be false
                object response = node.checkAddressTables();
                Console.WriteLine((string)response);
            } 
            catch (Exception e1)
            {
                Console.Error.WriteLine("Exception in executing method "
                        + "RequestProcessor.checkAddressTables");
            }

        }
    }
}
