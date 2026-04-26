using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Imhotep.Common.Models;

namespace Imhotep.Common.Services;

/// <summary>
/// Preserves operational context and historical execution records to support 
/// long-running autonomous workflows globally across all subsystems (ISL v2.2).
/// </summary>
public interface IStateAndMemoryService
{
   Task UpdateStateAsync(PlatformState state);
   Task<PlatformState?> GetCurrentStateAsync(string transactionId, StateCategory category);
   Task RecordMemoryEventAsync(MemoryRecord record);
   Task<IReadOnlyList<MemoryRecord>> GetTaskMemoryHistoryAsync(string transactionId, string taskId);
   Task<string> CreateVersionSnapshotAsync(string transactionId, string versionTag);
   Task RestoreFromSnapshotAsync(string transactionId, string snapshotId);
}

public class StateAndMemoryService : IStateAndMemoryService
{
   private readonly ILogger<StateAndMemoryService> _logger;

   private readonly Dictionary<string, Dictionary<StateCategory, PlatformState>> _activeStateStore = new();
   private readonly List<MemoryRecord> _memoryEventLog = new();

   public StateAndMemoryService(ILogger<StateAndMemoryService> logger)
   {
      _logger = logger;
   }

   public Task UpdateStateAsync(PlatformState state)
   {
      _logger.LogInformation("Updating {Category} State for Transaction {TransactionId}. Status: {Status}",
          state.Category, state.TransactionId, state.CurrentStatus);

      if (!_activeStateStore.ContainsKey(state.TransactionId))
      {
         _activeStateStore[state.TransactionId] = new Dictionary<StateCategory, PlatformState>();
      }

      _activeStateStore[state.TransactionId][state.Category] = state;
      return Task.CompletedTask;
   }

   public Task<PlatformState?> GetCurrentStateAsync(string transactionId, StateCategory category)
   {
      if (_activeStateStore.TryGetValue(transactionId, out var transactionStates) &&
          transactionStates.TryGetValue(category, out var state))
      {
         return Task.FromResult<PlatformState?>(state);
      }

      return Task.FromResult<PlatformState?>(null);
   }

   public Task RecordMemoryEventAsync(MemoryRecord record)
   {
      _logger.LogDebug("Recording Memory Event {RecordId} by Actor {ActorId} for Task {TaskId}",
          record.RecordId, record.ActorId, record.TaskId);

      _memoryEventLog.Add(record);
      return Task.CompletedTask;
   }

   public Task<IReadOnlyList<MemoryRecord>> GetTaskMemoryHistoryAsync(string transactionId, string taskId)
   {
      var history = _memoryEventLog
          .Where(r => r.TransactionId == transactionId && r.TaskId == taskId)
          .OrderBy(r => r.Timestamp)
          .ToList();

      return Task.FromResult<IReadOnlyList<MemoryRecord>>(history);
   }

   public Task<string> CreateVersionSnapshotAsync(string transactionId, string versionTag)
   {
      _logger.LogInformation("Creating persistent state snapshot for Transaction {TransactionId} [Version: {VersionTag}]", transactionId, versionTag);

      string snapshotId = $"SNAP-{versionTag}-{Guid.NewGuid().ToString("N").Substring(0, 8)}";
      return Task.FromResult(snapshotId);
   }

   public Task RestoreFromSnapshotAsync(string transactionId, string snapshotId)
   {
      _logger.LogWarning("Restoring Transaction {TransactionId} operational state from durable snapshot {SnapshotId}", transactionId, snapshotId);

      return Task.CompletedTask;
   }
}
