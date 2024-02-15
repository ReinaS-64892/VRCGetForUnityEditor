using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace net.rs64.VRCGetForUnityEditor
{
    internal static class RequestVRCGet
    {
        private static ProcessStartInfo VRCGetStartInfo(string arguments)
        {
            var startInfo = new ProcessStartInfo();
            startInfo.WorkingDirectory = Environment.CurrentDirectory;
            startInfo.RedirectStandardError = true;
            startInfo.RedirectStandardOutput = true;
            startInfo.UseShellExecute = false;
            startInfo.CreateNoWindow = true;
            // startInfo.StandardOutputEncoding = startInfo.StandardErrorEncoding = startInfo.StandardOutputEncoding = Encoding.GetEncoding("shift_jis");
            startInfo.FileName = "vrc-get";
            startInfo.Arguments = arguments;

            return startInfo;
        }

        private static string Request(string arguments, int wait = 100)
        {
            using (var proses = new Process())
            {
                proses.StartInfo = VRCGetStartInfo(arguments);
                proses.Start();

                proses.WaitForExit(wait);
                // if (!proses.HasExited) { proses.Kill(); return null; }
                // try { if (proses.ExitCode != 0) { return proses.StandardError.ReadToEnd(); } } catch (Exception e) { Debug.LogException(e); }

                return proses.StandardOutput.ReadToEnd();
            }
        }
        private static async void RequestVoid(string arguments)
        {
            await Task.Run(() => runProses(arguments));

            static void runProses(string arguments)
            {
                using (var proses = new Process())
                {
                    proses.StartInfo = VRCGetStartInfo(arguments);
                    proses.Start();

                    proses.WaitForExit();

                    try { if (proses.ExitCode != 0) { Debug.LogError(proses.StandardError.ReadToEnd()); return; } } catch (Exception e) { Debug.LogException(e); }

                    Debug.Log(proses.StandardOutput.ReadToEnd());
                }
            }
        }
        public static Project GetPackages()
        {
            return JsonUtility.FromJson<Project>(Request("info project --json-format 1"));
        }

        public static List<string> GetVersions(string packageName)
        {
            return JsonUtility.FromJson<OutputVersions>(Request($"info package {packageName} --json-format 1"))?.versions?.Select(Selector)?.ToList();
            static string Selector(OutputVersions.OutputVersion i) => i.version;
        }
        public static void Install(string packageName)
        {
            RequestVoid($"install --yes {packageName}");
        }
        public static void Install(string packageName, string version)
        {
            RequestVoid($"install --yes {packageName} {version}");
        }
        public static string Remove(string packageName)
        {
            return Request($"remove --yes {packageName}");
        }

        public static void Upgrade()
        {
             RequestVoid("upgrade");
        }

        public static List<string> Repositories()//URLが返ってくる
        {
            return Request("repo list").Split("\n").Select(i =>
            {
                var s = i.Split(" ");
                if (s.Length < 3) { return null; }
                return s[s.Length - 3];
            }).Where(i => i != null).ToList();
        }
        public static List<string> PackageNames(string urlOrName)
        {
            var result = Request($"repo packages {urlOrName}");
            if (result == null) { return null; }
            return result.Split("\n").Where(i => !string.IsNullOrWhiteSpace(i) && i.Contains("| ")).Select(i => i.Split("| ")?.LastOrDefault()).Where(i => !string.IsNullOrWhiteSpace(i)).ToList();
        }

        [Serializable]
        public class OutputVersions
        {
            public OutputVersion[] versions;
            [Serializable]
            public class OutputVersion
            {
                public string version;
            }
        }
    }

    [Serializable]
    internal class Project
    {
        public string unity_version;
        public Package[] packages;
    }

    [Serializable]
    public class Package
    {
        public string name;
        public string installed;
        public string locked;
        //public ??? requested;
    }
}
