using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Management;

namespace FileSHFR
{
    internal class FileSHFR
    {
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
            byte[] data = File.ReadAllBytes(inputPath);
            byte[] encryptedData = ProcessInParallel(data, algorithmId);

            string directory = Path.GetDirectoryName(inputPath);
            if (directory == null)
            {
                throw new ArgumentException("Не удалось определить директорию исходного файла.");
            }
            string outputFileName = Path.Combine(directory, Path.GetFileNameWithoutExtension(inputPath) + ".shfr");

            File.WriteAllBytes(outputFileName, encryptedData);
            File.Delete(inputPath);
        }

        static void Decrypt(string inputPath, string extension, int algorithmId)
        {
            if (Path.GetExtension(inputPath) != ".shfr")
            {
                throw new ArgumentException("Дешифровка требует файл с расширением .shfr");
            }

            byte[] data = File.ReadAllBytes(inputPath);
            byte[] decryptedData = ProcessInParallel(data, algorithmId);

            string directory = Path.GetDirectoryName(inputPath);
            if (directory == null) 
            { 
                throw new ArgumentException("Не удалось определить директорию исходного файла."); 
            }

            string baseName = Path.Combine(directory, Path.GetFileNameWithoutExtension(inputPath) + "." + extension.TrimStart('.'));

            File.WriteAllBytes(baseName, decryptedData);
            File.Delete(inputPath);
        }

        private static byte[] ProcessInParallel(byte[] data, int algorithmId)
        {
            int coreCount = GetPhysicalCoreCount();
            int chunkSize = data.Length / coreCount + (data.Length % coreCount == 0 ? 0 : 1);
            byte[][] chunks = Enumerable.Range(0, coreCount).Select(i => data.Skip(i * chunkSize).Take(chunkSize).ToArray()).ToArray();

            Parallel.For(0, coreCount, i => 
            {
                chunks[i] = ApplyCipher(chunks[i], algorithmId);
            });

            return chunks.SelectMany(chunk => chunk).ToArray();
        }

        private static int GetPhysicalCoreCount()
        {
            try
            {
                int count = 0;
                using (var searcher = new ManagementObjectSearcher("select NumberOfCores from Win32_Processor"))
                {
                    foreach (var item in searcher.Get())
                    {
                        count += int.Parse(item["NumberOfCores"].ToString());
                    }
                }
                return Math.Max(1, count);
            }
            catch
            {
                return Environment.ProcessorCount / 2;
            }
        }

        private static byte[] ApplyCipher(byte[] data, int algorithmId)
        {
            switch (algorithmId)
            {
                case 0: return ReverseBytes(data);
                case 1: return SwapOddEvenBytes(data);
                case 2: return XorCipher(data, key: 0x55);
                default: throw new ArgumentException("Неизвестный номер алгоритма.");
            }
        }

        private static byte[] ReverseBytes(byte[] data)
        {
            byte[] result = new byte[data.Length];
            Array.Copy(data, result, data.Length);
            Array.Reverse(result);
            return result;
        }

        private static byte[] SwapOddEvenBytes(byte[] data)
        {
            byte[] result = new byte[data.Length];
            for (int i = 0; i < data.Length; i++)
            {
                if (i % 2 == 0 && i + 1 < data.Length)
                {
                    result[i] = data[i + 1];
                    result[i + 1] = data[i];
                    i++;
                }
                else
                {
                    result[i] = data[i];
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
