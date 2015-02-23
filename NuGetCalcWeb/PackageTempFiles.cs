using System;
using System.Collections.Generic;
using System.IO;
using NuGet.Versioning;

namespace NuGetCalcWeb
{
    public static class PackageTempFiles
    {
        static PackageTempFiles()
        {
            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                foreach (var item in items)
                    item.Delete();
            };
        }

        private class Item
        {
            private static TimeSpan expire = TimeSpan.FromHours(1);

            public Item(string name, NuGetVersion version, string tempFileName)
            {
                this.Name = name;
                this.Version = version;
                this.TempFileName = tempFileName;
                this.ExpiresAt = DateTimeOffset.Now + expire;
            }

            public string Name { get; private set; }
            public NuGetVersion Version { get; private set; }
            public string TempFileName { get; private set; }
            public DateTimeOffset ExpiresAt { get; private set; }

            public void Update()
            {
                this.ExpiresAt = DateTimeOffset.Now + expire;
            }

            public void Delete()
            {
                try
                {
                    File.Delete(this.TempFileName);
                }
                catch { }
            }
        }

        private static List<Item> items = new List<Item>();

        private static void Remove(Item item)
        {
            lock (items)
                items.Remove(item);
        }

        public static string Get(string name, NuGetVersion version)
        {
            Item[] x;
            lock (items)
                x = items.ToArray();

            foreach (var item in x)
            {
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && item.Version == version)
                {
                    if (File.Exists(item.TempFileName))
                    {
                        return item.TempFileName;
                    }
                    else
                    {
                        Remove(item);
                        return null;
                    }
                }

                if (item.ExpiresAt > DateTimeOffset.Now)
                {
                    item.Delete();
                    Remove(item);
                }
            }

            return null;
        }

        public static void Add(string name, NuGetVersion version, string tempFileName)
        {
            lock (items)
                items.Add(new Item(name, version, tempFileName));
        }
    }
}
