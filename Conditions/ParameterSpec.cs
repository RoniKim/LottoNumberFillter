namespace LottoNumber.Conditions
{
    public sealed class ParameterSpec
    {
        public string Name { get; }
        public string Label { get; }
        public string DefaultValue { get; }

        public ParameterSpec(string name, string label, string defaultValue)
        {
            Name = name;
            Label = label;
            DefaultValue = defaultValue;
        }
    }
}

