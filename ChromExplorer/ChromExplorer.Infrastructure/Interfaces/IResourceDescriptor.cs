using System.Windows.Media;

namespace ChromExplorer.Infrastructure.Interfaces
{
    public interface IResourceDescriptor
    {
        string ObjectKey { get; set; }
        string Title { get; set; }
        string Description { get; set; }
        string Tooltip { get; set; }
        ImageSource Icon { get; set; }
    }
}
