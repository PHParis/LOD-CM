using System;
using System.IO;

namespace PlantUml.Net.Java
{
    internal class EnvironmentJavaLocator : IJavaLocator
    {
        public string GetJavaInstallationPath()
        {
            string javaHome = Environment.GetEnvironmentVariable("JAVA_HOME").Trim('"');
            var os = System.Environment.OSVersion.VersionString;
            if (os.ToLower().Contains("windows"))
                return Path.Combine(javaHome, "bin", "java.exe");
            else
                return Path.Combine(javaHome, "bin", "java");
        }
    }
}
