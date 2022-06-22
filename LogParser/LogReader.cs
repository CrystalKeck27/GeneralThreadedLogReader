using System.Text;

namespace LogParser;

#pragma warning disable CS4014
public class LogReader : ILogReader {

    private string _name;
    private string _location;
    private FileStream _stream;
    private FileSystemWatcher _watcher;
    private int _position = 0;
    private bool _fileContinues = true;
    private LinkedList<string> _chunks;
    private LinkedListNode<string>? _chunk;
    private TaskCompletionSource _waiter;
    private bool _reading;

    //private int _chunksAhead = 0;

    public LogReader(string location, string fileName) {
        _name = fileName;
        _location = location;

        _chunks = new LinkedList<string>();

        _stream = new FileStream(
            _location + _name,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite,
            bufferSize: 4096,
            useAsync: true);

        _watcher = new FileSystemWatcher();
        _watcher.Path = _location;
        _watcher.NotifyFilter = NotifyFilters.Size |
                                NotifyFilters.CreationTime |
                                NotifyFilters.FileName |
                                NotifyFilters.LastWrite;
        //_watcher.Filter = _name;
        _watcher.Changed += OnChanged;
        _watcher.EnableRaisingEvents = true;
        _waiter = new TaskCompletionSource();
        ReadChunk();
    }

    private void OnChanged(object source, FileSystemEventArgs e) {
        _fileContinues = true;
        ReadChunk();
    }

    private async Task<bool> ReadChunk() {
        _reading = true;
        byte[] buffer = new byte[4096];

        int amount = await _stream.ReadAsync(buffer, 0, buffer.Length);
        if (amount == 0) {
            _fileContinues = false;
            _reading = false;
            return false;
        }

        _chunks.AddLast(Encoding.UTF8.GetString(buffer, 0, amount));
        _chunk ??= _chunks.Last;

        _waiter.SetResult();
        _waiter = new TaskCompletionSource();
        _reading = false;
        if (_chunk!.Next == null) ReadChunk();
        return true;
    }

    public char? NextChar() {
        if (_chunk == null) {
            if (!_reading && _fileContinues) ReadChunk();
            return null;
        }

        if (_position < _chunk.Value.Length) {
            char c = _chunk.Value[_position];
            _position++;
            return c;
        }
        if (!_reading && _fileContinues) ReadChunk();

        LinkedListNode<string>? next = _chunk.Next;
        if (next == null) {
            return null;
        }

        _chunk = next;

        _position = 1;
        return _chunk.Value[0];
    }

    // This returns true if it can skip over <amount> chars
    // NextChar is NOT guaranteed to be available 
    public bool SkipChars(int amount) {
        _position += amount;
        if (_position <= _chunk.Value.Length) {
            return true;
        }

        LinkedListNode<string>? next = _chunk.Next;
        if (next == null) {
            _position -= amount;
            return false;
        }

        _position -= _chunk.Value.Length;
        _chunk = next;
        return true;
    }

    public string[] ReadOut(int[] breaks) {
        throw new NotImplementedException();
    }

    // Completes at the same time ReadChunk() returns true
    public Task NextChunkReadyAsync() {
        return _waiter.Task;
    }
}