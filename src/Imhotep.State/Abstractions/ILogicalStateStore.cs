using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Imhotep.State.Abstractions
{
   /// <summary>
   /// ISL v2.2 Sec 6.1: Logical Store Abstraction.
   /// Provides generic, durable state persistence applicable to any platform resource.
   /// </summary>
   public interface ILogicalStateStore<TRecord> where TRecord : class
   {
      Task<TRecord?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

      Task<IReadOnlyList<TRecord>> FindAsync(Func<TRecord, bool> predicate, CancellationToken cancellationToken = default);

      Task UpsertAsync(string id, TRecord record, CancellationToken cancellationToken = default);
   }
}
