using System.Net.Sockets;
using System.Text;

namespace pr_2_ser
{
    public class ClientHandler
    {
        private readonly string _storagePath;
        private readonly Action _onDisconnect;

        public ClientHandler(string storagePath, Action onDisconnect)
        {
            _storagePath = storagePath;
            _onDisconnect = onDisconnect;
        }

        public async Task HandleAsync(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true))
            using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true))
            {
                try
                {
                    var processor = new FileProcessor(_storagePath);
                    string filePath = await processor.ReceiveFileAsync(reader);
                    string result = await processor.AnalyzeFileAsync(filePath);
                    await processor.SendResponseAsync(writer, result);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Ошибка] {ex.Message}");
                    await new FileProcessor(_storagePath).SendResponseAsync(writer, "Ошибка при обработке файла.");
                }
                finally
                {
                    _onDisconnect?.Invoke();
                }
            }
        }
    }
}
