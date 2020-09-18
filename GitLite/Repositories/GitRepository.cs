using System;
using System.Linq;
using GitLite.Repositories.Data;
using GitLite.Repositories.Filters;
using LibGit2Sharp;

namespace GitLite.Repositories
{
    public class GitRepository
    {
        private readonly Repository _repo;
        private readonly string _path;
        private readonly FileStatus[] validStates = new FileStatus[] { FileStatus.DeletedFromWorkdir, FileStatus.Conflicted, FileStatus.DeletedFromWorkdir, FileStatus.RenamedInWorkdir, FileStatus.NewInWorkdir };

        public GitRepository(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("Invalid path", nameof(path));
            _path = path;
            _repo = new Repository(path);
        }

        public string GetPath() => _path;

        public BranchItem[] GetBranches(BranchFilter filter = null)
        {
            var branchQuery = _repo.Branches.AsQueryable();
            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.SearchText)) branchQuery = branchQuery.Where(t => t.FriendlyName.Contains(filter.SearchText));
                if (filter.IsRemote != null) branchQuery = branchQuery.Where(t => t.IsRemote == filter.IsRemote);
            }
            return branchQuery.Select(t => new BranchItem() { Name = t.FriendlyName, IsCurrent = t.IsCurrentRepositoryHead, IsTracking = t.IsTracking, AheadBy = t.TrackingDetails.AheadBy ?? 0, BehindBy = t.TrackingDetails.BehindBy ?? 0 }).ToArray();
        }

        public PatchItem[] LocalChanges()
            => _repo.RetrieveStatus().Select(t => new PatchItem() { FileName = t.FilePath, Status = t.State.ToString() }).ToArray();

        public int LocalChangesCount()
            => _repo.RetrieveStatus().Count();

        public IQueryable<CommitItem> GetCommits(string branchName, Filters.CommitFilter filter)
        {
            var commitQuery = _repo.Branches[branchName].Commits.AsQueryable();

            if (filter != null)
            {
                if (!string.IsNullOrWhiteSpace(filter.SearchText)) commitQuery = commitQuery.Where(t => t.Message.Contains(filter.SearchText));
            }

            return commitQuery.Select(t => new CommitItem()
            {
                Message = t.MessageShort,
                Author = t.Author.Name,
                Id = t.Id.Sha,
                Date = t.Author.When.UtcDateTime
            });
        }
    }
}
