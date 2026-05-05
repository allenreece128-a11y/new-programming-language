using System;
using System.Collections.Generic;
using NovaScript.Core;

namespace NovaScript
{
    class Program
    {
        static void Main(string[] args)
        {
            WriteLineColored("NovaScript Runtime Environment v1.0", ConsoleColor.Magenta);
            WriteLineColored("Dr4gon VM: READY", ConsoleColor.Cyan);
            WriteLineColored("System initialized.", ConsoleColor.Green);
            
            if (args.Length > 0)
            {
                string filePath = args[0];
                if (System.IO.File.Exists(filePath))
                {
                    RunFile(filePath);
                }
                else
                {
                    
                    string altPath = System.IO.Path.Combine(AppContext.BaseDirectory, filePath);
                    if (System.IO.File.Exists(altPath))
                    {
                        RunFile(altPath);
                    }
                    else
                    {
                        WriteLineColored($"[Error] Source file not found: {filePath}", ConsoleColor.Red);
                    }
                }
            }
            else
            {
                var term = new NovaTerminal();
                term.Start();
            }
        }

        private static void RunFile(string path)
        {
            string source = System.IO.File.ReadAllText(path);
            Run(source, sourcePath: path);
        }

        private static void Run(string source, Interpreter? interpreter = null, string? sourcePath = null)
        {
            try
            {
                var lexer = new Lexer(source);
                var tokens = lexer.Tokenize();
                
                var parser = new Parser(tokens);
                var statements = parser.Parse();

                if (interpreter == null) interpreter = new Interpreter();
                interpreter.Interpret(statements, sourcePath);
            }
            catch (Exception e)
            {
                WriteLineColored($"[Error] {e.Message}", ConsoleColor.Red);
            }
        }

        private static void WriteLineColored(string text, ConsoleColor color)
        {
            if (Console.IsOutputRedirected)
            {
                Console.WriteLine(text);
                return;
            }

            var previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ForegroundColor = previous;
        }
    }
}
