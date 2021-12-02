namespace KBot
{
    public class Program
    {
        static void Main()
        {
            new Bot().StartAsync().GetAwaiter().GetResult();
        }
    }
}