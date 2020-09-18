using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using GitLite.Repositories;
using GitLite.Repositories.Data;
using GitLite.Storage;
using LibGit2Sharp;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace GitLite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly ConfigStore _configStore = new ConfigStore();

        private GitRepository _gitRepository;

        public MainWindow()
        {
            InitializeComponent();
            LoadRepositories();

            var configSettings = _configStore.Load();
            if (!string.IsNullOrEmpty(configSettings.SelectedRepoName))
            {
                var repoSetting = configSettings.Repositories.FirstOrDefault(t => t.Name == configSettings.SelectedRepoName);
                if (repoSetting != null) cmbRepositories.SelectedIndex = cmbRepositories.Items.IndexOf(repoSetting);
            }
            _configStore.Store(configSettings);
        }

        private void AddSettings()
        {
            var fbd = new CommonOpenFileDialog()
            {
                EnsurePathExists = true,
                EnsureFileExists = false,
                IsFolderPicker = true,
                AllowNonFileSystemItems = false,
                Title = "Select The Folder To Process"
            };

            if (fbd.ShowDialog() == CommonFileDialogResult.Ok)
            {
                if (Repository.IsValid(fbd.FileName))
                {
                    var settings = _configStore.Load();
                    var repoList = settings.Repositories.ToList();

                    if (repoList.Any(t => t.Location == fbd.FileName)) return;

                    using (var repo = new Repository(fbd.FileName))
                    {
                        repoList.Add(new RepoSettings() { Location = fbd.FileName, Name = fbd.FileName.Split('/').LastOrDefault() });
                    }

                    settings.Repositories = repoList.ToArray();
                    _configStore.Store(settings);

                    LoadRepositories();
                }
            }
        }

        private void LoadRepositories()
        {
            var settings = _configStore.Load();
            cmbRepositories.ItemsSource = settings.Repositories;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            AddSettings();
        }

        private void cmbRepositories_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var settings = (RepoSettings)cmbRepositories.SelectedItem;
            if (settings == null) return;

            Title = $"GitLite - {settings.Name}";

            var configSettings = _configStore.Load();
            configSettings.SelectedRepoName = settings.Name;
            _configStore.Store(configSettings);

            _gitRepository = new GitRepository(settings.Location);

            var localChangesCount = _gitRepository.LocalChangesCount();
            var localBranches = _gitRepository.GetBranches(new Repositories.Filters.BranchFilter() { IsRemote = false });
            var remoteBranches = _gitRepository.GetBranches(new Repositories.Filters.BranchFilter() { IsRemote = true });
            var branches = new TreeItem[]
            {
                new TreeItem() { Name = "Local changes", Count = localChangesCount },
                new BranchesItem() { Name = "Local branches", Count = localBranches.Count(), Branches = localBranches, IsNodeExpanded = true },
                new BranchesItem() { Name = "Remote branches", Count = remoteBranches.Count(), Branches = remoteBranches, IsNodeExpanded = false }
            };
            lstBranches.ItemsSource = branches;
        }

        public T FindElementByName<T>(FrameworkElement element, string sChildName) where T : FrameworkElement
        {
            T childElement = null;
            var nChildCount = VisualTreeHelper.GetChildrenCount(element);
            for (int i = 0; i < nChildCount; i++)
            {
                FrameworkElement child = VisualTreeHelper.GetChild(element, i) as FrameworkElement;

                if (child == null)
                    continue;

                if (child is T && child.Name.Equals(sChildName))
                {
                    childElement = (T)child;
                    break;
                }

                childElement = FindElementByName<T>(child, sChildName);

                if (childElement != null)
                    break;
            }
            return childElement;
        }

        private void lstBranches_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            BranchItem selectedItem = lstBranches.SelectedItem as BranchItem;
            if (selectedItem == null) return;

            var configSettings = _configStore.Load();
            configSettings.SelectedBranchName = selectedItem.Name;
            _configStore.Store(configSettings);

            ContentControl.Template = this.FindResource("CommitView") as ControlTemplate;
            var lstCommits = FindElementByName<ListView>(ContentControl, "lstCommits");
            if (lstCommits.ItemsSource == null) lstCommits.Items.Clear();

            lstCommits.ItemsSource = _gitRepository.GetCommits(selectedItem.Name, new Repositories.Filters.CommitFilter() { }).Take(20);

            if (lstCommits.Items.Count > 0)
            {
                lstCommits.SelectedIndex = 0;
            }
        }

        private void lstCommits_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems.Count != 1) return;
            var selected = e.AddedItems[0];
            var commitItem = selected as CommitItem;
            var repoSettings = (RepoSettings)cmbRepositories.SelectedItem;
            if (repoSettings == null) return;

            using (var repo = new Repository(repoSettings.Location))
            {
                var commit1 = repo.Lookup<Commit>(commitItem.Id);
                if (commit1 == null || !commit1.Parents.Any()) return;
                var commit2 = commit1.Parents.First();

                var lstFiles = FindElementByName<ListView>(ContentControl, "lstFiles");
                var patch = repo.Diff.Compare<TreeChanges>(commit1.Tree, commit2.Tree);
                if (lstFiles.ItemsSource == null) lstFiles.Items.Clear();
                lstFiles.ItemsSource = patch.Select(t => new PatchItem() { FileName = t.Path, Status = GetStatusString(t.Status) });
            }
        }

        private string GetStatusString(ChangeKind kind)
        {
            return kind switch
            {
                ChangeKind.Added => "+",
                ChangeKind.Deleted => "-",
                ChangeKind.Modified => "c",
                ChangeKind.Renamed => "r",
                _ => string.Empty
            };
        }

        private string GetStatusString(FileStatus kind)
        {
            return kind switch
            {
                FileStatus.NewInWorkdir => "+",
                FileStatus.Conflicted => "co",
                FileStatus.ModifiedInWorkdir => "m",
                FileStatus.DeletedFromWorkdir => "-",
                _ => string.Empty
            };
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            var process = new Process();
            process.StartInfo.FileName = "bash";
            process.StartInfo.EnvironmentVariables["HOME"] = "%USERPROFILE%";
            process.Start();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = e.Source as BranchItem;
            var selectedRepo = (RepoSettings)cmbRepositories.SelectedItem;
            if (selectedRepo == null) return;

            var configSettings = _configStore.Load();
            configSettings.SelectedBranchName = selectedItem.Name;
            _configStore.Store(configSettings);

            using (var gitRepo = new Repository(selectedRepo.Location))
            {
                var branch = gitRepo.Branches[(string)selectedItem.Name];
            }
        }

        private void lstFiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var fileContent = FindElementByName<TextBox>(ContentControl, "txtFileContent");
            if (fileContent == null) return;
            fileContent.Clear();

            var selectedRepo = (RepoSettings)cmbRepositories.SelectedItem;
            if (selectedRepo == null) return;

            using (var repo = new Repository(selectedRepo.Location))
            {
                var lstCommits = FindElementByName<ListView>(ContentControl, "lstCommits");
                if (lstCommits == null) return;

                var commitItem = lstCommits.SelectedItem as CommitItem;

                if (commitItem == null) return;
                if (e.AddedItems.Count == 0) return;

                var file = e.AddedItems[0] as PatchItem;
                if (file == null) return;

                var commit = repo.Lookup<Commit>(commitItem.Id);
                if (commit == null) return;
                var treeEntry = commit[file.FileName];

                if (treeEntry == null) return;
                if (treeEntry.TargetType != TreeEntryTargetType.Blob) return;

                var blob = (Blob)treeEntry.Target;
                if (blob.IsBinary) return;

                var contentStream = blob.GetContentStream();
                if (blob.Size != contentStream.Length) return;

                using (var tr = new StreamReader(contentStream, Encoding.UTF8))
                {
                    string content = tr.ReadToEnd();
                    fileContent.Text = content;
                }
            }
        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            ContentControl.Template = FindResource("LocalChangesView") as ControlTemplate;
        }

        private readonly FileStatus[] validStates = new FileStatus[] { FileStatus.DeletedFromWorkdir, FileStatus.Conflicted, FileStatus.DeletedFromWorkdir, FileStatus.RenamedInWorkdir, FileStatus.NewInWorkdir };
        private void lstLocalFiles_Loaded(object sender, RoutedEventArgs e)
        {
            var selectedRepo = (RepoSettings)cmbRepositories.SelectedItem;
            if (selectedRepo == null) return;
            using (var gitRepo = new Repository(selectedRepo.Location))
            {
                var lstFiles = FindElementByName<ListView>(ContentControl, "lstLocalFiles");
                var addedFiles = gitRepo.RetrieveStatus().Where(t => validStates.Contains(t.State)).Select(t => new PatchItem() { FileName = t.FilePath, Status = GetStatusString(t.State) });

                lstFiles.ItemsSource = addedFiles;
            }
        }

        private void MenuItemStat_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = lstBranches.SelectedItem as BranchItem;
            if (selectedItem == null) return;

            var window = new Statistics(_gitRepository.GetPath(), selectedItem.Name);
            window.Show();
        }
    }
}
