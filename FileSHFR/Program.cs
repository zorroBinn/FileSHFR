using System;
using System.IO;
using System.Linq;

namespace FileSHFR
{
    internal class FileSHFR
    {
        private const int BufferSize = 4 * 1024 * 1024; //4MB

        static void Main(string[] args)
        {
            try
            {
                if (args.Length < 3)
                    throw new ArgumentException("Недостаточно аргументов. Формат:\n" +
                        "Шифрование: App.exe -e <файл> <номер_алгоритма>\n" +
                        "Дешифровка: App.exe -d <файл.shfr> <расширение> <номер_алгоритма>");

                string mode = args[0];
                string filePath = args[1];
                int algorithmId = int.Parse(args[args.Length - 1]);

                if (mode == "-e")
                {
                    Encrypt(filePath, algorithmId);
                }
                else if (mode == "-d" && args.Length >= 4)
                {
                    string extension = args[2];
                    Decrypt(filePath, extension, algorithmId);
                }
                else
                {
                    throw new ArgumentException("Некорректные аргументы.");
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Ошибка: {ex.Message}");
                Environment.Exit(1);
            }
        }

        static void Encrypt(string inputPath, int algorithmId)
        {
            string dir = Path.GetDirectoryName(inputPath);
            if (dir == null) 
            {
                throw new ArgumentException("Не удалось определить директорию файла.");
            }
            string outputPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(inputPath) + ".shfr");

            if (algorithmId == 0)
            {
                ReverseFile(inputPath, outputPath);
            }
            else
            {
                ProcessStream(inputPath, outputPath, algorithmId);
            }

            File.Delete(inputPath);
        }

        static void Decrypt(string inputPath, string extension, int algorithmId)
        {
            if (Path.GetExtension(inputPath) != ".shfr")
                throw new ArgumentException("Дешифровка требует файл с расширением .shfr");

            string dir = Path.GetDirectoryName(inputPath);
            if (dir == null) 
            {
                throw new ArgumentException("Не удалось определить директорию файла.");
            }
            string outputPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(inputPath) + "." + extension.TrimStart('.'));

            if (algorithmId == 0)
            {
                ReverseFile(inputPath, outputPath);
            }
            else
            {
                ProcessStream(inputPath, outputPath, algorithmId);
            }

            File.Delete(inputPath);
        }

        static void ProcessStream(string inputPath, string outputPath, int algorithmId)
        {
            using (FileStream input = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            using (FileStream output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = new byte[BufferSize];
                int bytesRead;

                while ((bytesRead = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    byte[] actualData = buffer.Take(bytesRead).ToArray();
                    byte[] processed = ApplyCipher(actualData, algorithmId);
                    output.Write(processed, 0, processed.Length);
                }
            }
        }

        static void ReverseFile(string inputPath, string outputPath)
        {
            using (FileStream input = new FileStream(inputPath, FileMode.Open, FileAccess.Read))
            using (FileStream output = new FileStream(outputPath, FileMode.Create, FileAccess.Write))
            {
                long length = input.Length;
                byte[] buffer = new byte[BufferSize];

                for (long pos = length; pos > 0;)
                {
                    int readSize = (int)Math.Min(BufferSize, pos);
                    pos -= readSize;

                    input.Seek(pos, SeekOrigin.Begin);
                    input.Read(buffer, 0, readSize);

                    Array.Reverse(buffer, 0, readSize);
                    output.Write(buffer, 0, readSize);
                }
            }
        }

        private static byte[] ApplyCipher(byte[] data, int algorithmId)
        {
            switch (algorithmId)
            {
                case 1: return SwapOddEvenBytes(data);
                case 2: return XorCipher(data, 0x55);
                default: throw new ArgumentException("Алгоритм не поддерживает потоковую обработку или неизвестен.");
            }
        }

        private static byte[] SwapOddEvenBytes(byte[] data)
        {
            byte[] result = new byte[data.Length];
            int i = 0;
            while (i < data.Length)
            {
                if (i + 1 < data.Length)
                {
                    result[i] = data[i + 1];
                    result[i + 1] = data[i];
                    i += 2;
                }
                else
                {
                    result[i] = data[i];
                    i++;
                }
            }
            return result;
        }

        private static byte[] XorCipher(byte[] data, byte key)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                result[i] = (byte)(data[i] ^ key);
            }
            return result;
        }
    }
}
