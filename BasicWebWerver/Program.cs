using Microsoft.VisualBasic;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

class Program
{
    private static string directoryPath = @"C:\Users\creamwx\Music\";
    
    private static BasicWebServer? server;// = new SimpleHttpServer(directoryPath);
    public static bool IsRunning { get; private set; }
    private static string NewLine = Environment.NewLine;
    public static string StartPrompt { get; private set; } = 
            $" Space Bar: Start server using defasults.{NewLine}" +
            $" d: Set root directory to serve.{NewLine}" +
            $" p: Set port.{NewLine}" +
            $" Esc: Exit";

    
    private static int port = 8000;

    static Task Main(string[] args)
    {
        Prompt(StartPrompt);
        while (true)
        {
            var StartKey = Console.ReadKey(true);
            if (StartKey.Key == ConsoleKey.Spacebar)
            {
                StartServer(directoryPath, port);
                break;
            }
            else if (StartKey.Key == ConsoleKey.D)
            {
                SetRoot();
                continue;
            }
            else if (StartKey.Key == ConsoleKey.P)
            {
                Setport();
                continue;
            }
            else if (StartKey.Key == ConsoleKey.Escape)
            {
                Environment.Exit(0);
            }
            //

        }
        //Environment.Exit(0);
        //int port = 8000;
        //StartServer(port);

        while (true)
        {
            var key = Console.ReadKey(true);
            if (key.Key == ConsoleKey.Escape)
            {
                StopServer(server);

                Prompt("Space to restart. Esc to exit");
                while (true)
                {
                    key = Console.ReadKey(true);
                    if (key.Key == ConsoleKey.Escape)
                    {
                        Environment.Exit(0);
                    }
                    else if (key.Key == ConsoleKey.Spacebar)
                    {
                        Console.Clear();
                        StartServer(directoryPath, port);
                        break;
                    }
                }
            }
        }
    }

    private static void Setport()
    {
        Console.WriteLine("Enter port number.");
        try
        {
            port = int.Parse(Console.ReadLine());
            Console.Clear();
            Console.WriteLine($"Port set: {port}");
            Prompt(StartPrompt);
        }
        catch (Exception)
        {
            Console.Clear();
            Console.WriteLine("Error parsing port string. Try again.");
            Prompt(StartPrompt);
        }
    }

    private static void SetRoot()
    {
        Console.WriteLine("Enter path to root.");
        string dir = Console.ReadLine();
        if(Directory.Exists(dir))
        {
            directoryPath = dir;
            Console.Clear() ;
            Console.WriteLine($"Root set: {directoryPath}");
            Prompt(StartPrompt);
            return;
        }
        Console.Clear ();
        Console.WriteLine("Directory does not exist: Wrap in quotes and try again.");
        Prompt(StartPrompt);
    }

    private static void Prompt(string msg)
    {
        Console.WriteLine();
        Console.WriteLine("########### Basic HTTP Web Server ##########");
        //Console.Write("\t");
        Console.WriteLine(msg);
        Console.WriteLine("############################################");
        Console.WriteLine();
    }

    private static void StartServer(string dir, int port)
    {
        directoryPath = dir;
        server = new BasicWebServer(directoryPath);
        _ = server.StartServer(port);
        IsRunning = true;
        SetConsoleTitle(directoryPath, port);
    }

    private static void SetConsoleTitle(string directoryPath = "", int port = 0)
    {
        if (!IsRunning)
        {
            Console.Title = "Stopped";
            return;
        }
        Console.Title = $"Serving: {directoryPath} on port {port}";
    }

    static string? GenerateHtml(List<string> fileList, string indexFilePath)
    {
        string indexStringPath = indexFilePath.Replace(".html", ".txt");
        string html = File.ReadAllText(indexStringPath);

        // Insert items intp list
        var splitHtml = html.Split("<!--list item-->");
        var htmlTop = splitHtml[0];
        var htmlBottom = splitHtml[1];

        foreach (var fileName in fileList)
        {
            htmlTop += WrapInListTags(fileName);
        }

        return htmlTop + htmlBottom;
    }

    static void Refresh()
    {
        //Console.WriteLine("refreshind folder...");
        var filePathList = Directory.GetFiles(directoryPath, "*.mp4").ToList();
        List<string> fileNameList = new List<string>();
        foreach (string filePath in filePathList)
        {
            fileNameList.Add(Path.GetFileName(filePath));
        }
        string indexFilePath = Path.Combine(directoryPath, "index.html");
        string indexHtml = GenerateHtml(fileNameList, indexFilePath);

        File.WriteAllText(indexFilePath, indexHtml);
    }

    static string WrapInListTags(string fileName)
    {
        return $"""<li><a href="{fileName}">{fileName}</a></li>{Environment.NewLine}""";
    }

    private static void StopServer(BasicWebServer server)
    {
        server.StopServer();
        IsRunning = false;
        SetConsoleTitle();
    }
}

