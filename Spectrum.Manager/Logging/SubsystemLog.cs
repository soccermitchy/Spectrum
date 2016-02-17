﻿using System;
using System.IO;

namespace Spectrum.Manager.Logging
{
    class SubsystemLog
    {
        private string FilePath { get; }
        private bool WriteToConsole { get; }

        public SubsystemLog(string filePath, bool writeToConsole = false)
        {
            if (File.Exists(filePath))
                File.Delete(filePath);

            FilePath = filePath;
            WriteToConsole = writeToConsole;
        }

        public void Error(string message)
        {
            WriteLine($"[!][{DateTime.Now}] {message}");
        }

        public void Info(string message, bool noNewLine = false)
        {
            var msg = $"[i][{DateTime.Now}] {message}";

            if (noNewLine)
            {
                Write(msg);
            }
            else
            {
                WriteLine(msg);
            }
        }

        public void Exception(Exception e)
        {
            WriteLine($"[e][{DateTime.Now}] {e.Message}");
            WriteLine($"   Target site: {e.TargetSite}");
            WriteLine("   Stack trace:");
            foreach (var s in e.StackTrace.Split('\n'))
            {
                WriteLine($"      {s}");
            }
        }

        public void WriteLine(string text)
        {
            using (var sw = new StreamWriter(FilePath, true))
            {
                sw.WriteLine(text);
            }

            if (WriteToConsole)
            {
                Console.WriteLine(text);
            }
        }

        private void Write(string text)
        {
            using (var sw = new StreamWriter(FilePath, true))
            {
                sw.Write(text);
            }

            if (WriteToConsole)
            {
                Console.Write(text);
            }
        }
    }
}