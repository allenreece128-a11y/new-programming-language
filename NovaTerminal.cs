using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using NovaScript.Core;

namespace NovaScript
{
    public class NovaTerminal
    {
        private readonly Interpreter _interpreter = new();
        private static readonly ConsoleColor Pink = ConsoleColor.Magenta;
        private static readonly ConsoleColor Blue = ConsoleColor.Cyan;
        private static readonly ConsoleColor Green = ConsoleColor.Green;
        private static readonly ConsoleColor Red = ConsoleColor.Red;
        private static readonly ConsoleColor Yellow = ConsoleColor.Yellow;
        private static readonly ConsoleColor Muted = ConsoleColor.DarkGray;
        private static readonly ConsoleColor Plain = ConsoleColor.Gray;
        private static readonly string[] ReeceAsciiArt =
        {
            "RRRRR   EEEEE   EEEEE    CCCC    EEEEE",
            "R   RR  E       E       C        E",
            "RRRRR   EEEE    EEEE    C        EEEE",
            "R  RR   E       E       C        E",
            "R   RR  EEEEE   EEEEE    CCCC    EEEEE"
        };

        public void Start()
        {
            PrintBanner();
            PrintHelp();

            var buffer = new StringBuilder();
            int blockBalance = 0; // counts '::' minus ';;'

            while (true)
            {
                if (blockBalance > 0 || buffer.Length > 0) WriteColored("... ", Blue);
                else WriteColored("ns> ", Pink);
                string? line = Console.ReadLine();
                if (line == null) break;
                string trimmedLine = line.Trim();

                if (buffer.Length == 0 && string.Equals(trimmedLine, "reece", StringComparison.OrdinalIgnoreCase))
                {
                    PrintReeceEasterEgg();
                    continue;
                }

                if (buffer.Length == 0 && string.Equals(trimmedLine, "hail", StringComparison.OrdinalIgnoreCase))
                {
                    RunHailEasterEgg();
                    continue;
                }

                if (buffer.Length == 0 && string.Equals(trimmedLine, "graph", StringComparison.OrdinalIgnoreCase))
                {
                    StartGraphBuilder();
                    continue;
                }

                // Handle dot-commands
                if (trimmedLine.StartsWith(".", StringComparison.Ordinal))
                {
                    if (string.Equals(trimmedLine, ".cancel", StringComparison.OrdinalIgnoreCase))
                    {
                        buffer.Clear();
                        blockBalance = 0;
                        WriteLineColored("Buffered input cleared.", Yellow);
                        continue;
                    }

                    if (buffer.Length > 0 || blockBalance > 0)
                    {
                        WriteLineColored("Finish the current block or use .cancel.", Yellow);
                        continue;
                    }

                    if (HandleCommand(trimmedLine))
                        continue;
                }

                // Accumulate multi-line input based on block tokens
                blockBalance += CountOccurrences(line, "::");
                blockBalance -= CountOccurrences(line, ";;");
                buffer.AppendLine(line);

                if (blockBalance <= 0)
                {
                    var source = buffer.ToString();
                    buffer.Clear();
                    blockBalance = 0;
                    ExecuteSource(source);
                }
            }
        }

        private bool HandleCommand(string input)
        {
            var parts = input.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
            var cmd = parts[0].ToLowerInvariant();
            var arg = parts.Length > 1 ? parts[1].Trim() : string.Empty;

            switch (cmd)
            {
                case ".exit":
                case ".quit":
                    WriteLineColored("Exiting NovaTerminal.", Green);
                    System.Environment.Exit(0);
                    return true;
                case ".help":
                    PrintHelp();
                    return true;
                case ".clear":
                    Console.Clear();
                    PrintBanner();
                    return true;
                case ".vars":
                    PrintVariables();
                    return true;
                case ".run":
                    if (string.IsNullOrWhiteSpace(arg))
                    {
                        WriteLineColored("Usage: .run <file.ns>", Yellow);
                        return true;
                    }
                    
                    string finalPath = arg;
                    if (!File.Exists(finalPath))
                    {
                        // Try relative to app base directory if not found in current directory
                        string appDir = AppContext.BaseDirectory;
                        string altPath = Path.Combine(appDir, arg);
                        if (File.Exists(altPath))
                        {
                            finalPath = altPath;
                        }
                        else
                        {
                            WriteLineColored($"[Error] File not found: {arg}", Red);
                            // Helpful diagnostic
                            // Console.WriteLine($"(Searched: {Path.GetFullPath(arg)})");
                            return true;
                        }
                    }
                    
                    try
                    {
                        var src = File.ReadAllText(finalPath);
                        ExecuteSource(src, finalPath);
                    }
                    catch (Exception ex)
                    {
                        WriteLineColored($"[Error] Could not read file: {ex.Message}", Red);
                    }
                    return true;
                case ".graph":
                    StartGraphBuilder();
                    return true;
                default:
                    WriteLineColored($"Unknown command: {cmd}. Type .help for a list of commands.", Yellow);
                    return true;
            }
        }

        private void PrintVariables()
        {
            try
            {
                var snapshot = _interpreter.SnapshotVariables();
                if (snapshot.Count == 0)
                {
                    WriteLineColored("<no variables defined>", Muted);
                    return;
                }
                WriteLineColored("Name                Kind     Value", Blue);
                WriteLineColored("----------------------------------------", Muted);
                foreach (var kv in snapshot)
                {
                    var kind = kv.Value.IsMutable ? "$" : "#";
                    var value = kv.Value.Value is null ? "(_)" : kv.Value.Value is bool b ? (b ? "(+)" : "(-)") : kv.Value.Value.ToString();
                    WriteColored(string.Format("{0,-19} ", kv.Key), Plain);
                    WriteColored(string.Format("{0,-7} ", kind), kv.Value.IsMutable ? Green : Blue);
                    WriteLineColored($"{value}", Plain);
                }
            }
            catch (Exception e)
            {
                WriteLineColored($"[Error] {e.Message}", Red);
            }
        }

        private void ExecuteSource(string source, string? sourcePath = null)
        {
            try
            {
                var lexer = new Lexer(source);
                var tokens = lexer.Tokenize();
                var parser = new Parser(tokens);
                var statements = parser.Parse();
                _interpreter.Interpret(statements, sourcePath);
            }
            catch (Exception e)
            {
                WriteLineColored($"[Error] {e.Message}", Red);
            }
        }

        private static int CountOccurrences(string text, string value)
        {
            if (string.IsNullOrEmpty(value)) return 0;
            int count = 0;
            int index = 0;
            while ((index = text.IndexOf(value, index, StringComparison.Ordinal)) >= 0)
            {
                count++;
                index += value.Length;
            }
            return count;
        }

        private static void PrintBanner()
        {
            WriteColored("NovaScript", Pink);
            WriteColored(" Runtime v1.0", Blue);
            WriteColored("  |  ", Muted);
            WriteLineColored("Dr4gon VM", Green);
            WriteLineColored("Interactive Terminal (NovaTerminal)\n", Muted);
        }

        private static void PrintHelp()
        {
            WriteLineColored("Commands:", Green);
            WriteHelpLine(".help", "Show this help");
            WriteHelpLine(".exit | .quit", "Exit the terminal");
            WriteHelpLine(".clear", "Clear the screen");
            WriteHelpLine(".vars", "List variables in current scope(s)");
            WriteHelpLine(".run <file.ns>", "Execute a NovaScript file in this session");
            WriteHelpLine(".graph", "Interactive graph builder (line/bar/pie)");
            WriteHelpLine(".cancel", "Clear unfinished block input");
            Console.WriteLine();
            WriteLineColored("Type NovaScript code directly to execute. Use '::' to open and ';;' to close blocks.", Muted);
            WriteLineColored("Tip: you can also type 'graph' without the dot command prefix.", Muted);
        }

        private void RunHailEasterEgg()
        {
            PrintHailChant();
            LaunchHailCatScene();
        }

        private static void PrintHailChant()
        {
            const string chant = "HAIL THE CHOSEN ONE";

            if (Console.IsOutputRedirected)
            {
                for (int i = 0; i < 40; i++)
                {
                    WriteLineColored(chant, Red);
                }
                return;
            }

            int width = Math.Max(20, Console.WindowWidth);
            int maxOffset = Math.Max(1, width - chant.Length - 1);
            var rng = new Random();
            var end = DateTime.UtcNow.AddSeconds(3.2);

            while (DateTime.UtcNow < end)
            {
                int offset = rng.Next(0, maxOffset + 1);
                string line = new string(' ', offset) + chant;
                if (line.Length > width) line = line.Substring(0, width);
                WriteLineColored(line, Red);
                System.Threading.Thread.Sleep(55);
            }
        }

        private void LaunchHailCatScene()
        {
            const string demoFile = "cat_hail_demo.ns";
            string path = demoFile;
            if (!File.Exists(path))
            {
                string appPath = Path.Combine(AppContext.BaseDirectory, demoFile);
                if (File.Exists(appPath))
                {
                    path = appPath;
                }
                else
                {
                    WriteLineColored($"[Error] Could not find {demoFile}.", Red);
                    return;
                }
            }

            try
            {
                string source = File.ReadAllText(path);
                string? detectedImage = FindHailImagePath();
                if (!string.IsNullOrWhiteSpace(detectedImage))
                {
                    string fileName = Path.GetFileName(detectedImage);
                    source = source.Replace(
                        "$ imagePath = \"shadow_cat.jpg\";",
                        $"$ imagePath = \"{EscapeNsString(fileName)}\";",
                        StringComparison.Ordinal);
                    WriteLineColored($"Using image file: {fileName}", Muted);
                }
                else
                {
                    WriteLineColored("No image file detected in NovaScript folder. Expected shadow_cat.jpg.", Yellow);
                }

                WriteLineColored("The chosen one arrives.", Red);
                WriteLineColored("Expected image file: shadow_cat.jpg", Muted);
                WriteLineColored("Close the scene with ESC or Q.", Muted);
                ExecuteSource(source, path);
            }
            catch (Exception ex)
            {
                WriteLineColored($"[Error] Could not launch cat scene: {ex.Message}", Red);
            }
        }

        private static string? FindHailImagePath()
        {
            static int ScoreFileName(string fileName)
            {
                string n = fileName.ToLowerInvariant();
                int score = 0;
                if (n.Contains("shadow", StringComparison.Ordinal)) score += 6;
                if (n.Contains("cat", StringComparison.Ordinal)) score += 5;
                if (n.Contains("hail", StringComparison.Ordinal)) score += 3;
                if (n == "shadow_cat.jpg") score += 100;
                return score;
            }

            string[] patterns = { "*.jpg", "*.jpeg", "*.png", "*.bmp", "*.gif", "*.webp" };
            var candidates = new List<string>();

            string cwd = Directory.GetCurrentDirectory();
            string app = AppContext.BaseDirectory;

            if (Directory.Exists(cwd))
            {
                foreach (string pattern in patterns)
                {
                    candidates.AddRange(Directory.GetFiles(cwd, pattern, SearchOption.TopDirectoryOnly));
                }
            }

            if (Directory.Exists(app) && !string.Equals(app, cwd, StringComparison.OrdinalIgnoreCase))
            {
                foreach (string pattern in patterns)
                {
                    candidates.AddRange(Directory.GetFiles(app, pattern, SearchOption.TopDirectoryOnly));
                }
            }

            if (candidates.Count == 0) return null;

            candidates.Sort((a, b) =>
            {
                int scoreCmp = ScoreFileName(Path.GetFileName(b)).CompareTo(ScoreFileName(Path.GetFileName(a)));
                if (scoreCmp != 0) return scoreCmp;
                return string.Compare(Path.GetFileName(a), Path.GetFileName(b), StringComparison.OrdinalIgnoreCase);
            });

            return candidates[0];
        }

        private void StartGraphBuilder()
        {
            try
            {
                WriteLineColored("Nova Graph Builder", Green);
                WriteLineColored("Press Enter to accept defaults.", Muted);

                string graphType = PromptGraphType();
                string title = PromptText("Chart title", "Points scored");
                string independentVariable = PromptText("Independent variable (x-axis)", "INDEPENDENT VARIABLE");
                string dependentVariable = PromptText("Dependent variable (y-axis)", "DEPENDENT VARIABLE");
                int width = PromptInt("Window width", 980, 320, 3840);
                int height = PromptInt("Window height", 560, 240, 2160);

                var dataLines = new List<string>();

                if (graphType == "pie")
                {
                    int slices = PromptInt("How many slices", 4, 1, 100);
                    for (int i = 1; i <= slices; i++)
                    {
                        string label = PromptText($"Slice {i} label", $"Slice {i}");
                        double value = PromptDouble($"Slice {i} value", i * 10.0);
                        dataLines.Add($"chart_add_slice(\"{EscapeNsString(label)}\", {FormatNumber(value)});");
                    }
                }
                else
                {
                    int seriesCount = PromptInt("How many series", 1, 1, 20);
                    for (int s = 1; s <= seriesCount; s++)
                    {
                        string defaultSeries = seriesCount == 1 ? "Series 1" : $"Series {s}";
                        string seriesName = PromptText($"Series {s} name", defaultSeries);
                        int points = PromptInt($"How many points in {seriesName}", 4, 1, 200);
                        for (int p = 1; p <= points; p++)
                        {
                            string xLabel = PromptText($"Point {p} x-value", $"X{p}");
                            double yValue = PromptDouble($"Point {p} y-value", p * 10.0);
                            dataLines.Add(
                                $"chart_add_point(\"{EscapeNsString(seriesName)}\", \"{EscapeNsString(xLabel)}\", {FormatNumber(yValue)});");
                        }
                    }
                }

                string script = BuildGraphScript(
                    graphType,
                    title,
                    independentVariable,
                    dependentVariable,
                    width,
                    height,
                    dataLines);

                WriteLineColored("Creating graph window...", Green);
                WriteLineColored("Close the graph window with ESC, Q, Enter, or Space.", Muted);
                ExecuteSource(script);
            }
            catch (Exception ex)
            {
                WriteLineColored($"[Error] Graph builder failed: {ex.Message}", Red);
            }
        }

        private static string BuildGraphScript(
            string graphType,
            string title,
            string independentVariable,
            string dependentVariable,
            int width,
            int height,
            List<string> dataLines)
        {
            string safeTitle = EscapeNsString(title);
            string safeIndependent = EscapeNsString(independentVariable);
            string safeDependent = EscapeNsString(dependentVariable);
            int textY = Math.Max(6, height - 22);

            var sb = new StringBuilder();
            sb.AppendLine($"gfx_init({width}, {height}, \"NovaScript Graph\");");
            sb.AppendLine("gfx_set_vsync((+));");
            sb.AppendLine("gfx_set_clear_color(0, 0, 0, 255);");
            sb.AppendLine($"chart_config(\"{graphType}\", \"{safeTitle}\", \"{safeIndependent}\", \"{safeDependent}\");");

            foreach (var line in dataLines)
            {
                sb.AppendLine(line);
            }

            sb.AppendLine("$ running = (+);");
            sb.AppendLine("* running ::");
            sb.AppendLine("    running = gfx_poll();");
            sb.AppendLine("    ~ running ::");
            sb.AppendLine("        ~ input_key_down(\"Escape\") ::");
            sb.AppendLine("            running = (-);");
            sb.AppendLine("        ;;");
            sb.AppendLine("        ~ input_key_down(\"Q\") ::");
            sb.AppendLine("            running = (-);");
            sb.AppendLine("        ;;");
            sb.AppendLine("        ~ input_key_down(\"Enter\") ::");
            sb.AppendLine("            running = (-);");
            sb.AppendLine("        ;;");
            sb.AppendLine("        ~ input_key_down(\"Space\") ::");
            sb.AppendLine("            running = (-);");
            sb.AppendLine("        ;;");
            sb.AppendLine("        chart_render();");
            sb.AppendLine($"        gfx_text(14, {textY}, \"ESC/Q/ENTER/SPACE TO CLOSE\", 1, 0, 255, 0, 255);");
            sb.AppendLine("        gfx_present();");
            sb.AppendLine("    ;;");
            sb.AppendLine(";;");
            sb.AppendLine("gfx_quit();");

            return sb.ToString();
        }

        private static string PromptGraphType()
        {
            while (true)
            {
                string raw = PromptText("Graph type (line/bar/pie)", "line")
                    .Trim()
                    .ToLowerInvariant()
                    .Replace(" ", string.Empty, StringComparison.Ordinal)
                    .Replace("-", string.Empty, StringComparison.Ordinal)
                    .Replace("_", string.Empty, StringComparison.Ordinal);

                if (raw == "line") return "line";
                if (raw == "bar" || raw == "barchart") return "bar";
                if (raw == "pie" || raw == "piechart" || raw == "pigraph" || raw == "pichart" || raw == "pi") return "pie";

                WriteLineColored("Use line, bar, or pie.", Yellow);
            }
        }

        private static string PromptText(string label, string defaultValue)
        {
            WriteColored($"{label} [{defaultValue}]: ", Blue);
            string? raw = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(raw)) return defaultValue;
            return raw.Trim();
        }

        private static int PromptInt(string label, int defaultValue, int min, int max)
        {
            while (true)
            {
                string raw = PromptText(label, defaultValue.ToString(CultureInfo.InvariantCulture));
                if (!int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value))
                {
                    WriteLineColored("Enter a whole number.", Yellow);
                    continue;
                }

                if (value < min || value > max)
                {
                    WriteLineColored($"Enter a value between {min} and {max}.", Yellow);
                    continue;
                }

                return value;
            }
        }

        private static double PromptDouble(string label, double defaultValue)
        {
            while (true)
            {
                string raw = PromptText(label, FormatNumber(defaultValue));
                if (!double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double value))
                {
                    WriteLineColored("Enter a valid number (example: 42 or 12.5).", Yellow);
                    continue;
                }

                return value;
            }
        }

        private static string EscapeNsString(string value)
        {
            return value
                .Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal)
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal);
        }

        private static string FormatNumber(double value)
        {
            double rounded = Math.Round(value);
            if (Math.Abs(value - rounded) < 0.000001)
            {
                return rounded.ToString(CultureInfo.InvariantCulture);
            }

            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static void PrintReeceEasterEgg()
        {
            if (Console.IsOutputRedirected || Console.IsInputRedirected)
            {
                WriteLineColored("", Red);
                foreach (var line in ReeceAsciiArt) WriteLineColored(line, Red);
                WriteLineColored("", Red);
                return;
            }

            int startTop = Console.CursorTop;
            int screenWidth = Math.Max(20, Console.WindowWidth);
            int artWidth = 0;
            foreach (var line in ReeceAsciiArt)
            {
                if (line.Length > artWidth) artWidth = line.Length;
            }
            int maxOffset = Math.Max(1, screenWidth - artWidth);
            int offset = 0;
            bool shouldManageCursorVisibility = OperatingSystem.IsWindows();
            bool oldCursorVisibility = true;
            var oldColor = Console.ForegroundColor;

            try
            {
                if (shouldManageCursorVisibility)
                {
                    oldCursorVisibility = Console.CursorVisible;
                    Console.CursorVisible = false;
                }

                while (true)
                {
                    if (Console.KeyAvailable)
                    {
                        Console.ReadKey(intercept: true);
                        break;
                    }

                    for (int i = 0; i < ReeceAsciiArt.Length; i++)
                    {
                        Console.SetCursorPosition(0, startTop + i);
                        string frameLine = new string(' ', offset) + ReeceAsciiArt[i];
                        if (frameLine.Length > screenWidth) frameLine = frameLine.Substring(0, screenWidth);
                        else frameLine = frameLine.PadRight(screenWidth);

                        Console.ForegroundColor = Red;
                        Console.Write(frameLine);
                    }

                    Console.SetCursorPosition(0, startTop + ReeceAsciiArt.Length);
                    string tip = "Press any key to stop";
                    if (tip.Length > screenWidth) tip = tip.Substring(0, screenWidth);
                    else tip = tip.PadRight(screenWidth);
                    Console.ForegroundColor = Muted;
                    Console.Write(tip);

                    offset = (offset + 1) % (maxOffset + 1);
                    System.Threading.Thread.Sleep(70);
                }
            }
            finally
            {
                Console.ForegroundColor = oldColor;
                if (shouldManageCursorVisibility)
                {
                    Console.CursorVisible = oldCursorVisibility;
                }
                Console.SetCursorPosition(0, startTop + ReeceAsciiArt.Length + 1);
            }
        }

        private static void WriteHelpLine(string command, string description)
        {
            WriteColored($"  {command,-14} ", Blue);
            WriteLineColored(description, Plain);
        }

        private static void WriteColored(string text, ConsoleColor color)
        {
            if (Console.IsOutputRedirected)
            {
                Console.Write(text);
                return;
            }

            var previous = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.Write(text);
            Console.ForegroundColor = previous;
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



