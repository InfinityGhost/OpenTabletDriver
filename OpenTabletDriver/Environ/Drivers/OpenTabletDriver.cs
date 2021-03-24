namespace OpenTabletDriver.Environ.Drivers
{
    public class OpenTabletDriver : IDriverInfoProvider
    {
        public DriverInfo GetDriverInfo()
        {
            if (Instance.Exists("OpenTabletDriver.Daemon") && !Instance.IsOwner)
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