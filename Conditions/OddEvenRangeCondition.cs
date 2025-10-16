using System;
using System.Collections.Generic;

namespace LottoNumber.Conditions
{
    /// <summary>
    /// 홀수 개수가 허용 범위를 벗어나는 조합을 제외합니다.
    /// </summary>
    public sealed class OddEvenRangeCondition : ICondition
    {
        public string Key => "OddEvenRange";
        public string DisplayName => "홀짝 분포 범위";
        public string Description => "홀수 개수가 지정한 범위를 벗어나면 제외합니다.";
        public IReadOnlyList<ParameterSpec> ParameterSpecs => new[]
        {
            new ParameterSpec("MinOdd", "최소 홀수 개수", "2"),
            new ParameterSpec("MaxOdd", "최대 홀수 개수", "4")
        };

        public bool Evaluate(int[] combo, IDictionary<string, string> parameters = null)
        {
            if (combo == null || combo.Length != 6)
                throw new ArgumentException("combo must be a 6-number array");

            int minOdd = Parse(parameters, "MinOdd", 2, 0, 6);
            int maxOdd = Parse(parameters, "MaxOdd", 4, minOdd, 6);

            var oddCount = 0;
            for (int i = 0; i < combo.Length; i++)
                if ((combo[i] & 1) == 1) oddCount++;

            if (oddCount < minOdd)
                return true;

            if (maxOdd >= minOdd && oddCount > maxOdd)
                return true;

            return false;
        }

        private static int Parse(IDictionary<string, string> parameters, string key, int fallback, int min, int max)
        {
            var value = fallback;
            if (parameters != null && parameters.TryGetValue(key, out var raw) && !string.IsNullOrWhiteSpace(raw))
            {
                if (int.TryParse(raw, out var parsed))
                    value = parsed;
            }

            if (value < min) value = min;
            if (value > max) value = max;
            return value;
        }
    }
}
