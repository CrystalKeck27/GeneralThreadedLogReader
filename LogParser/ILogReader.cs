namespace LogParser;

public interface ILogReader {
    public char? NextChar(); // returns null if no char available

    public bool SkipChars(int amount); // returns false if no char available

    public string[] ReadOut(int[] breaks);

    public Task NextChunkReadyAsync();
}