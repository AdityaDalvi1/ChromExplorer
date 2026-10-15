using System.Windows.Media;

namespace ChromExplorer.Infrastructure.Interfaces
{
    public interface IHeaderInfoProvider
    {
        string HeaderInfo { get; }
        ImageSource HeaderIcon { get; }
    }
}
