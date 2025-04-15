using System.Net;
using System.Net.Sockets;
using System.Text;

namespace pr_2_ser
{
    public class FileServer
    {
        private readonly int _port;
        private readonly string _storagePath;
        private readonly string _analysisFile;
        private readonly TcpListener _listener;
        private int _activeClients = 0;
        private readonly object _lockObject = new object();

        public FileServer(int port = 5050, string storagePath = "ReceivedFiles")
        {
            _port = port;
            _storagePath = storagePath;
            _analysisFile = Path.Combine(_storagePath, "analysis_result.txt");
            _listener = new TcpListener(IPAddress.Any, _port);
        }

        public async Task StartAsync()
        {
            if (!Directory.Exists(_storagePath))
                Directory.CreateDirectory(_storagePath);

            _listener.Start();
            Console.WriteLine($"Сервер запущен на порту {_port}...");

            while (true)
            {
                try
                {
                    TcpClient client = await _listener.AcceptTcpClientAsync();
                    lock (_lockObject) { _activeClients++; }
                    Console.WriteLine($"[Подключение] Клиент подключился. Активных: {_activeClients}");

                    _ = HandleClientAsync(client); // запускаем без ожидания
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Ошибка] Ошибка при принятии подключения: {ex.Message}");
                }
            }
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            using (client)
            using (NetworkStream stream = client.GetStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true })
            {
                try
                {
                    string fileName = await reader.ReadLineAsync();
                    if (fileName == null)
                    {
                        Console.WriteLine("[Отключение] Клиент отключился до отправки данных.");
                        return;
                    }

                    string filePath = Path.Combine(_storagePath, $"{Guid.NewGuid()}_{fileName}");

                    using (FileStream fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
                    using (StreamWriter fileWriter = new StreamWriter(fs, Encoding.UTF8))
                    {
                        string line;
                        while ((line = await reader.ReadLineAsync()) != "EOF")
                        {
                            if (line == null)
                            {
                                Console.WriteLine("[Отключение] Клиент неожиданно разорвал соединение.");
                                return;
                            }
                            await fileWriter.WriteLineAsync(line);
                        }
                        await fileWriter.FlushAsync();
                    }

                    string analysis = await AnalyzeFileAsync(filePath);
                    await File.AppendAllTextAsync(_analysisFile, analysis + "\n");
                    await writer.WriteLineAsync(analysis);
                    Console.WriteLine($"[Файл] Файл '{fileName}' сохранен и обработан.");
                }
                catch (IOException)
                {
                    Console.WriteLine("[Ошибка] Клиент неожиданно разорвал соединение.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Ошибка] Ошибка при обработке файла: {ex.Message}");
                    await writer.WriteLineAsync("Ошибка при обработке файла.");
                }
                finally
                {
                    lock (_lockObject) { _activeClients--; }
                    Console.WriteLine($"[Отключение] Клиент отключился. Активных: {_activeClients}");
                }
            }
        }

        private async Task<string> AnalyzeFileAsync(string filePath)
        {
            string[] lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);

            int lineCount = lines.Count(line => !string.IsNullOrWhiteSpace(line));
            int wordCount = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Sum(line => line.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            int charCount = lines
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .Sum(line => line.Length);

            return $"Файл: {Path.GetFileName(filePath)}\nСтрок: {lineCount}, Слов: {wordCount}, Символов: {charCount}";
        }
    }
}
