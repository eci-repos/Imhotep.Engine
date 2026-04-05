namespace Tests.Library;

public class FolderFileHelper
{

   /// <summary>
   /// Reads a specification payload from the Artifacts folder.
   /// </summary>
   public static async Task<string> ReadFileAsync(string folder, string fileName)
   {
      var filePath = Path.Combine(folder, fileName);

      if (!File.Exists(filePath))
      {
         throw new FileNotFoundException(
            $"The test artifact '{fileName}' was not found in the Artifacts directory. "
            + "Ensure it is set to 'Copy to Output Directory'.", filePath);
      }

      return await File.ReadAllTextAsync(filePath);
   }

}
