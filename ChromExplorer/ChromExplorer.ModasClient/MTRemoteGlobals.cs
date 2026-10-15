namespace ChromExplorer.ModasClient
{
    /// <summary>
    /// Global constants 
    /// </summary>
    public class MTRemoteGlobals
    {
        static MTRemoteGlobals()
        {
            CompressData = true;
        }

        public static bool CompressData { get; set; }
    }
}