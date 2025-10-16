using System;
using System.Collections.Generic;
using System.Linq;

namespace LottoNumber.Conditions
{
    // 지정한 숫자들이 모두 포함되어 있는지 확인하고 하나라도 빠지면 제외하는 조건입니다.
    public sealed class MustIncludeNumbersCondition : ICondition
    {
        public string Key => "MustInclude";
        public string DisplayName => "특정 숫자 모두 포함";
        public string Description => "지정한 숫자들이 모두 포함되어 있지 않으면 제외합니다 (예: 1,7)";
        public IReadOnlyList<ParameterSpec> ParameterSpecs => new[]
        {
            new ParameterSpec("Numbers", "숫자 목록 (쉼표로 구분)", "1,7")
        };

        public bool Evaluate(int[] combo, IDictionary<string, string> parameters = null)
        {
            if (combo == null || combo.Length != 6)
                throw new ArgumentException("combo must be a 6-number array");

            // 필수 숫자 목록을 추출합니다.
            var required = ParseNumbers(parameters, "Numbers");
            if (required.Count == 0) return false; // 지정된 숫자가 없으면 아무 것도 제외하지 않습니다.

            var set = new HashSet<int>(combo);
            foreach (var n in required)
            {
                if (!set.Contains(n))
                    return true; // 필수 숫자가 하나라도 빠졌다면 제외합니다.
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
