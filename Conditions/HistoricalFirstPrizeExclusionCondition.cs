using System;
using System.Collections.Generic;
using LottoNumber.Services;

namespace LottoNumber.Conditions
{
    public sealed class HistoricalFirstPrizeExclusionCondition : ICondition
    {
        public string Key => "HistoricalFirstPrizeExclude";
        public string DisplayName => "역대 1등 당첨 조합 제외";
        public string Description => "역대 1등 당첨번호 조합과 일치하면 제외합니다.";
        public IReadOnlyList<ParameterSpec> ParameterSpecs => Array.Empty<ParameterSpec>();

        public bool Evaluate(int[] combo, IDictionary<string, string> parameters = null)
        {
            if (combo == null || combo.Length != 6)
                throw new ArgumentException("combo must be a 6-number array");

            return WinningHistoryCache.Instance.ContainsCombination(combo);
        }
    }
}
