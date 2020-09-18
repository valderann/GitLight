namespace GitLite.Repositories.Data
{
    public class TreeItem
    {
        public string Name { get; set; }
        public int Count { get; set; }
        public bool IsNodeExpanded { get; set; }
    }

    public class LocalItem : TreeItem
    { 
    }

    public class BranchesItem : TreeItem
    {
        public BranchItem[] Branches { get; set; }
    }

    public class BranchItem : TreeItem
    {
        public bool IsTracking { get; set; }
        public bool IsCurrent { get; set; }
        public int AheadBy { get; set; }
        public int BehindBy { get; set; }

        public bool IsAhead => AheadBy > 0;
        public bool IsBehind => BehindBy > 0;
    }
}
