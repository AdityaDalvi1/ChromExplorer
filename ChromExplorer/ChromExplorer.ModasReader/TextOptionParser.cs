using System;
using System.Reflection;
using log4net;

namespace ChromExplorer.ModasReader
{
    public class TextOptionsParser
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private const string FracBitsKey = "MRF_FracStepBits";

        public TextOptionsParser()
        {
            Init();
        }

        public void Init()
        {
            FracStepBits = 0;
            Parsed = false;
        }

        public bool Parsed { get; set; }
        public int FracStepBits { get; set; }

        public void Parse(string value)
        {
            Init();

            var fb = GetIntOptionValue(FracBitsKey, value, 0);

            if (fb > 6)
                fb = 6;

            FracStepBits = fb;


            Parsed = true;

        }

        private int GetIntOptionValue(string name, string options, int defaultValue = 0)
        {
            try
            {
                var toFind = name.ToUpper();
                var toParse = options.ToUpper();

                if (string.IsNullOrEmpty(toFind) || string.IsNullOrEmpty(toParse))
                    return defaultValue;

                var pos = toParse.IndexOf(toFind, StringComparison.Ordinal);

                if (pos >= 0)
                {
                    var numberStart = toParse.IndexOf('=', pos);
                    var numberEnd = toParse.IndexOf(';', pos);

                    if (numberStart == -1 || numberEnd == -1)
                        return defaultValue;

                    if (numberStart < toParse.Length && numberEnd < toParse.Length)
                    {
                        var numberString = toParse.Substring(numberStart + 1, numberEnd - numberStart - 1);

                        int result;

                        if (int.TryParse(numberString, out result))
                        {
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log.Warn("Error during text options parse", ex);
            }

            return defaultValue;
        }
    }
}
