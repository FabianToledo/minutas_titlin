
using Entities;

string workingFolder = @".";
string minutaInPath = Path.Combine(workingFolder, @"minutas_in");
string minutaOutPath = Path.Combine(workingFolder, @"minutas_out");

if (!Directory.Exists(minutaOutPath))
{
    Directory.CreateDirectory(minutaOutPath);
}

if (!Directory.Exists(minutaInPath))
{
    Directory.CreateDirectory(minutaInPath);
}

DirectoryInfo minutaInDir = new(minutaInPath);

foreach (FileInfo info in minutaInDir.GetFiles())
{
    if (File.Exists(info.FullName))
    {
        if (!info.Name.Contains("titlin"))
            continue;

        Console.WriteLine();
        Console.WriteLine($"Nombre de archivo: {info.Name}");

        try
        {
            IEnumerable<Entry> allEntries = Entry.ReadDataFromFile(info.FullName);

            Entry.Adjust(allEntries);

            Entry.WriteDataToFile(allEntries, Path.Combine(minutaOutPath, info.Name));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            continue;
        }

    }
}

Console.WriteLine();
Console.WriteLine("Pulse una tecla para cerrar la ventana.");
Console.ReadKey();

