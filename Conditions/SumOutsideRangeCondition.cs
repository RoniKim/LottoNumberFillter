using System;
using System.Collections.Generic;

namespace LottoNumber.Conditions
{
    // 6개 숫자의 합이 지정한 범위를 벗어나면 제외하는 조건입니다.
    public sealed class SumOutsideRangeCondition : ICondition
    {
        public string Key => "SumOutsideRange";
        public string DisplayName => "합계가 범위를 벗어남 (MIN, MAX)";
        public string Description => "6개 숫자의 합이 [MIN, MAX] 범위를 벗어나면 제외합니다 (기본: MIN=90, MAX=210)";
        public IReadOnlyList<ParameterSpec> ParameterSpecs => new[]
        {
            new ParameterSpec("MIN", "최솟값", "90"),
            new ParameterSpec("MAX", "최댓값", "210")
        };

        public bool Evaluate(int[] combo, IDictionary<string, string> parameters = null)
        {
            if (combo == null || combo.Length != 6)
                throw new ArgumentException("combo must be a 6-number array");

            int min = 90, max = 210; // 입력이 없으면 기본 합계 범위를 사용합니다.
            if (parameters != null)
            {
                if (parameters.TryGetValue("MIN", out var minText)) int.TryParse(minText, out min);
                if (parameters.TryGetValue("MAX", out var maxText)) int.TryParse(maxText, out max);
            }

            int sum = 0;
            for (int i = 0; i < combo.Length; i++) sum += combo[i];
            return sum < min || sum > max; // 범위를 벗어나면 제외합니다.
        }
    }
}
