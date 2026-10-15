using System.Collections.Generic;

namespace ChromExplorer.DataModel.Chromatogram
{
    // One distinct compound (Parent Ion) available in a file, together with the scan mass range
    // its acquisition method covers (e.g. ParentIon=422, MFrom=255, MTo=470) and every MethodId
    // that belongs to it — needed to build a reload filter if this compound wasn't already loaded.
    public class CompoundInfo
    {
        public double ParentIon { get; set; }
        public double MFrom { get; set; }
        public double MTo { get; set; }
        public List<int> MethodIds { get; } = new List<int>();
    }
}
