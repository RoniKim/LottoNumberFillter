using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using LottoNumber.Conditions;
using LottoNumber.Services;
using Microsoft.Win32;

namespace LottoNumber.ViewModels
{
    public sealed class ConditionOption : INotifyPropertyChanged
    {
        public ICondition Condition { get; }
        public string Key => Condition.Key;

        private string _displayName;
        public string DisplayName
        {
            get { return _displayName; }
            set { if (_displayName != value) { _displayName = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DisplayName))); } }
        }

        public ConditionOption(ICondition condition)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
            _displayName = condition.DisplayName;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public override string ToString() => DisplayName;
    }

    public sealed class ConditionInstance : INotifyPropertyChanged
    {
        public ICondition Condition { get; }
        public string Key => Condition.Key;
        public string Name => Condition.DisplayName;

        public Dictionary<string, string> Parameters { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { if (_isEnabled != value) { _isEnabled = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsEnabled))); } }
        }

        public string Summary
        {
            get
            {
                if (Parameters == null || Parameters.Count == 0) return Name;
                var joined = string.Join(", ", Parameters.Select(p => p.Key + "=" + p.Value));
                return Name + " (" + joined + ")";
            }
        }

        public ConditionInstance(ICondition condition)
        {
            Condition = condition ?? throw new ArgumentNullException(nameof(condition));
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public sealed class ParamEntry : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Label { get; set; }

        private string _value;
        public string Value
        {
            get { return _value; }
            set { if (_value != value) { _value = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value))); } }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }

    public class MainWindowViewModel : ViewModelBase
    {
        private const int TotalCombinations = 8145060; // 45개 중 6개를 선택하는 전체 조합 수

        public ObservableCollection<ConditionInstance> Conditions { get; } = new ObservableCollection<ConditionInstance>();

        private ConditionInstance _selectedCondition;
        public ConditionInstance SelectedCondition
        {
            get { return _selectedCondition; }
            set { _selectedCondition = value; Raise(nameof(SelectedCondition)); }
        }

        public ObservableCollection<ConditionOption> ConditionOptions { get; }
        public ObservableCollection<ParamEntry> SelectedParameters { get; } = new ObservableCollection<ParamEntry>();
        public ObservableCollection<NumberProbabilityItem> WinningNumberStats { get; } = new ObservableCollection<NumberProbabilityItem>();

        private ConditionOption _selectedConditionOption;
        public ConditionOption SelectedConditionOption
        {
            get { return _selectedConditionOption; }
            set
            {
                _selectedConditionOption = value;
                Raise(nameof(SelectedConditionOption));
                Raise(nameof(SelectedConditionDescription));

                SelectedParameters.Clear();
                var specs = SelectedConditionOption?.Condition?.ParameterSpecs;
                if (specs != null)
                {
                    foreach (var spec in specs)
                        SelectedParameters.Add(new ParamEntry { Name = spec.Name, Label = spec.Label, Value = spec.DefaultValue });
                }
            }
        }

        public string SelectedConditionDescription => "상세: " + (SelectedConditionOption?.Condition?.Description ?? string.Empty);

        public sealed class PageItem : INotifyPropertyChanged
        {
            private bool _isMarked;
            public bool IsMarked
            {
                get { return _isMarked; }
                set { if (_isMarked != value) { _isMarked = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsMarked))); } }
            }

            public string Text { get; set; }
            public event PropertyChangedEventHandler PropertyChanged;
        }

        public sealed class NumberProbabilityItem
        {
            public NumberProbabilityItem(int number, int count, double rate)
            {
                Number = number;
                Count = count;
                Rate = rate;
            }

            public int Number { get; }
            public int Count { get; }
            public double Rate { get; }
            public string Display => $"{Number}({(Rate * 100.0):0.#}%)";
        }

        private sealed class ConditionSnapshot
        {
            public ConditionSnapshot(ICondition condition, Dictionary<string, string> parameters)
            {
                Condition = condition;
                Parameters = parameters;
            }

            public ICondition Condition { get; }
            public Dictionary<string, string> Parameters { get; }
        }

        private static readonly StringComparer ParameterComparer = StringComparer.OrdinalIgnoreCase;

        private IReadOnlyList<ConditionSnapshot> CaptureActiveConditionSnapshots()
        {
            if (Conditions.Count == 0) return Array.Empty<ConditionSnapshot>();

            var list = new List<ConditionSnapshot>(Conditions.Count);
            foreach (var instance in Conditions)
            {
                if (instance == null || !instance.IsEnabled || instance.Condition == null)
                    continue;

                var parameters = instance.Parameters.Count > 0
                    ? new Dictionary<string, string>(instance.Parameters, ParameterComparer)
                    : new Dictionary<string, string>(ParameterComparer);

                list.Add(new ConditionSnapshot(instance.Condition, parameters));
            }
            return list;
        }

        private HashSet<string> CaptureRemovedSnapshot()
        {
            return _removed.Count == 0 ? null : new HashSet<string>(_removed, StringComparer.Ordinal);
        }

        private ObservableCollection<PageItem> _filteredPreview = new ObservableCollection<PageItem>();
        public ObservableCollection<PageItem> FilteredPreview
        {
            get { return _filteredPreview; }
            private set { _filteredPreview = value; Raise(nameof(FilteredPreview)); }
        }

        private const int FixedPageSize = 50;
        public int PageSize => FixedPageSize;

        private int _pageIndex;
        public int PageIndex
        {
            get { return _pageIndex; }
            set
            {
                var next = value < 0 ? 0 : value;
                if (_pageIndex != next)
                {
                    _pageIndex = next;
                    Raise(nameof(PageIndex));
                    Raise(nameof(CanPrev));
                    Raise(nameof(CanNext));
                    Raise(nameof(PageSummary));
                }
            }
        }

        private bool _hasNextPage;
        public bool HasNextPage
        {
            get { return _hasNextPage; }
            private set { if (_hasNextPage != value) { _hasNextPage = value; Raise(nameof(HasNextPage)); } }
        }

        private readonly HashSet<string> _removed = new HashSet<string>(StringComparer.Ordinal);

        public ICommand AddConditionCommand { get; }
        public ICommand RemoveSelectedConditionCommand { get; }
        public ICommand ApplyFiltersCommand { get; }
        public ICommand ExportFilteredCsvCommand { get; }
        public ICommand NextPageCommand { get; }
        public ICommand PrevPageCommand { get; }
        public ICommand GoToPageCommand { get; }
        public ICommand DeleteSelectedPageItemsCommand { get; }
        public ICommand CancelCommand { get; }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy; }
            private set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    Raise(nameof(IsBusy));
                    Raise(nameof(CanInteract));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool CanInteract => !IsBusy;

        private int _progressPercent;
        public int ProgressPercent
        {
            get { return _progressPercent; }
            private set { if (_progressPercent != value) { _progressPercent = value; Raise(nameof(ProgressPercent)); } }
        }

        private int _totalMatches;
        public int TotalMatches
        {
            get { return _totalMatches; }
            private set { if (_totalMatches != value) { _totalMatches = value; Raise(nameof(TotalMatches)); Raise(nameof(PageSummary)); Raise(nameof(TotalMatchesSummary)); } }
        }

        public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalMatches / (double)PageSize);
        public string PageSummary => $"Page {PageIndex + 1} / {Math.Max(1, TotalPages)}";
        public string TotalMatchesSummary => $"총 {TotalMatches.ToString("N0")}개";
        public bool CanPrev => !IsBusy && PageIndex > 0;
        public bool CanNext => !IsBusy && (PageIndex + 1) < TotalPages;

        private readonly List<List<PageItem>> _pagesCache = new List<List<PageItem>>();
        private System.Threading.CancellationTokenSource _cts;

        public string PageInput { get; set; } = "1";

        public MainWindowViewModel()
        {
            ConditionOptions = new ObservableCollection<ConditionOption>(ConditionDiscovery.DiscoverAll().Select(c => new ConditionOption(c)));
            SelectedConditionOption = ConditionOptions.FirstOrDefault();
            LoadWinningNumberStats();

            AddConditionCommand = new RelayCommand(_ => AddCondition());
            RemoveSelectedConditionCommand = new RelayCommand(_ => RemoveSelected(), _ => SelectedCondition != null);
            ApplyFiltersCommand = new RelayCommand(_ => ApplyFilters(), _ => CanInteract);
            ExportFilteredCsvCommand = new RelayCommand(_ => ExportFilteredCsv(), _ => CanInteract);
            NextPageCommand = new RelayCommand(_ => ShowPage(PageIndex + 1), _ => CanNext);
            PrevPageCommand = new RelayCommand(_ => ShowPage(Math.Max(0, PageIndex - 1)), _ => CanPrev);
            GoToPageCommand = new RelayCommand(_ => GoToPage(), _ => CanInteract);
            DeleteSelectedPageItemsCommand = new RelayCommand(_ => DeleteSelectedFromPage(), _ => CanInteract);
            CancelCommand = new RelayCommand(_ => CancelWork(), _ => IsBusy);
        }

        private void AddCondition()
        {
            try
            {
                if (SelectedConditionOption == null) return;

                var instance = new ConditionInstance(SelectedConditionOption.Condition)
                {
                    IsEnabled = true
                };

                foreach (var entry in SelectedParameters)
                    instance.Parameters[entry.Name] = entry.Value;

                Conditions.Add(instance);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "조건 추가 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void RemoveSelected()
        {
            if (SelectedCondition == null) return;
            Conditions.Remove(SelectedCondition);
            SelectedCondition = null;
        }

        private static bool ShouldExclude(int[] combo, IReadOnlyList<ConditionSnapshot> conditions, HashSet<string> removed)
        {
            if (conditions != null)
            {
                for (int i = 0; i < conditions.Count; i++)
                {
                    var snapshot = conditions[i];
                    if (snapshot.Condition == null)
                        continue;

                    if (snapshot.Condition.Evaluate(combo, snapshot.Parameters))
                        return true;
                }
            }

            return removed != null && removed.Contains(Signature(combo));
        }

        private void LoadWinningNumberStats()
        {
            try
            {
                WinningNumberStats.Clear();
                var stats = WinningHistoryCache.Instance.GetNumberStatistics();
                if (stats == null || stats.Count == 0)
                    return;

                foreach (var stat in stats.OrderBy(s => s.Number))
                    WinningNumberStats.Add(new NumberProbabilityItem(stat.Number, stat.Count, stat.Rate));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "당첨 통계 로드 실패", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ResetPagingState()
        {
            _pagesCache.Clear();
            _removed.Clear();
            FilteredPreview = new ObservableCollection<PageItem>();
            PageIndex = 0;
            TotalMatches = 0;
            HasNextPage = false;
        }

        private void ApplyFilters()
        {
            if (!CanInteract) return;

            var requestedPage = PageIndex;
            var pageSize = PageSize;

            CancelWork();
            ResetPagingState();

            var activeConditions = CaptureActiveConditionSnapshots();
            var removedSnapshot = CaptureRemovedSnapshot();

            _cts = new System.Threading.CancellationTokenSource();
            var token = _cts.Token;
            IsBusy = true;
            ProgressPercent = 0;

            Task.Run(() =>
            {
                try
                {
                    var pages = new List<List<PageItem>>();
                    long visited = 0;
                    int matched = 0;

                    foreach (var combo in CombinationGenerator.All6of45())
                    {
                        token.ThrowIfCancellationRequested();
                        visited++;

                        if (ShouldExclude(combo, activeConditions, removedSnapshot))
                        {
                            if ((visited & 65535L) == 0) UpdateProgress(visited);
                            continue;
                        }

                        var comboText = string.Join(",", combo);
                        var pageIndex = matched / pageSize;
                        while (pages.Count <= pageIndex)
                            pages.Add(new List<PageItem>(pageSize));

                        pages[pageIndex].Add(new PageItem { Text = comboText });
                        matched++;

                        if ((visited & 65535L) == 0) UpdateProgress(visited);
                    }

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        _pagesCache.Clear();
                        foreach (var page in pages)
                            _pagesCache.Add(page);

                        TotalMatches = matched;
                        if (_pagesCache.Count == 0)
                        {
                            PageIndex = 0;
                            FilteredPreview = new ObservableCollection<PageItem>();
                        }
                        else
                        {
                            if (requestedPage >= _pagesCache.Count)
                                requestedPage = _pagesCache.Count - 1;

                            ShowPage(requestedPage);
                        }

                        ProgressPercent = 0;
                        IsBusy = false;
                        Raise(nameof(CanPrev));
                        Raise(nameof(CanNext));
                        CommandManager.InvalidateRequerySuggested();
                    });
                }
                catch (OperationCanceledException)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressPercent = 0;
                        IsBusy = false;
                        Raise(nameof(CanPrev));
                        Raise(nameof(CanNext));
                    });
                }
                catch (Exception ex)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ProgressPercent = 0;
                        IsBusy = false;
                        Raise(nameof(CanPrev));
                        Raise(nameof(CanNext));
                        MessageBox.Show(ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }, token);
        }

        private void ShowPage(int newIndex)
        {
            if (_pagesCache.Count == 0)
            {
                PageIndex = 0;
                FilteredPreview = new ObservableCollection<PageItem>();
                HasNextPage = false;
                return;
            }

            if (newIndex < 0) newIndex = 0;
            if (newIndex >= _pagesCache.Count) newIndex = _pagesCache.Count - 1;

            if (PageIndex != newIndex)
                PageIndex = newIndex;

            var bucket = _pagesCache[PageIndex];
            FilteredPreview = new ObservableCollection<PageItem>(bucket);
            HasNextPage = (PageIndex + 1) < _pagesCache.Count;
            Raise(nameof(CanPrev));
            Raise(nameof(CanNext));
            CommandManager.InvalidateRequerySuggested();
        }

        private void UpdateProgress(long visited)
        {
            var pct = (int)Math.Max(0, Math.Min(100, (visited * 100.0) / TotalCombinations));
            Application.Current.Dispatcher.BeginInvoke(new Action(() => ProgressPercent = pct));
        }

        private void GoToPage()
        {
            if (!int.TryParse(PageInput, out var page) || page <= 0)
                page = 1;

            ShowPage(page - 1);
        }

        private void ExportFilteredCsv()
        {
            try
            {
                var dialog = new SaveFileDialog
                {
                    Title = "Export Filtered CSV",
                    Filter = "CSV file (*.csv)|*.csv|Text file (*.txt)|*.txt|All files (*.*)|*.*",
                    FileName = "filtered_combinations.csv"
                };

                if (dialog.ShowDialog() == true)
                {
                    CancelWork();

                    var activeConditions = CaptureActiveConditionSnapshots();
                    var removedSnapshot = CaptureRemovedSnapshot();

                    _cts = new System.Threading.CancellationTokenSource();
                    var token = _cts.Token;
                    IsBusy = true;
                    ProgressPercent = 0;

                    Task.Run(() =>
                    {
                        try
                        {
                            using (var writer = new StreamWriter(dialog.FileName, false, new UTF8Encoding(false)))
                            {
                                long visited = 0;
                                foreach (var combo in CombinationGenerator.All6of45())
                                {
                                    token.ThrowIfCancellationRequested();
                                    visited++;

                                    if (!ShouldExclude(combo, activeConditions, removedSnapshot))
                                        writer.WriteLine(string.Join(",", combo));

                                    if ((visited & 65535L) == 0)
                                    {
                                        var pct = (int)Math.Max(0, Math.Min(100, (visited * 100.0) / TotalCombinations));
                                        Application.Current.Dispatcher.BeginInvoke(new Action(() => ProgressPercent = pct));
                                    }
                                }
                            }

                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ProgressPercent = 0;
                                IsBusy = false;
                            });
                        }
                        catch (OperationCanceledException)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ProgressPercent = 0;
                                IsBusy = false;
                            });
                        }
                        catch (Exception ex)
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ProgressPercent = 0;
                                IsBusy = false;
                                MessageBox.Show(ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                        }
                    }, token);
                }
            }
            catch (OperationCanceledException)
            {
                MessageBox.Show("Export cancelled", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteSelectedFromPage()
        {
            var removedSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in FilteredPreview.Where(i => i.IsMarked).ToList())
            {
                _removed.Add(item.Text);
                removedSet.Add(item.Text);
            }

            if (_pagesCache.Count > 0)
            {
                var remaining = new List<PageItem>(TotalMatches);
                for (int i = 0; i < _pagesCache.Count; i++)
                {
                    foreach (var entry in _pagesCache[i])
                    {
                        if (!removedSet.Contains(entry.Text))
                            remaining.Add(entry);
                    }
                }

                _pagesCache.Clear();
                for (int i = 0; i < remaining.Count; i++)
                {
                    var pidx = i / PageSize;
                    if (_pagesCache.Count <= pidx)
                        _pagesCache.Add(new List<PageItem>(PageSize));

                    _pagesCache[pidx].Add(remaining[i]);
                }

                TotalMatches = remaining.Count;
                if (PageIndex >= _pagesCache.Count)
                    PageIndex = Math.Max(0, _pagesCache.Count - 1);

                ShowPage(PageIndex);
            }
        }

        private static string Signature(int[] combo) => string.Join(",", combo);

        private void CancelWork()
        {
            var source = _cts;
            if (source != null)
            {
                try { source.Cancel(); }
                catch { }
            }

            _cts = null;

            if (Application.Current?.Dispatcher?.CheckAccess() == false)
                Application.Current.Dispatcher.Invoke(ResetWorkState);
            else
                ResetWorkState();
        }

        private void ResetWorkState()
        {
            ProgressPercent = 0;
            if (IsBusy)
            {
                IsBusy = false;
                Raise(nameof(CanPrev));
                Raise(nameof(CanNext));
            }
        }
    }

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
