using System.Runtime.CompilerServices;
using System.ServiceModel;

namespace HelloService
{
    [ServiceContract]
    public interface IRequestProcessor
    {
        [OperationContract (Action = "RequestProcessor.joinNode")]
        string joinNode(string nodeSocketAddress);

        [OperationContract(Action = "RequestProcessor.signOffNode")]
        string signOffNode(int nodeSocketAddress);

        [OperationContract(Action = "RequestProcessor.propagateSignOffRequest")]
        string propagateSignOffRequest(int nodeID);

        [OperationContract(Action = "RequestProcessor.propagateJoinRequest")]
        string propagateJoinRequest(string nodeSocketAddress);

        [OperationContract(Action = "RequestProcessor.startElection")]
        string startElection(string startedIP);

        [OperationContract(Action = "RequestProcessor.propagateCoordinatorMessage")]
        string propagateCoordinatorMessage(string starterIP);

        [OperationContract(Action = "RequestProcessor.showNetworkState")]
        string showNetworkState();

        [OperationContract(Action = "RequestProcessor.checkHostEligibility")]
        bool checkHostEligibility(string IP);

        [OperationContract(Action = "RequestProcessor.atLeastOneHostAvailable")]
        bool atLeastOneHostAvailable();

        [OperationContract(Action = "RequestProcessor.startDistributedReadWrite")]
        string startDistributedReadWrite();

        [OperationContract(Action = "RequestProcessor.doCeMutEx")]
        string doCeMutEx();

        [OperationContract(Action = "RequestProcessor.sendMessage")]
        [MethodImpl(MethodImplOptions.Synchronized)]
        string sendMessage(string message, string requesterIP);

        [OperationContract(Action = "RequestProcessor.readFinalString")]
        string readFinalString();

        [OperationContract(Action = "RequestProcessor.enterCS")]
        [MethodImpl(MethodImplOptions.Synchronized)]
        string enterCS(string entererIP, string word);

        [OperationContract(Action = "RequestProcessor.sendmessage")]
        [MethodImpl(MethodImplOptions.Synchronized)]
        string sendmessage(string message, string requesterIP,int requesterTimestamp, string receiverIP);

        [OperationContract(Action = "RequestProcessor.doRicartAgrawala")]
        string doRicartAgrawala();

        [OperationContract(Action = "RequestProcessor.checkAddressTables")]
        string checkAddressTables();
    }
}
