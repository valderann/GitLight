using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using GitLite.Extensions;
using GitLite.Repositories;
using LiveCharts;
using LiveCharts.Configurations;
using LiveCharts.Wpf;

namespace GitLite
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Statistics : Window
    {
        private readonly string _branchName;
        private readonly GitRepository _gitRepo;

        public Statistics(string path, string branchName)
        {
            InitializeComponent();

            Title = $"Statistics - {branchName}";

            _branchName = branchName;
            _gitRepo = new GitRepository(path);

            LoadCharts();
        }

        private void LoadCharts()
        {
            var commits = _gitRepo.GetCommits(_branchName, new Repositories.Filters.CommitFilter() { });
            chartView.Visibility = Visibility.Visible;

            var from = fromDatePicker.SelectedDate;
            var to = toDatePicker.SelectedDate;

            var totalCommitsByAuthor = commits.FilterByDate(from, to).GroupBy(t => NormalizeName(t.Author), (t, v) => new AuthorCount() { AuthorName = t, Count = v.Count() }).ToArray();
            PrintAuthorLines(ByUser, totalCommitsByAuthor);

            var commitsPerMonth = commits.FilterByDate(from, to).GroupBy(t => new { Date = new DateTime(t.Date.Year, t.Date.Month, 1) }, (t, v) => new DateModel { DateTime = t.Date, Value = v.Count() }).OrderBy(t => t.DateTime).ToArray();
            PrintLineSeries(commitsTime, commitsPerMonth);
        }

        public static T GetChildOfType<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        private static string NormalizeName(string name)
            => name.Replace(".", "").Replace(" ", "").ToLower();

        private static void PrintLineSeries(CartesianChart chart, DateModel[] values)
        {
            var dayConfig = Mappers.Xy<DateModel>()
                .X(dayModel => (double)dayModel.DateTime.Ticks / TimeSpan.FromDays(1).Ticks)
                .Y(dayModel => dayModel.Value);

            chart.Series.Clear();
            var total = values.Sum(t => t.Value);
            chart.Series = new SeriesCollection(dayConfig)
            {
                new LineSeries()
                {
                    Title = $"Commits by month",
                    Values = new ChartValues<DateModel>(values.AsEnumerable()),
                }
            };

            chart.AxisX.Clear();
            chart.AxisX.Add(new Axis
            {
                LabelFormatter = value => new DateTime((long)(value * TimeSpan.FromDays(1).Ticks)).ToShortDateString()
            });
        }

        public class DateModel
        {
            public System.DateTime DateTime { get; set; }
            public int Value { get; set; }
        }

        private static void PrintAuthorLines(LiveCharts.Wpf.PieChart chart, IEnumerable<AuthorCount> results)
        {
            chart.Series.Clear();
            var total = results.Sum(t => t.Count);
            foreach (var result in results.OrderByDescending(t => t.Count))
            {
                var percentage = result.Count == 0 ? 0 : Math.Round(((decimal)result.Count / (decimal)total) * 100, 2);
                chart.Series.Add(new PieSeries() { Title = $"{result.AuthorName } {percentage}%", Values = new ChartValues<long>(new List<long>() { result.Count }.AsEnumerable()) });
            }
        }

        private class AuthorCount
        {
            public string AuthorName { get; set; }
            public int Count { get; set; }
        }
    }
}
