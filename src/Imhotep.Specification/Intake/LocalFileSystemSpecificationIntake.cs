using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Imhotep.Specification.Intake;

/// <summary>
/// Configuration options for the local file system intake boundary.
/// </summary>
public class LocalIntakeOptions
{
   public string BaseDirectory { get; set; } = string.Empty;
}

/// <summary>
/// ISL v3.0: Concrete implementation of ISpecificationIntake for the MACS local-first environment [3, 4].
/// Manages STP ingestion and state transitions using local file system directories.
/// </summary>
public class LocalFileSystemSpecificationIntake : ISpecificationIntake
{
   private readonly string _baseDirectory;
   private readonly ILogger<LocalFileSystemSpecificationIntake> _logger;

   public LocalFileSystemSpecificationIntake(
       IOptions<LocalIntakeOptions> options,
       ILogger<LocalFileSystemSpecificationIntake> logger)
   {
      if (string.IsNullOrWhiteSpace(options.Value.BaseDirectory))
         throw new ArgumentException("BaseDirectory must be configured.", nameof(options));

      _baseDirectory = options.Value.BaseDirectory;
      _logger = logger ?? throw new ArgumentNullException(nameof(logger));

      EnsureDirectoriesExist();
   }

   // --- INITIALIZATION ---

   /// <summary>
   /// Automatically maps the IntakeState enum to physical folders (e.g., /Pending, /Admitted) 
   /// to maintain durable state transition memory [5].
   /// </summary>
   private void EnsureDirectoriesExist()
   {
      foreach (IntakeState state in Enum.GetValues(typeof(IntakeState)))
      {
         Directory.CreateDirectory(GetDirectoryForState(state));
      }
   }

   private string GetDirectoryForState(IntakeState state) =>
       Path.Combine(_baseDirectory, state.ToString());

   /// <summary>
   /// Enforces Zero-Trust by sanitizing inputs to prevent directory traversal attacks [2].
   /// </summary>
   private string SanitizeFileName(string identifier)
   {
      var invalidChars = Path.GetInvalidFileNameChars();
      var sanitized = new string(identifier.Where(c => !invalidChars.Contains(c)).ToArray());

      if (!sanitized.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
      {
         sanitized += ".md";
      }
      return sanitized;
   }

   // --- SINGLE SUBMISSION ---

   public async Task<PendingPayloadRecord?> ReceivePayloadAsync(string sourceIdentifier, CancellationToken cancellationToken = default)
   {
      var safeFileName = SanitizeFileName(sourceIdentifier);
      var filePath = Path.Combine(GetDirectoryForState(IntakeState.Pending), safeFileName);

      if (!File.Exists(filePath))
      {
         _logger.LogWarning("Requested payload not found at {FilePath}", filePath);
         return null;
      }

      return await ReadPayloadRecordAsync(filePath, cancellationToken);
   }

   // --- AUTOMATED BACKGROUND POLLING ---

   public async Task<IEnumerable<PendingPayloadRecord>> GetPendingPayloadsAsync(CancellationToken cancellationToken = default)
   {
      var pendingDir = GetDirectoryForState(IntakeState.Pending);
      var files = Directory.GetFiles(pendingDir, "*.md");
      var records = new List<PendingPayloadRecord>();

      foreach (var file in files)
      {
         var record = await ReadPayloadRecordAsync(file, cancellationToken);
         if (record != null)
         {
            records.Add(record);
         }
      }

      return records;
   }

   #region -- State Transition Logic with Durable Memory

   public Task UpdatePayloadStateAsync(string transactionId, IntakeState newState, CancellationToken cancellationToken = default)
   {
      var safeFileName = SanitizeFileName(transactionId);
      string? currentFilePath = null;

      // Locate the file across any of the known state directories
      foreach (IntakeState state in Enum.GetValues(typeof(IntakeState)))
      {
         var testPath = Path.Combine(GetDirectoryForState(state), safeFileName);
         if (File.Exists(testPath))
         {
            currentFilePath = testPath;
            break;
         }
      }

      if (currentFilePath == null)
      {
         _logger.LogError("Cannot update state for {TransactionId}: File not found.", transactionId);
         throw new FileNotFoundException($"Payload for {transactionId} not found.");
      }

      var targetDir = GetDirectoryForState(newState);
      var targetFilePath = Path.Combine(targetDir, safeFileName);

      // Execute the physical state transition
      if (currentFilePath != targetFilePath)
      {
         File.Move(currentFilePath, targetFilePath, overwrite: true);
         _logger.LogInformation("Moved payload {TransactionId} to state {NewState}.", transactionId, newState);
      }

      return Task.CompletedTask;
   }

   #endregion

   // --- UTILITY ---

   private async Task<PendingPayloadRecord> ReadPayloadRecordAsync(string filePath, CancellationToken cancellationToken)
   {
      try
      {
         var rawMarkdown = await File.ReadAllTextAsync(filePath, cancellationToken);
         var transactionId = Path.GetFileNameWithoutExtension(filePath);

         return new PendingPayloadRecord(
            transactionId,
            rawMarkdown,
            filePath,
            DateTime.UtcNow
         );
      }
      catch (Exception ex)
      {
         _logger.LogError(ex, "Failed to read payload file: {FilePath}", filePath);
         throw;
      }
   }
}
