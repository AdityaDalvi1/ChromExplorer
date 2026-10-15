using System.Collections.Generic;
using System.Linq;
using System.Xml.XPath;

namespace ChromExplorer.ModasReader
{
    public class MethodAction
    {
        public MethodAction(XPathNavigator action)
        {
            var nav = action.CreateNavigator();

            var expr = nav.Compile("Duration");
            var iterator = nav.Select(expr);

            if (iterator.MoveNext())
            {
                if (iterator.Current != null)
                    Duration = iterator.Current.ValueAsDouble;
            }

            expr = nav.Compile("GateOn");
            iterator = nav.Select(expr);

            if (iterator.MoveNext())
            {
                if (iterator.Current != null)
                {
                    var gateStr = iterator.Current.Value;
                    IsDetection = gateStr.Contains("DigitizerOn");
                }
            }
            
            expr = nav.Compile("MainRFTriggers/MainRFTrigger");
            iterator = nav.Select(expr);

            Triggers = new List<MainRFTrigger>();

            while (iterator.MoveNext())
            {
                if (iterator.Current != null)
                {
                    Triggers.Add(new MainRFTrigger(iterator.Current));
                }
            }
        }

        public bool IsDetection { get; private set; }
        public double Duration { get; private set; }
        public IList<MainRFTrigger> Triggers { get; private set; }

        public MainRFTrigger GetTrigger(double start, double end)
        {
            return Triggers.FirstOrDefault(t => t.Delay >= start && t.Delay < end);
        }
    }
}
