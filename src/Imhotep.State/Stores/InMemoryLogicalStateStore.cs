using Imhotep.State.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.State.Stores
{
   /// <summary>
   /// ISL v2.2: In-Memory Logical State Store implementation for the MACS POC.
   /// Provides thread-safe, local-first state persistence to support parallel execution loops.
   /// </summary>
   public class InMemoryLogicalStateStore<TRecord> : ILogicalStateStore<TRecord> where TRecord : class
   {
      // Thread-safe dictionary to handle parallel task execution safely
      private readonly ConcurrentDictionary<string, TRecord> _store = new(StringComparer.OrdinalIgnoreCase);

      public Task<TRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();

         _store.TryGetValue(id, out var record);
         return Task.FromResult(record);
      }

      public Task<IReadOnlyList<TRecord>> FindAsync(Func<TRecord, bool> predicate, CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();

         // Execute the predicate and safely cast to the strictly required immutable IReadOnlyList
         var results = _store.Values.Where(predicate).ToList().AsReadOnly();
         return Task.FromResult<IReadOnlyList<TRecord>>(results);
      }

      public Task UpsertAsync(string id, TRecord record, CancellationToken cancellationToken = default)
      {
         cancellationToken.ThrowIfCancellationRequested();

         _store[id] = record;
         return Task.CompletedTask; // Emulates an ISL v2.2 state transition transaction commit
      }
   }
}
