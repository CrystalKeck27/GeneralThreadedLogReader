using System;

namespace LogParser {
    internal class Parser {

        private IProcessCommunicator _communicator;
        private ILogReader _reader;

        private Parser(IProcessCommunicator communicator, ILogReader reader) {
            _communicator = communicator;
            _reader = reader;

            ParseLoop();
        }

        private static void Main(string[] args) {
            LogReader reader = new LogReader("C:\\Users\\KECKAX\\Documents\\", "bleh.log");
            VoidProcessCommunicator communicator = new VoidProcessCommunicator();
            Parser parser = new Parser(communicator, reader);
        }

        // This never stops, call once in constructor
        private void ParseLoop() {
            int i = 0;
            while (true) {
                char c = _reader.NextChar();
                i++;
                if (c == '\n') {
                    Console.WriteLine(_reader.ReadOut(i));
                    i = 0;
                }
            }
        }
    }
}