namespace KBot;

public static class Program
{
    private static void Main()
    {
        new Bot().StartAsync().GetAwaiter().GetResult();
    }
}