using System;
using System.Collections.Generic;

namespace LottoNumber.Conditions
{
    /// <summary>
    /// 연속되는 숫자에 대한 제약(허용 페어 수, 연속 길이)을 한 번에 검사합니다.
    /// </summary>
    public sealed class ConsecutiveNumberConstraintCondition : ICondition
    {
        public string Key => "ConsecutiveNumberConstraint";
        public string DisplayName => "연속 수 제약";
        public string Description => "연속 번호 페어 수와 연속 길이 한계를 초과하면 제외합니다.";
        public IReadOnlyList<ParameterSpec> ParameterSpecs => new[]
        {
            new ParameterSpec("MaxPairs", "허용 연속 페어 수", "1"),
            new ParameterSpec("MinRunLength", "제한 연속 길이", "3")
        };

        public bool Evaluate(int[] combo, IDictionary<string, string> parameters = null)
        {
            if (combo == null || combo.Length != 6)
                throw new ArgumentException("combo must be a 6-number array");

            int maxPairs = Parse(parameters, "MaxPairs", 1, -1, 5);
            int minRunLength = Parse(parameters, "MinRunLength", 3, 0, 6);

            var consecutivePairs = 0;
            var currentRun = 1;
            var longestRun = 1;

            for (int i = 1; i < combo.Length; i++)
            {
                if (combo[i] == combo[i - 1] + 1)
                {
                    consecutivePairs++;
                    currentRun++;
                    if (currentRun > longestRun) longestRun = currentRun;
                }
                else
                {
                    currentRun = 1;
                }
            }

            if (maxPairs >= 0 && consecutivePairs > maxPairs)
                return true;

            if (minRunLength >= 2 && longestRun >= minRunLength)
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
