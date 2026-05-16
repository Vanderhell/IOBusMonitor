using System;
using System.Reflection;

namespace IOBusMonitor
{
    internal static class AppVersionProvider
    {
        public static string GetDisplayVersion()
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

            if (informationalVersion != null && !string.IsNullOrWhiteSpace(informationalVersion.InformationalVersion))
                return informationalVersion.InformationalVersion;

            Version version = assembly.GetName().Version;
            return version != null
                ? string.Format("{0}.{1}.{2}", version.Major, version.Minor, version.Build)
                : "0.0.0";
        }
    }
}
