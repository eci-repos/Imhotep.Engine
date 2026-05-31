namespace Imhotep.Repository.Configuration;

/// <summary>
/// ISL v3.0: Structured configuration options for the Artifact Repository.
/// </summary>
public class ArtifactRepositoryOptions
{
   public string BaseDirectory { get; set; } = string.Empty;
   public string ConnectionString { get; set; } = string.Empty;
}
