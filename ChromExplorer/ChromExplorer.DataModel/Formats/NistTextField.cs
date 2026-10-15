namespace ChromExplorer.DataModel.Formats
{
    //TODO: Do we actually need this?

    public enum NistPropertyDataType
    {
    }


    public class NistTextField
    {
        public string Name { get; set; }
        public string PropertyName { get; set; }
        public bool IsRequired { get; set; }
        public int Length { get; set; }
    }
}