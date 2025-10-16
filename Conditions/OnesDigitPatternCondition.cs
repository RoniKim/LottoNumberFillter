using System;
using System.Collections.Generic;

namespace LottoNumber.Conditions
{
    public sealed class OnesDigitPatternCondition : ICondition
    {
        public string Key => "OnesDigitPattern";
        public string DisplayName => "일의 자리 분포 제한";
        public string Description => "같은 일의 자리 숫자가 3개 이상이거나, 2개짜리 일의 자리 그룹이 3개 이상이면 제외합니다.";
        public IReadOnlyList<ParameterSpec> ParameterSpecs => Array.Empty<ParameterSpec>();

        public bool Evaluate(int[] combo, IDictionary<string, string> parameters = null)
        {
            if (combo == null || combo.Length != 6)
                throw new ArgumentException("combo must be a 6-number array");

            var digitCounts = new int[10];
            for (int i = 0; i < combo.Length; i++)
            {
                var value = combo[i];
                if (value < 1 || value > 45)
                    throw new ArgumentException("combo must contain values between 1 and 45");

                var digit = value % 10;
                digitCounts[digit]++;
                if (digitCounts[digit] >= 3)
                    return true;
            }

            var pairGroups = 0;
            for (int i = 0; i < digitCounts.Length; i++)
            {
                if (digitCounts[i] == 2)
                    pairGroups++;
                if (pairGroups >= 3)
                    return true;
            }

            return false;
        }
    }
}
