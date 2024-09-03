namespace VSIXTest
{
    public class OptionWithParameter
    {
        public string Option { get; set; }
        public string Parameter { get; set; }
        public bool ShowParameter { get; set; }

        public OptionWithParameter(string option, string parameter, bool showParameter)
        {
            Option = option;
            Parameter = parameter;
            ShowParameter = showParameter;
        }
    }
}