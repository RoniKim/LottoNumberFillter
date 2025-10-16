using System.Collections.Generic;

namespace LottoNumber.ViewModels
{
    public static class CombinationGenerator
    {
        // 1부터 45까지 번호에서 6개를 오름차순으로 뽑아 모든 조합을 생성합니다.
        public static IEnumerable<int[]> All6of45()
        {
            for (int a = 1; a <= 40; a++)
            for (int b = a + 1; b <= 41; b++)
            for (int c = b + 1; c <= 42; c++)
            for (int d = c + 1; d <= 43; d++)
            for (int e = d + 1; e <= 44; e++)
            for (int f = e + 1; f <= 45; f++)
            {
                yield return new[] { a, b, c, d, e, f };
            }
        }
    }
}
