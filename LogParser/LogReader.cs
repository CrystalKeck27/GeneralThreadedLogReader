using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

namespace LogParser;

#pragma warning disable CS4014
public class LogReader : ILogReader {

    private string _name;
    private string _location;
    private int _start = 0; // Used in readOut
    private int _position = 0;

    private readonly ConcurrentQueue<string> _chunksConcurrent;
    private readonly ManualResetEvent _readerWait, _chunkAvailable, _readAvailable;

    private LinkedList<string> _chunks;
    private LinkedListNode<string>? _chunk;


    public LogReader(string location, string fileName) {
        _name = fileName;
        _location = location;

        _chunksConcurrent = new ConcurrentQueue<string>();
        _readerWait = new ManualResetEvent(false);
        _chunkAvailable = new ManualResetEvent(false);
        _readAvailable = new ManualResetEvent(false);

        _chunks = new LinkedList<string>();

        FileSystemWatcher watcher = new FileSystemWatcher();
        watcher.Path = _location;
        watcher.NotifyFilter = NotifyFilters.Size |
                               NotifyFilters.CreationTime |
                               NotifyFilters.FileName |
                               NotifyFilters.LastWrite;
        watcher.Filter = _name;
        watcher.Changed += OnChanged;
        watcher.EnableRaisingEvents = true;

        Thread readerThread = new Thread(() => ReadThread(_location + _name));
        readerThread.Start();
    }

    private void ReadThread(string path) {
        byte[] buffer = new byte[4096];
        FileStream stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            useAsync: false);

        while (true) {
            int amount = stream.Read(buffer, 0, 4096);
            while (amount == 0) {
                // Uncomment this line for profiling
                // Environment.Exit(0);
                // Comment this line for actual use
                _readAvailable.WaitOne();
                amount = stream.Read(buffer, 0, 4096);
            }

            _chunksConcurrent.Enqueue(Encoding.UTF8.GetString(buffer, 0, amount));
            _chunkAvailable.Set();
            _chunkAvailable.Reset();

            if (_chunksConcurrent.Count > 2) {
                _readerWait.WaitOne();
            }
        }
    }

    private void OnChanged(object source, FileSystemEventArgs e) {
        _readAvailable.Set();
        _readAvailable.Reset();
    }

    private void NextChunk() {
        while (_chunksConcurrent.IsEmpty) {
            _chunkAvailable.WaitOne();
        }

        string? chunk;
        if (!_chunksConcurrent.TryDequeue(out chunk)) {
            throw new Exception("TryDequeue Failed");
        }

        _readerWait.Set();
        _readerWait.Reset();
        _chunks.AddLast(chunk);
        _chunk = _chunks.Last;
        _position = 0;
    }

    public char NextChar() {
        if (_chunk == null || _position >= _chunk!.Value.Length) {
            NextChunk();
        }

        char c = _chunk!.Value[_position];
        _position++;
        return c;
    }

    // This returns true if it can skip over <amount> chars
    // NextChar is NOT guaranteed to be available 
    public void SkipChars(int amount) {
        _position += amount;
        if (_chunk == null || _position >= _chunk!.Value.Length) {
            NextChunk();
        }
    }

    public string ReadOut(int count) {
        int end = _start + count;
        string s = "";
        Debug.Assert(_chunks.First != null, "_chunks.First != null");
        LinkedListNode<string> chunk = _chunks.First;
        while (end > chunk.Value.Length) {
            s += chunk.Value[_start..];
            _start = 0;
            end -= chunk.Value.Length - _start;
            Debug.Assert(chunk.Next != null, "chunk.Next != null");
            chunk = chunk.Next;
            _chunks.RemoveFirst();
        }

        s += chunk.Value[_start..end];
        _start = end;
        return s;
    }
}