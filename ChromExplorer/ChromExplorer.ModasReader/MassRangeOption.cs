using System.Collections.Generic;
using System.Linq;

namespace ChromExplorer.ModasReader
{
    // One distinct scan mass range (Method.MFrom-MTo) found in a file, e.g. "270-620".
    // A single range can be shared by several Method entries/parent masses (e.g. 609 and
    // 609.1 both scanning 270-620), so this carries every MethodId and ParentIonMass that
    // belongs to it.
    public class MassRangeOption
    {
        public double MFrom { get; set; }
        public double MTo { get; set; }
        public List<int> MethodIds { get; } = new List<int>();
        public List<double> ParentMasses { get; } = new List<double>();

        public string ParentMassDisplay
        {
            get { return string.Join(", ", ParentMasses.OrderBy(p => p).Select(p => p.ToString("0.####"))); }
        }

        public override string ToString()
        {
            return string.Format("{0:0.##}-{1:0.##}", MFrom, MTo);
        }
    }
}
