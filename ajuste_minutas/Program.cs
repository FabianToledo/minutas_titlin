
using System.Diagnostics;
using System.Globalization;

string workingFolder = @".";
string minutaInPath = Path.Combine(workingFolder, @"minutas_in");
string minutaOutPath = Path.Combine(workingFolder, @"minutas_out");

const string separator = "*~*";

const int C_MINUTA = 0;
const int C_CUENTA = 1;
const int C_VALOR = 2;
const int C_TIPO = 3; // D (DEBE) o C (HABER)
const int C_MONEDA = 4; // ARS o USD

const string DEBE = "D";
const string HABER = "C";

const string ARS = "ARS";
const string USD = "USD";

const string CUENTA_ARS_DEBE =  "551015004";
const string CUENTA_ARS_HABER = "541009100";
const string CUENTA_USD =       "800000710";

Debug.WriteLine(workingFolder);
Debug.WriteLine(minutaInPath);
Debug.WriteLine(minutaOutPath);

if (!Directory.Exists(minutaOutPath))
{
    Directory.CreateDirectory(minutaOutPath);
}

if (!Directory.Exists(minutaInPath))
{
    Directory.CreateDirectory(minutaInPath);
}

DirectoryInfo minutaInDir = new DirectoryInfo(minutaInPath);

foreach (FileInfo info in minutaInDir.GetFiles())
{
    Debug.WriteLine(info.FullName);
    if (File.Exists(info.FullName))
    {

        if (GetFileType(info.Name) != FileType.titlin)
            continue;

        List<string[]> allData = ReadDataFromFile(info.FullName);

        Execute(allData);

        WriteDataToFile(allData, Path.Combine(minutaOutPath, info.Name));
        


    }
}

List<string[]> Execute(List<string[]> allData)
{

    decimal debe_ars = 0;
    decimal haber_ars = 0;
    decimal debe_usd = 0;
    decimal haber_usd = 0;

    int line = 0;
    foreach (string[] data in allData)
    {
        line++;
        if ( data[C_MONEDA].Contains(ARS) )
        {
            if ( data[C_TIPO] == DEBE)
            {
                var valor = decimal.Parse(data[C_VALOR], NumberStyles.Number, CultureInfo.InvariantCulture);
                //Debug.WriteLine($"{data[C_MONEDA]} {data[C_TIPO]} Debe: {data[C_VALOR]} ({valor})");
                debe_ars += valor;
            }
            else if (data[C_TIPO] == HABER)
            {
                var valor = decimal.Parse(data[C_VALOR], NumberStyles.Number, CultureInfo.InvariantCulture);
                //Debug.WriteLine($"{data[C_MONEDA]} {data[C_TIPO]} Haber: {data[C_VALOR]} ({valor})");
                haber_ars += valor;
            }

        }
        else if (data[C_MONEDA].Contains(USD))
        {
            if (data[C_TIPO] == DEBE)
            {
                var valor = decimal.Parse(data[C_VALOR], NumberStyles.Number, CultureInfo.InvariantCulture);
                //Debug.WriteLine($"{data[C_MONEDA]} {data[C_TIPO]} Debe: {data[C_VALOR]} ({valor})");
                debe_usd += valor;
            }
            else if (data[C_TIPO] == HABER)
            {
                var valor = decimal.Parse(data[C_VALOR], NumberStyles.Number, CultureInfo.InvariantCulture);
                //Debug.WriteLine($"{data[C_MONEDA]} {data[C_TIPO]} Haber: {data[C_VALOR]} ({valor})");
                haber_usd += valor;
            }

        }
    }
    Debug.WriteLine($"{ARS} Total Debe:  {debe_ars}");
    Debug.WriteLine($"{ARS} Total Haber: {haber_ars}");
    Debug.WriteLine($"{USD} Total Debe:  {debe_usd}");
    Debug.WriteLine($"{USD} Total Haber: {haber_usd}");

    if (haber_ars > debe_ars)
    {
        line = 0;
        foreach (string[] data in allData)
        {
            line++;
            if (IsAdjAccount(data, moneda: ARS, tipo: DEBE, cuenta: CUENTA_ARS_DEBE))
            {
                Debug.WriteLine($"{data[C_MONEDA]} {data[C_TIPO]} {data[C_CUENTA].TrimStart('0')} Agregado: {haber_ars - debe_ars}");
                break;
            }
        }
    }
    else if (haber_ars < debe_ars)
    {
        line = 0;
        foreach (string[] data in allData)
        {
            line++;
            if (IsAdjAccount(data, moneda: ARS, tipo: HABER, cuenta: CUENTA_ARS_HABER))
            {
                Debug.WriteLine($"{data[C_MONEDA]} {data[C_TIPO]} {data[C_CUENTA].TrimStart('0')} Agregado: {debe_ars - haber_ars}");
                break;
            }
        }
    }

    if (haber_usd > debe_usd)
    {
        line = 0;
        foreach (string[] data in allData)
        {
            line++;
            if (IsAdjAccount(data, moneda: USD, tipo: DEBE, cuenta: CUENTA_USD))
            {
                Debug.WriteLine($"{data[C_MONEDA]} {data[C_TIPO]} {data[C_CUENTA].TrimStart('0')} Agregado: {haber_usd - debe_usd}");
                break;
            }
        }
    }
    else if (haber_usd < debe_usd)
    {
        line = 0;
        foreach (string[] data in allData)
        {
            line++;
            if (IsAdjAccount(data, moneda: USD, tipo: HABER, cuenta: CUENTA_USD))
            {
                Debug.WriteLine($"{data[C_MONEDA]} {data[C_TIPO]} {data[C_CUENTA].TrimStart('0')} Agregado: {debe_usd - haber_usd}");
                break;
            }
        }
    }

    return allData;

}

bool IsAdjAccount(string[] data, string moneda, string tipo, string cuenta)
{
    return (data[C_MONEDA].Contains(moneda) && data[C_TIPO] == tipo &&
        int.Parse(data[C_CUENTA], NumberStyles.Integer, CultureInfo.InvariantCulture) ==
        int.Parse(cuenta, NumberStyles.Integer, CultureInfo.InvariantCulture)) ;
}

List<string[]> ReadDataFromFile(string fullName)
{
    using StreamReader src = File.OpenText(fullName);

    List<string[]> allData = new();
    string? s;
    int line = 0;
    while ((s = src.ReadLine()) != null)
    {
        if (string.IsNullOrWhiteSpace(s)) continue;
        line++;
        //Debug.WriteLine($"Before: {s}");
        string[] data = s.Split(separator);

        if (data.Length != 0)
        {
            allData.Add(data);
        }
    }
    return allData;
}

void WriteDataToFile(List<string[]> allData, string fullName)
{
    using StreamWriter dst = File.CreateText(fullName);
    foreach (string[] data in allData)
    {
        string transformedData = string.Join(separator, data);
        //Debug.WriteLine($"After:  {transformedData}");
        dst.WriteLine(transformedData);
    }
}

FileType GetFileType(string fileName)
{
    // Which kind of file did we open?
    // We do this only for performance

    if (fileName.Contains("titcab"))
    {
        return FileType.titcab;
    }
    else if (fileName.Contains("titlin"))
    {
        return FileType.titlin;
    }
    else
    {
        return FileType.unknown;
    }
}

enum FileType
{
    unknown = 0,
    titcab = 1,
    titlin = 2
}



//string transformedData = string.Join(separator, data);
//Debug.WriteLine($"After:  {transformedData}");
//dst.WriteLine(transformedData);


//try
//{
//    decimal number = decimal.Parse(data[TITLIN_DOUBLE], System.Globalization.NumberStyles.Number, CultureInfo.InvariantCulture);

//    number *= 100;

//    string numberStr = number.ToString("000000000000000000.00", CultureInfo.InvariantCulture);

//    data[TITLIN_DOUBLE] = numberStr;
//}
//catch (Exception e)
//{
//    Console.WriteLine(e.Message);
//    Console.WriteLine($"Verificar el archivo: {info.Name} en la linea: {line}");
//    Console.WriteLine(s);
//    Console.WriteLine("");
//    Console.WriteLine("Pulsar una tecla para continuar...");
//    Console.ReadKey();
//}