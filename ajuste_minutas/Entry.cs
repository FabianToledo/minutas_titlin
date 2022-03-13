using System.Diagnostics;
using System.Globalization;

namespace Entities
{

    internal class Entry
    {
        public int NroMinuta { get; set; }
        public string NroCuenta { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public TipoMovimiento Tipo { get; set; }
        public Moneda Moneda { get; set; }
        public int Transaccion { get; set; }
        public string CampoCeros { get; set; } = string.Empty;
        public int Grupo { get; set; }

        static readonly string SEPARATOR = "*~*";

        internal Entry(string fileLine)
        {
            Deserialize(fileLine);
        }

        internal void Deserialize(string fileLine)
        {
            string[] data = fileLine.Split(SEPARATOR);
            NroMinuta = int.Parse(data[0], NumberStyles.Integer, CultureInfo.InvariantCulture);
            NroCuenta = data[1];
            Amount = decimal.Parse(data[2], NumberStyles.Number, CultureInfo.InvariantCulture);
            Tipo = data[3] == ParseHelper.DEBE ? TipoMovimiento.Debe : data[3] == ParseHelper.HABER ? TipoMovimiento.Haber : TipoMovimiento.Unknown;
            Moneda = data[4].Contains(ParseHelper.ARS) ? Moneda.ARS :
                     data[4].Contains(ParseHelper.USD) ? Moneda.USD :
                     Moneda.Unknown;
            Transaccion = int.Parse(data[5], NumberStyles.Integer, CultureInfo.InvariantCulture);
            CampoCeros = data[6];
            Grupo = int.Parse(data[7], NumberStyles.Integer, CultureInfo.InvariantCulture);
        }

        internal string Serialize()
        {
            string[] data = new string[8];
            data[0] = NroMinuta.ToString("0000", CultureInfo.InvariantCulture);
            data[1] = NroCuenta;
            data[2] = Amount.ToString("000000000000000000.00", CultureInfo.InvariantCulture);
            data[3] = Tipo.GetString();
            data[4] = "Minuta Titulos: " + Moneda.GetString();
            data[5] = Transaccion.ToString("00000", CultureInfo.InvariantCulture);
            data[6] = CampoCeros;
            data[7] = Grupo.ToString("0000", CultureInfo.InvariantCulture);
            return string.Join(SEPARATOR, data);
        }

        internal static IEnumerable<Entry> ReadDataFromFile(string fullName)
        {
            using StreamReader src = File.OpenText(fullName);

            List<Entry> allEntries = new();
            string? fileLine;
            int line = 0;
            while ((fileLine = src.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(fileLine)) continue;
                line++;
                //Debug.WriteLine($"Before: {fileLine}");

                allEntries.Add(new Entry(fileLine));

            }
            return allEntries;
        }

        internal static void WriteDataToFile(IEnumerable<Entry> allEntries, string fullName)
        {
            using StreamWriter dst = File.CreateText(fullName);
            // UNIX style
            dst.NewLine = "\n";
            foreach (Entry data in allEntries)
            {
                string transformedData = data.Serialize();
                //Debug.WriteLine($"After:  {transformedData}");
                dst.WriteLine(transformedData);
            }
        }

        internal static void Adjust(IEnumerable<Entry> entries)
        {
            var groups = entries.GroupBy(e => new { e.NroMinuta, e.Moneda, e.Grupo });

            foreach (var group in groups)
            {
                Debug.WriteLine($"{group.Key}");
                var debe = group.Where(e => e.Tipo == TipoMovimiento.Debe).Sum(e => e.Amount);
                var haber = group.Where(e => e.Tipo == TipoMovimiento.Haber).Sum(e => e.Amount);

                if (group.Where(e => e.Tipo == TipoMovimiento.Unknown || e.Moneda == Moneda.Unknown).Any())
                {
                    Console.WriteLine($"Error en: {group.Key}");
                    throw new Exception("Hay tipos de movimientos o monedas desconocidas, no se puede ajustar automaticamente, verificar manualmente");
                }
                if (Math.Abs(debe - haber) > 0.05M)
                {
                    Console.WriteLine($"{group.Key} Debe - Haber: {debe - haber} > 0,05");
                    continue;
                }
                if (group.Key.Moneda == Moneda.Unknown)
                {
                    Console.WriteLine($"{group.Key} Moneda desconocida");
                    continue;
                }

                (var ajTipo, var ajCuenta) = GetTipoYCuenta(debe, haber, group.Key.Moneda);

                if (debe != haber)
                {
                    decimal ajuste = Math.Abs(debe - haber);

                    var adjEntry = group.FirstOrDefault(e => string.Compare(e.NroCuenta, ajCuenta) == 0 && e.Tipo == ajTipo);
                    if (adjEntry == null)
                    {
                        Console.WriteLine($"{group.Key} ERROR, no se encontró la cuenta ({ajTipo}): {ajCuenta.TrimStart('0')} para hacer Ajuste: {ajuste}");
                        continue;
                    }

                    adjEntry.Amount += ajuste;

                    Console.WriteLine($"{group.Key} Se ajustó la Cuenta: {ajCuenta.TrimStart('0')} Tipo op.: {ajTipo} Ajuste: {ajuste}");
                }

                debe = group.Where(e => e.Tipo == TipoMovimiento.Debe).Sum(e => e.Amount);
                haber = group.Where(e => e.Tipo == TipoMovimiento.Haber).Sum(e => e.Amount);
                if (debe - haber != 0.0M)
                {
                    throw new Exception($"La verificación del ajuste no da cero (Debe - Haber = {debe - haber}), no se puede ajustar automaticamente, verificar manualmente");
                }
                else
                {
                    Console.WriteLine($"{group.Key} OK, debe - haber = {debe - haber}");
                }

            }

        }

        internal static (TipoMovimiento, string) GetTipoYCuenta(decimal debe, decimal haber, Moneda moneda)
        {
            const string CUENTA_ARS_DEBE = "0000000551015004";
            const string CUENTA_ARS_HABER = "0000000541009100";
            const string CUENTA_USD = "0000000800000710";

            TipoMovimiento ajTipo = debe > haber ? TipoMovimiento.Haber : haber > debe ? TipoMovimiento.Debe : TipoMovimiento.Unknown;

            string ajCuenta = moneda == Moneda.ARS && ajTipo == TipoMovimiento.Haber ? CUENTA_ARS_HABER :
                              moneda == Moneda.ARS && ajTipo == TipoMovimiento.Debe ? CUENTA_ARS_DEBE :
                              moneda == Moneda.USD ? CUENTA_USD : "";

            return (ajTipo, ajCuenta);
        }
    }

    internal enum TipoMovimiento
    {
        Unknown,
        Debe,
        Haber
    }

    internal enum Moneda
    {
        Unknown,
        ARS,
        USD
    }

    internal static class ParseHelper
    {
        internal static readonly string DEBE = "D";
        internal static readonly string HABER = "C";
        internal static readonly string ARS = "ARS";
        internal static readonly string USD = "USD";

        internal static string GetString(this TipoMovimiento tipo) =>
            tipo switch
            {
                TipoMovimiento.Unknown => "UNK",
                TipoMovimiento.Debe => DEBE,
                TipoMovimiento.Haber => HABER,
                _ => ""
            };

        internal static string GetString(this Moneda moneda) =>
            moneda switch
            {
                Moneda.Unknown => "UNK",
                Moneda.ARS => ARS,
                Moneda.USD => USD,
                _ => ""
            };
    }
}
