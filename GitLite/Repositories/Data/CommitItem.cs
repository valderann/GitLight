using System;

namespace GitLite.Repositories.Data
{
    public class CommitItem
    {
        public string Message { get; set; }
        public DateTimeOffset Date { get; set; }
        public string Author { get; set; }
        public string Id { get; set; }
    }
}
