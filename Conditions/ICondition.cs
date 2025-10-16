using System;
using System.Collections.Generic;
using System.Linq;

namespace LottoNumber.Conditions
{
    // 조건을 정의할 때 구현해야 하는 최소한의 계약입니다.
    public interface ICondition
    {
        string Key { get; }
        string DisplayName { get; }
        string Description { get; }
        IReadOnlyList<ParameterSpec> ParameterSpecs { get; }
        bool Evaluate(int[] combo, IDictionary<string, string> parameters = null);
    }

    // 어셈블리 내의 모든 ICondition 구현 클래스를 찾아 인스턴스로 반환합니다.
    public static class ConditionDiscovery
    {
        public static IEnumerable<ICondition> DiscoverAll()
        {
            var type = typeof(ICondition);
            var asm = type.Assembly;
            foreach (var t in asm.GetTypes().Where(t => !t.IsAbstract && type.IsAssignableFrom(t)))
            {
                ICondition instance = null;
                try { instance = (ICondition)Activator.CreateInstance(t); }
                catch { }
                if (instance != null) yield return instance;
            }
        }
    }
}
