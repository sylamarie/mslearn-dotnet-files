using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

record SalesData(double Total);

class Program
{
    static void Main()
    {
        // project folders
        var currentDirectory = Directory.GetCurrentDirectory();
        var storesDir       = Path.Combine(currentDirectory, "stores");        // e.g., stores/201, stores/202, ...
        var outputDir       = Path.Combine(currentDirectory, "salesTotalDir"); // where we’ll put the summary
        Directory.CreateDirectory(outputDir);

        var summaryFile = Path.Combine(outputDir, "salesSummary.txt");

        // find all sales.json files under stores/**/
        var salesFiles = FindFiles(storesDir, "*.json");

        // compute totals per store/file
        var perStoreTotals = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        double grandTotal = 0;

        foreach (var file in salesFiles)
        {
            // identify the store name from the parent folder (e.g., 201, 202, 203, 204)
            var storeName = new DirectoryInfo(Path.GetDirectoryName(file)!).Name;

            double fileTotal = ReadSalesJsonTotal(file); // reads { "Total": <number> }
            grandTotal += fileTotal;

            if (perStoreTotals.ContainsKey(storeName))
                perStoreTotals[storeName] += fileTotal;   // if you had multiple json files per store
            else
                perStoreTotals[storeName] = fileTotal;
        }

        // write the nice summary
        GenerateSalesSummary(summaryFile, perStoreTotals, grandTotal);

        Console.WriteLine($"Sales summary created at: {summaryFile}");
    }

    // find files matching pattern under folder & subfolders
    static IEnumerable<string> FindFiles(string folder, string pattern)
    {
        if (!Directory.Exists(folder))
            yield break;

        foreach (var file in Directory.EnumerateFiles(folder, pattern, SearchOption.AllDirectories))
            yield return file;
    }

    // read { "Total": number } from a sales.json file
    static double ReadSalesJsonTotal(string filePath)
    {
        try
        {
            string json = File.ReadAllText(filePath);
            SalesData? data = JsonConvert.DeserializeObject<SalesData?>(json);
            return data?.Total ?? 0;
        }
        catch
        {
            // if a file is malformed, treat as zero and continue
            return 0;
        }
    }

    // pretty report like your example
    static void GenerateSalesSummary(string outputPath, Dictionary<string, double> salesData, double totalSales)
    {
        // for $ formatting like $x,xxx,xxx.xx
        var culture = CultureInfo.GetCultureInfo("en-US");

        var sb = new StringBuilder();
        sb.AppendLine("Sales Summary");
        sb.AppendLine("----------------------------");
        sb.AppendLine($" Total Sales: {totalSales.ToString("C", culture)}");
        sb.AppendLine();
        sb.AppendLine(" Details:");

        // sort by store name (201, 202, …) or by total if you prefer
        foreach (var kvp in SortedByStoreName(salesData))
        {
            sb.AppendLine($"  {kvp.Key}: {kvp.Value.ToString("C", culture)}");
        }

        File.WriteAllText(outputPath, sb.ToString());
    }

    static IEnumerable<KeyValuePair<string,double>> SortedByStoreName(Dictionary<string,double> map)
    {
        var keys = new List<string>(map.Keys);
        keys.Sort(StringComparer.OrdinalIgnoreCase);
        foreach (var k in keys) yield return new KeyValuePair<string,double>(k, map[k]);
    }
}