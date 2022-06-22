using System;

namespace LogParser {
    internal class Parser {

        private IProcessCommunicator _communicator;
        private ILogReader _reader;
        
        private Parser(IProcessCommunicator communicator, ILogReader reader) {
            _communicator = communicator;
            _reader = reader;
            
            ParseLoop();
            Console.ReadLine();
        }
        
        private static void Main(string[] args) {
            LogReader reader = new LogReader("C:\\Users\\KECKAX\\Documents\\", "SampleLog.log");
            VoidProcessCommunicator communicator = new VoidProcessCommunicator();
            Parser parser = new Parser(communicator, reader);
        }

        // This never stops, call once in constructor
        private async void ParseLoop() {
            while (true) {
                char? c = _reader.NextChar();
                if (c == null) {
                    await _reader.NextChunkReadyAsync();
                    c = _reader.NextChar();
                }
                Console.Write(c);
            }
        }
    }
}