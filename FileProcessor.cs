using System.Text;

namespace pr_2_ser
{
    public class FileProcessor
    {
        private readonly string _storagePath;
        private readonly string _analysisLogFile;

        public FileProcessor(string storagePath)
        {
            _storagePath = storagePath;
            _analysisLogFile = Path.Combine(storagePath, "analysis_result.txt");
        }

        public async Task<string> ReceiveFileAsync(BinaryReader reader)
        {
            int nameLength = reader.ReadInt32();
            string fileName = Encoding.UTF8.GetString(reader.ReadBytes(nameLength));

            int contentLength = reader.ReadInt32();
            byte[] fileBytes = reader.ReadBytes(contentLength);

            string filePath = Path.Combine(_storagePath, $"{Guid.NewGuid()}_{fileName}");
            await File.WriteAllBytesAsync(filePath, fileBytes);
            return filePath;
        }

        public async Task<string> AnalyzeFileAsync(string filePath)
        {
            string[] lines = await File.ReadAllLinesAsync(filePath, Encoding.UTF8);

            int lineCount = lines.Count(l => !string.IsNullOrWhiteSpace(l));
            int wordCount = lines.Sum(l => l.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
            int charCount = lines.Sum(l => l.Length);

            string result = $"Файл: {Path.GetFileName(filePath)}\nСтрок: {lineCount}, Слов: {wordCount}, Символов: {charCount}";
            await File.AppendAllTextAsync(_analysisLogFile, result + "\n");

            return result;
        }
        public Task SendResponseAsync(BinaryWriter writer, string message)
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            writer.Write(data.Length);
            writer.Write(data);
            writer.Flush();
            return Task.CompletedTask;
        }

    }
}
