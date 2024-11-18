using ICSharpCode.SharpZipLib.GZip;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace WebContentToCode
{
    enum Encoding
    {
        none,
        gzip
    }

    partial struct Config
    {
        public Encoding Encoding = Encoding.none;
        public List<string> FileExtensions = [];
        public string outputFileName = "webFiles.h";
        public bool usePROGMEM = false;
        public bool allowRecursiveProcessing = false;
        public bool usePragmaOnce = true;

        [GeneratedRegex("[-.]")]
        public static partial Regex caseUnifyingRegex();
        public const string caseUnifier = "_";
        public bool unifyCase = false;

        public Config(string[] args)
        {
            string currentOption = string.Empty;
            foreach (string arg in args)
            {
                if (arg.StartsWith('-'))
                {
                    currentOption = arg;

                    //Flags (must use continue)
                    switch (currentOption)
                    {
                        case "-progmem":
                            usePROGMEM = true;
                            break;
                        case "-r":
                            allowRecursiveProcessing = true;
                            break;
                        case "-noPragma":
                            usePragmaOnce = false;
                            break;
                        case "-uc":
                            unifyCase = true;
                            break;
                        case "-f":
                        case "-e":
                        case "-o":
                            break;
                        default:
                            throw new("Unknown option: " + arg);
                    }
                    continue;
                }

                //Option parameters
                switch (currentOption)
                {
                    case "-f":
                        FileExtensions.Add(arg);
                        break;
                    case "-e":
                        if (Enum.TryParse(arg.ToLower(), out Encoding encoding))
                        {
                            Encoding = encoding;
                            currentOption = string.Empty; //Reset current option (single arg)
                            break;
                        }
                        throw new("Unknown encoding: " + arg);
                    case "-o":
                        outputFileName = arg;
                        currentOption = string.Empty; //Reset current option (single arg)
                        break;
                    default:
                        throw new("Unknown parameter: " + arg);
                }
            }
        }
    }

    internal class Program
    {
        private static Config config;

        private static byte[] GZipCompress(byte[] inputBytes)
        {
            using Stream memOutput = new MemoryStream();
            using GZipOutputStream zipOut = new(memOutput);
            zipOut.Write(inputBytes);
            zipOut.Flush();
            zipOut.Finish();

            byte[] bytes = new byte[memOutput.Length];
            memOutput.Seek(0, SeekOrigin.Begin);
            memOutput.Read(bytes, 0, bytes.Length);
            return bytes;
        }

        static void Main(string[] args)
        {
            config = new(args);
            List<(string, byte[])> convertedFiles = [];

            //Metrics
            Stopwatch sw = Stopwatch.StartNew();
            int originalByteCount = 0;

            string currentDir = Directory.GetCurrentDirectory();
            foreach (string file in Directory.EnumerateFiles(currentDir, "*.*", SearchOption.AllDirectories))
            {
                if (config.allowRecursiveProcessing || Path.GetFileName(file) != config.outputFileName
                    && (config.FileExtensions.Count == 0 || config.FileExtensions.Any(file.EndsWith)))
                {
                    Console.WriteLine("Found file: " + file);

                    byte[] bytes = File.ReadAllBytes(file);
                    originalByteCount += bytes.Length;

                    switch (config.Encoding)
                    {
                        case Encoding.gzip:
                            convertedFiles.Add((file, GZipCompress(bytes)));
                            break;
                        default:
                            convertedFiles.Add((Path.GetFileName(file), bytes));
                            break;
                    }
                }
            }

            int compressedByteCount = convertedFiles.Sum(x => x.Item2.Length);
            float compressionPercent = MathF.Round(compressedByteCount / (float)originalByteCount * 100, 2);
            Console.WriteLine($"Writing {compressedByteCount}/{originalByteCount} ({compressionPercent}%) bytes to \"{config.outputFileName}\"...");

            //Write to output file (this is so readable)
            using (StreamWriter outFile = new(config.outputFileName))
            {
                if (config.usePragmaOnce)
                {
                    outFile.WriteLine("#pragma once\n");
                }

                outFile.WriteLine("//This file was generated with WCTC. Do not change.\n");

                foreach (var file in convertedFiles)
                {
                    string fileNameNoExtension = Path.GetFileNameWithoutExtension(file.Item1);
                    string fileName = config.unifyCase ? Config.caseUnifyingRegex().Replace(fileNameNoExtension, Config.caseUnifier) : fileNameNoExtension;

                    outFile.Write($"const uint8_t {fileName}_{Path.GetExtension(file.Item1)[1..]}[] {(config.usePROGMEM ? "PROGMEM " : string.Empty)}= {{ ");

                    for (int i = 0; i < file.Item2.Length; i++)
                    {
                        outFile.Write("0x" + file.Item2[i].ToString("X2").ToLower() + (i != file.Item2.Length - 1 ? "," : string.Empty));
                    }

                    outFile.WriteLine(" };\n");
                }
            }

            Console.WriteLine($"Finished in {sw.Elapsed.TotalSeconds} seconds");
        }
    }
}
