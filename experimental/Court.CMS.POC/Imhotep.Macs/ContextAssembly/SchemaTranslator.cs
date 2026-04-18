
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Imhotep.Macs.ContestAssembly;

/// <summary>
/// Middleware utility to automate the conversion of the Psj_Court_Canonical CSV schema 
/// into strict ISL-formatted DataEntity Markdown blocks for the STP.
/// </summary>
public class SchemaTranslator
{

   public void GenerateDataEntityMarkdown(string csvFilePath, string outputPath)
   {
      // Read all lines and skip the CSV header row
      var lines = File.ReadAllLines(csvFilePath).Skip(1);

      // Dictionary to group columns by Table Name
      var tables = new Dictionary<string, List<string>>();

      foreach (var line in lines)
      {
         if (string.IsNullOrWhiteSpace(line)) continue;

         // Split the CSV line based on the known NODS canonical schema structure
         var columns = line.Split(',');
         if (columns.Length < 9) continue;

         // Extract mapping targets based on the CSV column ordinal positions
         string tableName = columns[2].Trim();       // TABLE_NAME
         string columnName = columns[3].Trim();      // COLUMN_NAME
         string dataType = columns[4].Trim();        // DATA_TYPE
         string constraint = columns[5].Trim();      // CONSTRAINT_TYPE

         if (!tables.ContainsKey(tableName))
         {
            tables[tableName] = new List<string>();
         }

         // Format the individual column entry
         string fieldDescription = $"- `{columnName}` ({dataType})";

         // Append constraint identifiers if they exist (e.g., [PRIMARY KEY], [FOREIGN KEY])
         if (!string.IsNullOrWhiteSpace(constraint))
         {
            fieldDescription += $" [{constraint}]";
         }

         tables[tableName].Add(fieldDescription);
      }

      var markdownBuilder = new StringBuilder();
      markdownBuilder.AppendLine("**DataEntity**");

      // Automatically format the extracted elements into ISL-compliant entity blocks
      foreach (var table in tables)
      {
         // Generate the mandatory Traceability Identifier (e.g., ENT-NODS-CASEDETAIL)
         string entityId = $"**ENT-NODS-{table.Key.ToUpper()}**";

         markdownBuilder.AppendLine($"{entityId}: {table.Key} - NCSC NODS canonical table mapped from Psj_Court_Canonical schema.");

         foreach (var column in table.Value)
         {
            markdownBuilder.AppendLine(column);
         }
         markdownBuilder.AppendLine(); // Blank line for markdown spacing between entities
      }

      // Write the finalized ISL payload section to disk
      File.WriteAllText(outputPath, markdownBuilder.ToString());

      Console.WriteLine($"SUCCESS: ISL DataEntity Markdown generated at: {outputPath}");
      Console.WriteLine($"Total Canonical Entities Processed: {tables.Count}");
   }

}

