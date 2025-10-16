using System;
using System.Collections.Generic;
using System.Linq;

namespace LottoNumber.Conditions
{
    // 지정한 숫자 중 하나라도 포함되어 있으면 제외하는 조건입니다.
    public sealed class MustExcludeNumbersCondition : ICondition
    {
        public string Key => "MustExclude";
        public string DisplayName => "특정 숫자 포함 시 제외";
        public string Description => "지정한 숫자 중 하나라도 포함되어 있으면 제외합니다 (예: 1,7)";
        public IReadOnlyList<ParameterSpec> ParameterSpecs => new[]
        {
            new ParameterSpec("Numbers", "숫자 목록 (쉼표로 구분)", "1,7")
        };

        public bool Evaluate(int[] combo, IDictionary<string, string> parameters = null)
        {
            if (combo == null || combo.Length != 6)
                throw new ArgumentException("combo must be a 6-number array");

            var forbidden = ParseNumbers(parameters, "Numbers");
            if (forbidden.Count == 0) return false; // 제외할 숫자가 없으면 그대로 유지합니다.

            var set = new HashSet<int>(combo);
            foreach (var n in forbidden)
            {
                if (set.Contains(n))
                    return true; // 제외 목록에 있는 숫자가 포함되면 제외합니다.
            }
            return false;
        }

        private static List<int> ParseNumbers(IDictionary<string, string> parameters, string key)
        {
            var result = new List<int>();
            if (parameters == null || !parameters.TryGetValue(key, out var text) || string.IsNullOrWhiteSpace(text))
                return result;
            var parts = text.Split(new[] { ',', ' ', ';', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (int.TryParse(p, out var n) && n >= 1 && n <= 45)
                    if (!result.Contains(n)) result.Add(n);
            }
            return result;
        }
    }
}
