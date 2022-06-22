namespace LogParser; 

public class VoidProcessCommunicator : IProcessCommunicator {

    public void SendLine() {
        Console.WriteLine("Message");
    }
}