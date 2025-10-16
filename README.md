# LottoNumber 6/45 조합 필터 (Lazy)

## 개요

IEnumerable / yield를 사용하여 모든 로또 6/45 조합(C(45,6) =
8,145,060)을 지연 생성합니다.\
초보자도 쉽게 확장 가능한 조건 플러그인 시스템을 제공하며, Conditions/
폴더에 클래스를 추가하면 자동으로 UI에 표시됩니다.\
UI에서는 일부 결과(최초 N개)를 미리보기로 보여주며, 전체 필터링 결과를
CSV 파일로 내보낼 수 있습니다.

------------------------------------------------------------------------

## 조건 인터페이스

``` csharp
public interface ICondition
{
    string Key { get; }
    string DisplayName { get; }
    string Description { get; }
    bool Evaluate(int[] combo, IDictionary<string,string> parameters = null);
}
```

-   `Evaluate` 메서드는 조합이 **조건을 만족할 경우 true**를 반환합니다.
    (즉, 해당 조합은 제외됨)
-   `parameters`는 단순한 key-value 문자열로 전달됩니다.

------------------------------------------------------------------------

## 기본 제공 조건 (Bundled Conditions)

-   **OddEvenRange**: 홀수 개수가 지정된 범위를 벗어난 조합을
    제외합니다. (기본값 2\~4)\
    기존의 `OddCountAtLeast` / `OddEvenBalance` 조건을 대체합니다.

-   **ConsecutiveNumberConstraint**: 연속된 숫자 쌍의 최대 개수(기본
    ≤1)와 차단할 최소 연속 길이(기본 ≥3)를 동시에 제어합니다.

-   **HistoricalFirstPrizeExclusion**: 과거 1등 당첨 이력이 있는 조합을
    제외합니다. (데이터는 dhlottery API에서 가져옴)

-   그 외 조건: `DecadeClusterLimit`, `OnesDigitPattern`,
    `SumOutsideRange`, `MustIncludeNumbers`, `MustExcludeNumbers`,
    `LowHighImbalance`

------------------------------------------------------------------------

## 새 조건 추가 (3단계)

1.  `Conditions/` 폴더에 `ICondition`을 구현하는 클래스를 생성합니다.

예시:

``` csharp
public sealed class SumLessThanCondition : ICondition
{
    public string Key => "SumLessThan";
    public string DisplayName => "Sum Less Than (T)";
    public string Description => "합계가 T보다 작으면 제외";

    public IReadOnlyList<ParameterSpec> ParameterSpecs => new[]
    {
        new ParameterSpec("T", "합계 임계값", "100")
    };

    public bool Evaluate(int[] combo, IDictionary<string,string> parameters = null)
    {
        if (combo == null || combo.Length != 6) throw new ArgumentException();

        var threshold = 100;
        if (parameters != null && parameters.TryGetValue("T", out var raw))
            int.TryParse(raw, out threshold);

        var sum = 0;
        for (int i = 0; i < combo.Length; i++) sum += combo[i];

        return sum < threshold; // 조건을 만족하면 제외합니다.
    }
}
```

2.  솔루션을 빌드하면, 앱이 리플렉션을 통해 자동으로 조건을 탐색하고
    드롭다운에 표시합니다.

3.  (선택사항) 조건에 매개변수가 필요한 경우, UI에서 자동 생성된 필드에
    값을 입력합니다.

------------------------------------------------------------------------

## 사용법

-   조건을 선택하고 매개변수를 설정합니다.\
    예: `OddEvenRange` → `MinOdd=2`, `MaxOdd=4`
-   "필터 적용"을 클릭하면 미리보기가 표시됩니다.
-   "CSV 내보내기"를 클릭하면 필터링된 전체 결과를 파일로 스트리밍
    저장합니다. (대용량 메모리 사용 없음)

------------------------------------------------------------------------

## 구현 세부사항

-   **Generator**: `ViewModels/CombinationGenerator.All6of45()`는 중첩
    루프와 `yield return`을 사용하여 1\~45 범위 내에서 오름차순 6개
    조합을 지연 생성합니다.\
-   **Filtering**: `ViewModel`이 제너레이터를 순회하며, 활성화된 조건 중
    `Evaluate(...)`가 `true`인 경우 해당 조합을 제외합니다.\
-   **UI Preview**: 페이지 크기 단위로 제한되어 있어 UI 응답성이
    유지됩니다.
