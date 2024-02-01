using System.Diagnostics;
using System.Management;

internal class Program
{
    private static string LogPath = "process.log";
    private static string LogMessage = "{0} - Process {1} has been created, path is: {2}, arguments: {3}";
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Console.WriteLine("Please provide a process name");
            return;
        }

        var processName = args[0];
        var processes = Process.GetProcessesByName(processName);
        string queryPart = "";

        if (processes.Length == 0)
        {
            Console.WriteLine($"No processes with name '{processName}' found");
            return;
        }
        else
        {
            Console.WriteLine($"Found {processes.Length} processes with name '{processName}'");
        }

        queryPart = $"TargetInstance.ParentProcessId = {processes[0].Id}";

        for (int i = 1; i < processes.Length; i++)
        {
            Process? process = processes[i];
            var id = process.Id;
            queryPart += $" or TargetInstance.ParentProcessId = {id}";
        }

        WqlEventQuery query = new("__InstanceCreationEvent", new TimeSpan(0, 0, 1), $"TargetInstance isa \"Win32_Process\" and {queryPart}");

        // Initialize an event watcher and subscribe to events
        // that match this query
        ManagementEventWatcher watcher = new(query);
        watcher.Options.Timeout = new TimeSpan(0, 0, 10);

        watcher.EventArrived += (sender, e) =>
        {
            var date = DateTime.Now;
            // Display information from the event
            var newEvent = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            var instanceName = newEvent["Name"];
            var executablePath = newEvent["ExecutablePath"];
            var commandLine = newEvent["CommandLine"];
            string logMessage = string.Format(LogMessage, date, instanceName, executablePath, commandLine);

            Console.WriteLine(logMessage);

            // Save logs to a file
            File.AppendAllText(LogPath, logMessage + Environment.NewLine);
        };

        watcher.Start();

        // Block the program from exiting
        Console.WriteLine("Press any key to stop watching...");
        Console.ReadKey();

        // Cancel the subscription
        watcher.Stop();
    }
}