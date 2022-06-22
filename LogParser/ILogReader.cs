namespace LogParser;

public interface ILogReader {
    public char NextChar(); // Waits Sync until chars available

    public void SkipChars(int amount); // Waits Sync until chars available

    public string ReadOut(int count);
}