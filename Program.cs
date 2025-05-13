using pr_2_ser;

class Program
{
    static async Task Main()
    {
        var server = new FileServer(5050, "ReceivedFiles");
        await server.StartAsync();
    }
}
