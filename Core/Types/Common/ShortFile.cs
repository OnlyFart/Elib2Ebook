namespace Core.Types.Common;

public class ShortFile {
    public string Name { get; set; }
    public byte[] Bytes { get; set; }

    public ShortFile(string name, byte[] bytes) {
        Name = name;
        Bytes = bytes;
    }
}