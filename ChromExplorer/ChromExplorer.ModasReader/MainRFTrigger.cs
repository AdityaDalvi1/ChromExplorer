using System.Xml.XPath;

namespace ChromExplorer.ModasReader
{
    public class MainRFTrigger
    {
        public MainRFTrigger(XPathNavigator action)
        {
            var nav = action.CreateNavigator();

            var expr = nav.Compile("Delay");
            var iterator = nav.Select(expr);

            if (iterator.MoveNext())
            {
                if (iterator.Current != null)
                    Delay = iterator.Current.ValueAsDouble;
            }

            expr = nav.Compile("StepsCount");
            iterator = nav.Select(expr);

            if (iterator.MoveNext())
            {
                if (iterator.Current != null)
                    Steps = (uint)iterator.Current.ValueAsInt;
            }

            expr = nav.Compile("StepSize");
            iterator = nav.Select(expr);

            if (iterator.MoveNext())
            {
                if (iterator.Current != null)
                    StepSize = iterator.Current.ValueAsInt;
            }

            expr = nav.Compile("StartLevel");
            iterator = nav.Select(expr);

            if (iterator.MoveNext())
            {
                if (iterator.Current != null)
                    StartLevel = (uint)iterator.Current.ValueAsInt;
            }

        }

        public double Delay { get; private set; }
        public uint Steps { get; private set; }
        public int StepSize { get; private set; }
        public uint StartLevel { get; private set; }
    }
}
