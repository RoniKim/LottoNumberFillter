using System;
using System.Collections.Generic;

namespace LottoNumber.Conditions
{
    // 낮은 구간(1~22) 또는 높은 구간(23~45)의 숫자가 K개 이상이면 제외하는 조건입니다.
    public sealed class LowHighImbalanceAtLeastCondition : ICondition
    {
        public string Key => "LowHighImbalanceAtLeast";
        public string DisplayName => "저/고 구간 불균형 (K 이상)";
        public string Description => "낮은 구간(1~22) 또는 높은 구간(23~45)의 숫자가 K개 이상이면 제외합니다 (기본: K=5)";
        public IReadOnlyList<ParameterSpec> ParameterSpecs => new[]
        {
            new ParameterSpec("K", "불균형 K", "5")
        };

        public bool Evaluate(int[] combo, IDictionary<string, string> parameters = null)
        {
            if (combo == null || combo.Length != 6)
                throw new ArgumentException("combo must be a 6-number array");

            int k = 0;
            if (parameters != null && parameters.TryGetValue("K", out var text))
                int.TryParse(text, out k);
            if (k <= 0) k = 5; // 입력이 없으면 기본값 5로 적용합니다.

            int low = 0, high = 0;
            for (int i = 0; i < combo.Length; i++)
            {
                if (combo[i] <= 22) low++; else high++;
            }
            return low >= k || high >= k; // 어느 한쪽 구간이라도 기준을 넘으면 제외합니다.
        }
    }
}
