LottoNumber 6/45 Combination Filter (Lazy)

Overview
- Generates all Lotto 6-of-45 combinations lazily (C(45,6) = 8,145,060) using IEnumerable/yield.
- Beginner-friendly condition plugin system: drop a class in Conditions/ and it auto-appears.
- UI shows a preview (first N matches) and supports full CSV export of filtered results.

Condition Interface
public interface ICondition {
  string Key { get; }
  string DisplayName { get; }
  string Description { get; }
  bool Evaluate(int[] combo, IDictionary<string,string> parameters = null);
}
- Evaluate returns true if the combo MEETS the condition (i.e., it will be excluded).
- Parameters are provided as simple key-value strings.

Bundled Conditions
- OddEvenRange: excludes combinations whose odd-count lies outside the specified range (default 2~4) – replaces the old OddCountAtLeast / OddEvenBalance pair.
- ConsecutiveNumberConstraint: enforces both the maximum number of consecutive pairs (default ≤1) and the minimum run length to block (default ≥3).
- HistoricalFirstPrizeExclusion: skips any combination that ever won 1st prize (data pulled from dhlottery API).
- DecadeClusterLimit, OnesDigitPattern, SumOutsideRange, MustIncludeNumbers, MustExcludeNumbers, LowHighImbalance.

Add a New Condition (3 steps)
1) Create a class in Conditions/ implementing ICondition.
   - Example skeleton:
     public sealed class SumLessThanCondition : ICondition {
       public string Key => "SumLessThan";
       public string DisplayName => "Sum Less Than (T)";
       public string Description => "합계가 T보다 작으면 제외";
       public IReadOnlyList<ParameterSpec> ParameterSpecs => new[] {
         new ParameterSpec("T", "합계 임계값", "100")
       };
       public bool Evaluate(int[] combo, IDictionary<string,string> parameters = null) {
         if (combo == null || combo.Length != 6) throw new ArgumentException();
         var threshold = 100;
         if (parameters != null && parameters.TryGetValue("T", out var raw)) int.TryParse(raw, out threshold);
         var sum = 0; for (int i = 0; i < combo.Length; i++) sum += combo[i];
         return sum < threshold; // 조건을 만족하면 제외합니다.
       }
     }

2) Build the solution. The app auto-discovers conditions via reflection and lists them in the dropdown.

3) (Optional) If your condition needs parameters, enter them via the generated fields.

Usage
- Pick conditions and set parameters (예: OddEvenRange MinOdd=2, MaxOdd=4).
- Click "필터 적용" to preview matches.
- Click "CSV 내보내기" to stream all filtered results to a file (no massive memory usage).

Implementation Notes
- Generator: ViewModels/CombinationGenerator.All6of45() uses nested loops with yield return to lazily produce ascending 6-number combos in 1..45.
- Filtering: the ViewModel iterates the generator and excludes combos where any enabled condition's Evaluate(...) returns true.
- UI Preview: limited by a page size to keep the UI responsive.
