﻿using ICSharpCode.SharpZipLib.GZip;
using System.Diagnostics;
using System.Text;

namespace WebContentToCode
{
    enum Encoding
    {
        none,
        gzip
    }

    struct Config
    {
        public Encoding Encoding = Encoding.none;
        public List<string> FileExtensions = [];
        public string outputFileName = "webFiles.h";
        public bool usePROGMEM = false;
        public bool allowRecursiveProcessing = false;

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
                        case "-p":
                            usePROGMEM = true;
                            break;
                        case "-r":
                            allowRecursiveProcessing = true;
                            break;
                        default:
                            break;
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

        private static byte[] GZipCompress(byte[] text)
        {
            using Stream memOutput = new MemoryStream();
            using GZipOutputStream zipOut = new(memOutput);
            zipOut.Write(text);
            zipOut.Flush();
            zipOut.Finish();

            byte[] bytes = new byte[memOutput.Length];
            memOutput.Seek(0, SeekOrigin.Begin);
            memOutput.Read(bytes, 0, bytes.Length);

            return bytes;
        }

        private static string Dump(byte[] bytes)
        {
            StringBuilder sb = new();

            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append("0x" + bytes[i].ToString("X2").ToLower());

                if (i != bytes.Length - 1)
                {
                    sb.Append(',');
                }
            }

            return sb.ToString();
        }

        private static IEnumerable<string> ToArrayDefinitions(List<(string, byte[])> convertedFiles)
        {
            foreach (var file in convertedFiles)
            {
                yield return $"const uint8_t {Path.GetFileNameWithoutExtension(file.Item1)}_{Path.GetExtension(file.Item1)[1..]}[] {(config.usePROGMEM ? "PROGMEM " : string.Empty)}= {{ {Dump(file.Item2)} }};\n";
            }
        }

        static void Main(string[] args)
        {
            config = new(args);
            List<(string, byte[])> convertedFiles = [];

            Stopwatch sw = Stopwatch.StartNew();

            string currentDir = Directory.GetCurrentDirectory();
            foreach (string file in Directory.EnumerateFiles(currentDir, "*.*", SearchOption.AllDirectories))
            {
                if (config.allowRecursiveProcessing || Path.GetFileName(file) != config.outputFileName && 
                    (config.FileExtensions.Count == 0 
                    || config.FileExtensions.Any(file.EndsWith)))
                {
                    Console.WriteLine("Found file: " + file);

                    switch (config.Encoding)
                    {
                        case Encoding.gzip:
                            convertedFiles.Add((file, GZipCompress(File.ReadAllBytes(file))));
                            break;
                        default:
                            convertedFiles.Add((Path.GetFileName(file), File.ReadAllBytes(file)));
                            break;
                    }
                }
            }

            Console.WriteLine($"Writing to file...");

            File.WriteAllLines(config.outputFileName, ToArrayDefinitions(convertedFiles));
            Console.WriteLine($"Done ({sw.Elapsed.TotalSeconds}ms)!");
        }
    }
}
