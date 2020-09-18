using System;
using System.Collections.Generic;
using System.Linq;
using GitLite.Repositories.Data;

namespace GitLite.Extensions
{
    public static class CommitExtensions
    {
        public static IEnumerable<CommitItem> FilterByDate(this IEnumerable<CommitItem> query, DateTime? from, DateTime? to)
        {
            if(from.HasValue && to.HasValue)
            {
                return query.Where(t => t.Date >= from && t.Date < to);
            }
            return query;
        }
    }
}
