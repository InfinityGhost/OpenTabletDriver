namespace OpenTabletDriver.Environ.Drivers
{
    public class OpenTabletDriver : IDriverInfoProvider
    {
        public DriverInfo GetDriverInfo()
        {
            if (!Instance.IsOwner && Instance.Exists("OpenTabletDriver.Daemon"))
            {
                return new DriverInfo
                {
                    Name = "OpenTabletDriver",
                    IsSendingInput = true
                };
            }

            return null;
        }
    }
}