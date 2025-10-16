using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;

namespace LottoNumber.Services
{
    public sealed class WinningHistoryCache
    {
        private const string CacheFileName = "winning-history.csv";
        private static readonly Lazy<WinningHistoryCache> _instance = new Lazy<WinningHistoryCache>(() => new WinningHistoryCache());

        private readonly object _lock = new object();
        private readonly List<WinningDrawRecord> _records = new List<WinningDrawRecord>();
        private readonly HashSet<long> _keys = new HashSet<long>();
        private readonly int[] _numberCounts = new int[46];
        private readonly string _cachePath;
        private bool _initialized;
        private int _maxDrawNo;
        private int _totalDraws;

        private WinningHistoryCache()
        {
            var baseDirectory = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                baseDirectory = AppDomain.CurrentDomain.BaseDirectory ?? string.Empty;
            }

            var folder = Path.Combine(baseDirectory, "LottoNumber");
            Directory.CreateDirectory(folder);
            _cachePath = Path.Combine(folder, CacheFileName);
        }

        public static WinningHistoryCache Instance => _instance.Value;

        public void EnsureInitialized()
        {
            if (_initialized)
                return;

            lock (_lock)
            {
                if (_initialized)
                    return;

                LoadCacheFromDisk();
                var added = FetchLatestRecords();
                if (added || !File.Exists(_cachePath))
                {
                    SaveCacheToDisk();
                }

                _initialized = true;
            }
        }

        public bool ContainsCombination(int[] combo)
        {
            if (combo == null || combo.Length != 6)
                throw new ArgumentException("combo must be a 6-number array", nameof(combo));

            EnsureInitialized();

            var key = Encode(combo);
            lock (_lock)
            {
                return _keys.Contains(key);
            }
        }

        public IReadOnlyList<WinningNumberStatistic> GetNumberStatistics()
        {
            EnsureInitialized();

            lock (_lock)
            {
                if (_totalDraws <= 0)
                    return Array.Empty<WinningNumberStatistic>();

                var stats = new WinningNumberStatistic[45];
                for (int number = 1; number <= 45; number++)
                {
                    var count = _numberCounts[number];
                    var rate = _totalDraws == 0 ? 0d : (double)count / _totalDraws;
                    stats[number - 1] = new WinningNumberStatistic(number, count, rate);
                }
                return stats;
            }
        }

        private void LoadCacheFromDisk()
        {
            if (!File.Exists(_cachePath))
                return;

            foreach (var line in File.ReadAllLines(_cachePath))
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                var parts = line.Split(',');
                if (parts.Length != 7)
                    continue;

                if (!int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var drawNo))
                    continue;

                var numbers = new int[6];
                var valid = true;
                for (int i = 0; i < 6; i++)
                {
                    if (!int.TryParse(parts[i + 1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var value))
                    {
                        valid = false;
                        break;
                    }
                    numbers[i] = value;
                }

                if (!valid)
                    continue;

                Array.Sort(numbers);
                if (!IsValidCombination(numbers))
                    continue;

                var key = Encode(numbers);
                if (_keys.Add(key))
                {
                    _records.Add(new WinningDrawRecord(drawNo, numbers));
                    if (drawNo > _maxDrawNo)
                        _maxDrawNo = drawNo;
                    _totalDraws++;
                    AccumulateNumbers(numbers);
                }
            }

            if (_records.Count > 0)
            {
                _records.Sort((a, b) => a.DrawNumber.CompareTo(b.DrawNumber));
            }
        }

        private bool FetchLatestRecords()
        {
            var added = false;
            var nextDraw = _maxDrawNo <= 0 ? 1 : _maxDrawNo + 1;

            using (var client = new HttpClient())
            {
                client.Timeout = TimeSpan.FromSeconds(10);

                while (true)
                {
                    WinningDrawRecord record;
                    try
                    {
                        record = FetchDraw(client, nextDraw);
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"회차 {nextDraw} 당첨 데이터를 가져오지 못했습니다.", ex);
                    }

                    if (record == null)
                        break;

                    Array.Sort(record.Numbers);
                    if (!IsValidCombination(record.Numbers))
                    {
                        nextDraw++;
                        continue;
                    }

                    var key = Encode(record.Numbers);
                    if (_keys.Add(key))
                    {
                        _records.Add(record);
                        if (record.DrawNumber > _maxDrawNo)
                            _maxDrawNo = record.DrawNumber;
                        _totalDraws++;
                        AccumulateNumbers(record.Numbers);
                        added = true;
                    }

                    nextDraw++;
                }
            }

            if (added)
            {
                _records.Sort((a, b) => a.DrawNumber.CompareTo(b.DrawNumber));
            }

            return added;
        }

        private static WinningDrawRecord FetchDraw(HttpClient client, int drawNo)
        {
            var url = string.Format(CultureInfo.InvariantCulture, "https://www.dhlottery.co.kr/common.do?method=getLottoNumber&drwNo={0}", drawNo);
            var response = client.GetAsync(url).GetAwaiter().GetResult();
            response.EnsureSuccessStatusCode();
            var json = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

            if (string.IsNullOrWhiteSpace(json))
                return null;
            if (json.IndexOf("\"returnValue\":\"success\"", StringComparison.OrdinalIgnoreCase) < 0)
                return null;

            var numbers = new int[6];
            for (int i = 1; i <= 6; i++)
            {
                var value = ExtractInt(json, $"\"drwtNo{i}\":");
                if (value < 1 || value > 45)
                    throw new InvalidOperationException("Invalid winning number received from API.");
                numbers[i - 1] = value;
            }

            var parsedDraw = ExtractInt(json, "\"drwNo\":");
            if (parsedDraw <= 0)
                parsedDraw = drawNo;

            return new WinningDrawRecord(parsedDraw, numbers);
        }

        private void SaveCacheToDisk()
        {
            var lines = _records
                .OrderBy(r => r.DrawNumber)
                .Select(r => string.Join(",", new[] { r.DrawNumber.ToString(CultureInfo.InvariantCulture) }.Concat(r.Numbers.Select(n => n.ToString(CultureInfo.InvariantCulture)))))
                .ToArray();

            File.WriteAllLines(_cachePath, lines);
        }

        private static int ExtractInt(string json, string token)
        {
            var index = json.IndexOf(token, StringComparison.Ordinal);
            if (index < 0)
                return -1;

            index += token.Length;
            while (index < json.Length && (json[index] == ' ' || json[index] == ':'))
                index++;

            var start = index;
            while (index < json.Length && char.IsDigit(json[index]))
                index++;

            if (start == index)
                return -1;

            var text = json.Substring(start, index - start);
            return int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : -1;
        }

        private static bool IsValidCombination(int[] numbers)
        {
            if (numbers == null || numbers.Length != 6)
                return false;

            var previous = 0;
            for (int i = 0; i < numbers.Length; i++)
            {
                var value = numbers[i];
                if (value < 1 || value > 45)
                    return false;
                if (i > 0 && value <= previous)
                    return false;
                previous = value;
            }

            return true;
        }

        private static long Encode(int[] numbers)
        {
            long key = 0;
            for (int i = 0; i < numbers.Length; i++)
            {
                key |= ((long)numbers[i] & 0x3F) << (i * 6);
            }
            return key;
        }

        private void AccumulateNumbers(int[] numbers)
        {
            for (int i = 0; i < numbers.Length; i++)
            {
                var value = numbers[i];
                if (value >= 1 && value <= 45)
                    _numberCounts[value]++;
            }
        }

        private sealed class WinningDrawRecord
        {
            public WinningDrawRecord(int drawNumber, int[] numbers)
            {
                DrawNumber = drawNumber;
                Numbers = (int[])numbers.Clone();
            }

            public int DrawNumber { get; }
            public int[] Numbers { get; }
        }
    }

    public sealed class WinningNumberStatistic
    {
        public WinningNumberStatistic(int number, int count, double rate)
        {
            Number = number;
            Count = count;
            Rate = rate;
        }

        public int Number { get; }
        public int Count { get; }
        public double Rate { get; }
    }
}
