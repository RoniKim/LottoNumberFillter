using System;
using System.Collections.Generic;

namespace LottoNumber.Conditions
{
    public sealed class DecadeClusterLimitCondition : ICondition
    {
        public string Key => "DecadeClusterLimit";
        public string DisplayName => "동일 10단위 초과 제한";
        public string Description => "같은 10단위 구간에서 3개 이상 나온 조합을 제외합니다.";
        public IReadOnlyList<ParameterSpec> ParameterSpecs => Array.Empty<ParameterSpec>();

        public bool Evaluate(int[] combo, IDictionary<string, string> parameters = null)
        {
            if (combo == null || combo.Length != 6)
                throw new ArgumentException("combo must be a 6-number array");

            var buckets = new int[5];
            for (int i = 0; i < combo.Length; i++)
            {
                var value = combo[i];
                if (value < 1 || value > 45)
                    throw new ArgumentException("combo must contain values between 1 and 45");

                var index = Math.Min(4, value / 10);
                buckets[index]++;
                if (buckets[index] >= 3)
                    return true;
            }

            return false;
        }
    }
}
