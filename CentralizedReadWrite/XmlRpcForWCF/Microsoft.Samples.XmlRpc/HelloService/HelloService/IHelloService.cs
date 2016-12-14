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
    }
}
