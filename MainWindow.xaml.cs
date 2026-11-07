using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MoodJournalWpf;

public partial class MainWindow : Window
{
    private readonly CultureInfo _ru = CultureInfo.GetCultureInfo("ru-RU");
    private readonly List<MoodEntry> _entries = new();
    private readonly HashSet<string> _selectedActivities = new();
    private Button[] _moodButtons = Array.Empty<Button>();
    private Button[] _activityButtons = Array.Empty<Button>();
    private int _selectedMood;

    private static readonly string[] MoodNames =
    {
        "", "Плохо", "Грустно", "Обычно", "Хорошо", "Отлично"
    };

    private static readonly string[] MoodEmojis =
    {
        "", "😞", "😕", "😐", "🙂", "🤩"
    };

    public MainWindow()
    {
        InitializeComponent();

        _moodButtons = new[] { Mood1, Mood2, Mood3, Mood4, Mood5 };
        _activityButtons = new[]
        {
            ActivityStudy, ActivityWork, ActivityWalk,
            ActivityFriends, ActivitySport, ActivityRest
        };

        TodayText.Text = DateTime.Today.ToString("d MMMM yyyy", _ru);
        FillDemoEntries();
        RefreshViews();
    }

    private void FillDemoEntries()
    {
        int[] scores = { 3, 4, 2, 5, 3, 4, 4 };
        string[] comments =
        {
            "Обычный спокойный день.",
            "Удалось закончить важное дело.",
            "Немного устала, хочется отдохнуть.",
            "Хороший день, было много приятных моментов.",
            "Много задач и мыслей.",
            "Прогулка помогла переключиться.",
            "Сегодня чувствую себя уверенно."
        };
        string[] activities =
        {
            "Учёба", "Работа • Общение", "Учёба", "Прогулка • Общение",
            "Работа", "Прогулка • Отдых", "Учёба • Отдых"
        };

        for (int i = 0; i < scores.Length; i++)
        {
            _entries.Add(new MoodEntry
            {
                Date = DateTime.Today.AddDays(i - 6).AddHours(12).AddMinutes(20),
                Score = scores[i],
                Comment = comments[i],
                Activities = activities[i]
            });
        }
    }

    private void MoodButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || !int.TryParse(button.Tag?.ToString(), out int value))
        {
            return;
        }

        _selectedMood = value;

        foreach (Button moodButton in _moodButtons)
        {
            bool selected = moodButton == button;
            moodButton.Background = Brush(selected ? "#F0EDFC" : "#FBFBFE");
            moodButton.BorderBrush = Brush(selected ? "#765DDF" : "#EAEBF4");
            moodButton.BorderThickness = new Thickness(selected ? 2 : 1);
        }

        SaveStatusText.Text = $"Выбрано: {MoodNames[value].ToLower(_ru)}";
    }

    private void ActivityButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button || button.Tag is not string activity)
        {
            return;
        }

        if (!_selectedActivities.Add(activity))
        {
            _selectedActivities.Remove(activity);
        }

        bool selected = _selectedActivities.Contains(activity);
        button.Background = Brush(selected ? "#F0EDFC" : "#F6F7FB");
        button.BorderBrush = Brush(selected ? "#AA9BE6" : "#E9EBF3");
        button.Foreground = Brush(selected ? "#654ACB" : "#575C73");
    }

    private void SaveEntry_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedMood == 0)
        {
            SaveStatusText.Text = "Сначала выбери настроение";
            return;
        }

        string activities = string.Join(" • ", _activityButtons
            .Select(button => button.Tag?.ToString() ?? "")
            .Where(_selectedActivities.Contains));

        _entries.Add(new MoodEntry
        {
            Date = DateTime.Now,
            Score = _selectedMood,
            Comment = CommentBox.Text.Trim(),
            Activities = activities
        });

        _selectedMood = 0;
        _selectedActivities.Clear();
        CommentBox.Clear();

        foreach (Button moodButton in _moodButtons)
        {
            moodButton.Background = Brush("#FBFBFE");
            moodButton.BorderBrush = Brush("#EAEBF4");
            moodButton.BorderThickness = new Thickness(1);
        }

        foreach (Button activityButton in _activityButtons)
        {
            activityButton.Background = Brush("#F6F7FB");
            activityButton.BorderBrush = Brush("#E9EBF3");
            activityButton.Foreground = Brush("#575C73");
        }

        SaveStatusText.Text = "Запись добавлена ✓";
        RefreshViews();
    }

    private void DashboardNav_Click(object sender, RoutedEventArgs e) => ShowView("dashboard");

    private void HistoryNav_Click(object sender, RoutedEventArgs e) => ShowView("history");

    private void AnalyticsNav_Click(object sender, RoutedEventArgs e) => ShowView("analytics");

    private void ShowView(string view)
    {
        DashboardView.Visibility = view == "dashboard" ? Visibility.Visible : Visibility.Collapsed;
        HistoryView.Visibility = view == "history" ? Visibility.Visible : Visibility.Collapsed;
        AnalyticsView.Visibility = view == "analytics" ? Visibility.Visible : Visibility.Collapsed;

        PageTitle.Text = view switch
        {
            "history" => "История настроения",
            "analytics" => "Твоя аналитика",
            _ => "Моё настроение"
        };

        PageSubtitle.Text = view switch
        {
            "history" => "Каждый день — часть твоей истории",
            "analytics" => "Замечай изменения и находи закономерности",
            _ => "Побудь немного с собой"
        };

        foreach (Button nav in new[] { DashboardNav, HistoryNav, AnalyticsNav })
        {
            bool active = nav == (view switch
            {
                "history" => HistoryNav,
                "analytics" => AnalyticsNav,
                _ => DashboardNav
            });

            nav.Background = Brush(active ? "#383653" : "#1F2238");
            nav.Foreground = Brush(active ? "#FFFFFF" : "#AEB3C7");
        }

        if (view == "analytics")
        {
            DrawChart(LargeChartCanvas);
        }
    }

    private void RefreshViews()
    {
        List<MoodEntry> ordered = _entries.OrderByDescending(entry => entry.Date).ToList();
        RecentEntriesList.ItemsSource = ordered.Take(3).ToList();
        AllEntriesList.ItemsSource = ordered;

        DateTime weekStart = DateTime.Today.AddDays(-6);
        List<MoodEntry> week = _entries.Where(entry => entry.Date.Date >= weekStart).ToList();

        string average = week.Count == 0
            ? "—"
            : week.Average(entry => entry.Score).ToString("0.0", _ru);

        SummaryAvgText.Text = average;
        SummaryTotalText.Text = week.Count.ToString(_ru);
        AnalyticsAvgText.Text = average;
        AnalyticsCountText.Text = _entries.Count.ToString(_ru);

        var bestDay = week
            .GroupBy(entry => entry.Date.Date)
            .Select(group => new { Date = group.Key, Average = group.Average(entry => entry.Score) })
            .OrderByDescending(group => group.Average)
            .ThenByDescending(group => group.Date)
            .FirstOrDefault();

        SummaryBestDayText.Text = bestDay == null
            ? "—"
            : _ru.TextInfo.ToTitleCase(bestDay.Date.ToString("ddd", _ru));

        string? topActivity = week
            .SelectMany(entry => entry.Activities.Split('•', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            .GroupBy(activity => activity)
            .OrderByDescending(group => group.Count())
            .ThenBy(group => group.Key)
            .Select(group => group.Key)
            .FirstOrDefault();

        AnalyticsTopActivityText.Text = topActivity ?? "Нет данных";
        InsightText.Text = week.Count == 0
            ? "Добавь первую запись, чтобы увидеть, как менялось настроение."
            : $"За неделю сделано {week.Count} отметок. Средний балл — {average} из 5. " +
              "Регулярные записи помогут лучше замечать изменения.";

        DrawChart(WeekChartCanvas);
        DrawChart(LargeChartCanvas);
    }

    private void ChartCanvas_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (sender is Canvas canvas && _entries.Count > 0)
        {
            DrawChart(canvas);
        }
    }

    private void DrawChart(Canvas canvas)
    {
        canvas.Children.Clear();

        double width = canvas.ActualWidth;
        if (width <= 0)
        {
            return;
        }

        double baseline = canvas.Height - 35;
        double usableHeight = baseline - 20;
        double columnSpace = width / 7;
        double barWidth = Math.Min(40, columnSpace * 0.48);

        for (int lineIndex = 1; lineIndex <= 5; lineIndex++)
        {
            double y = baseline - usableHeight * lineIndex / 5.0;
            var line = new Line
            {
                X1 = 2,
                X2 = width - 2,
                Y1 = y,
                Y2 = y,
                Stroke = Brush("#F0F1F6"),
                StrokeThickness = 1
            };
            canvas.Children.Add(line);
        }

        for (int i = 0; i < 7; i++)
        {
            DateTime day = DateTime.Today.AddDays(i - 6);
            var scores = _entries.Where(entry => entry.Date.Date == day.Date)
                .Select(entry => entry.Score).ToList();
            double value = scores.Count == 0 ? 0 : scores.Average();
            double barHeight = value == 0 ? 5 : usableHeight * value / 5.0;
            double left = columnSpace * i + (columnSpace - barWidth) / 2;

            var bar = new Rectangle
            {
                Width = barWidth,
                Height = barHeight,
                RadiusX = 8,
                RadiusY = 8,
                Fill = Brush(value == 0 ? "#E9EAF1" : (i == 6 ? "#7056D8" : "#B8A9F0"))
            };
            Canvas.SetLeft(bar, left);
            Canvas.SetTop(bar, baseline - barHeight);
            canvas.Children.Add(bar);

            var dayLabel = new TextBlock
            {
                Text = day.ToString("ddd", _ru),
                FontSize = 11,
                Foreground = Brush("#9297A9"),
                Width = columnSpace,
                TextAlignment = TextAlignment.Center
            };
            Canvas.SetLeft(dayLabel, columnSpace * i);
            Canvas.SetTop(dayLabel, baseline + 9);
            canvas.Children.Add(dayLabel);
        }
    }

    private static SolidColorBrush Brush(string color) =>
        (SolidColorBrush)new BrushConverter().ConvertFromString(color)!;

    public sealed class MoodEntry
    {
        public required DateTime Date { get; init; }
        public required int Score { get; init; }
        public string Comment { get; init; } = "";
        public string Activities { get; init; } = "";

        public string Emoji => MoodEmojis[Score];
        public string MoodName => MoodNames[Score];
        public string CommentText => string.IsNullOrWhiteSpace(Comment) ? "Без заметки" : Comment;
        public string ActivitiesText => string.IsNullOrWhiteSpace(Activities) ? "Без активностей" : Activities;
        public string DateText => Date.Date == DateTime.Today
            ? $"Сегодня, {Date:HH:mm}"
            : Date.Date == DateTime.Today.AddDays(-1)
                ? $"Вчера, {Date:HH:mm}"
                : Date.ToString("d MMM, HH:mm", CultureInfo.GetCultureInfo("ru-RU"));
    }
}
