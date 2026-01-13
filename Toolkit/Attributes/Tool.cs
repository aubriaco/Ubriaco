namespace Ubriaco.Toolkit;

public class Tool : Attribute
{
    public string CMD { get; set; }

    public Tool(string cmd)
    {
        CMD = cmd;
    }
}