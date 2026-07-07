using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Shared.Data.Entities.Identity.Log;
using Shared.Enums;
using Shared.Interfaces.IdentityServices;
using Shared.Interfaces.Log;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Shared.Services.Log
{
    public sealed class AuditInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUserService _currentUser;
        private readonly IHttpContextAccessor _httpContext;
        private readonly ILogPipeline _pipeline;

        public AuditInterceptor(ICurrentUserService currentUser, IHttpContextAccessor httpContext,
            ILogPipeline pipeline)
        {
            _currentUser = currentUser;
            _httpContext = httpContext;
            _pipeline = pipeline;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var context = eventData.Context;

            if (context == null)
                return result;

            var entries = context.ChangeTracker.Entries()
                            .Where(x =>
                                x.State == EntityState.Added ||
                                x.State == EntityState.Modified ||
                                x.State == EntityState.Deleted)
                            .ToList();

            foreach (var entry in entries)
            {
                if (entry.Entity is LogBase)
                    continue;

                var audit = BuildAuditLog(entry);

                await _pipeline.EnqueueAsync(audit, cancellationToken);
            }

            return result;
        }

        private AuditLog BuildAuditLog(EntityEntry entry)
        {
            var action = entry.State switch
                {
                    EntityState.Added => AuditActionType.Create,

                    EntityState.Modified => AuditActionType.Update,

                    EntityState.Deleted => AuditActionType.Delete,

                    _ => throw new InvalidOperationException()
                };

            return new AuditLog
            {
                UserId = _currentUser.UserId,

                UserName = _currentUser.UserName,

                ActionType = action,

                TableName =
                    entry.Metadata.GetTableName()!,

                RecordId =
                    entry.Properties.FirstOrDefault(x => x.Metadata.IsPrimaryKey()) ?.CurrentValue? .ToString() ?? "",

                OldValues = JsonSerializer.Serialize(entry.OriginalValues.Properties
                            .ToDictionary(
                                p => p.Name,
                                p => entry.OriginalValues[p])),

                NewValues = JsonSerializer.Serialize(entry.CurrentValues.Properties
                            .ToDictionary(
                                p => p.Name,
                                p => entry.CurrentValues[p])),

                CreatedAt = DateTime.UtcNow
            };
        }
    }
}
