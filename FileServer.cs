using System.Net;
using System.Net.Sockets;

namespace pr_2_ser
{
    public class FileServer
    {
        private readonly int _port;
        private readonly string _storagePath;
        private readonly TcpListener _listener;
        private int _activeClients = 0;
        private readonly object _lock = new();

        public FileServer(int port, string storagePath)
        {
            _port = port;
            _storagePath = storagePath;
            _listener = new TcpListener(IPAddress.Any, _port);
        }

        public async Task StartAsync()
        {
            Directory.CreateDirectory(_storagePath);
            _listener.Start();
            Console.WriteLine($"Сервер запущен на порту {_port}...");

            while (true)
            {
                TcpClient client = await _listener.AcceptTcpClientAsync();
                lock (_lock) _activeClients++;
                Console.WriteLine($"[Подключение] Клиент подключился. Активных: {_activeClients}");

                _ = Task.Run(async () =>
                {
                    await new ClientHandler(_storagePath, DecreaseClientCount).HandleAsync(client);
                });
            }
        }

        private void DecreaseClientCount()
        {
            lock (_lock) _activeClients--;
            Console.WriteLine($"[Отключение] Клиент отключился. Активных: {_activeClients}");
        }
    }
}
