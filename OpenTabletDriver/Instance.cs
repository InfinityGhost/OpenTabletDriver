using System;
using System.Threading;

namespace OpenTabletDriver
{
    public class Instance : IDisposable
    {
        public Instance(string name)
        {
            this.mutex = new Mutex(true, $"{prefix}{name}", out bool createdNew);
            AlreadyExists = !createdNew;
            if (!isOwnerSet)
            {
                IsOwner = createdNew;
                isOwnerSet = true;
            }
        }

        public static bool Exists(string name)
        {
            var result = Mutex.TryOpenExisting($"{prefix}{name}", out var mutex);
            using (mutex)
            {
                mutex?.Close();
                return result;
            }
        }

        private const string prefix = @"Global\";
        private Mutex mutex;
        private static bool isOwnerSet;
        public static bool IsOwner { protected set; get; }
        public bool AlreadyExists { protected set; get; }

        public void Dispose()
        {
            mutex.Close();
            mutex.Dispose();
        }
    }
}