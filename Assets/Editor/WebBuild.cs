using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ShiftRunner.Editor
{
    public static class WebBuild
    {
        private const string ScenePath = "Assets/Scenes/PacketRun.unity";
        private const string OutputPath = "Builds/WebGL";
        private const string ArchivePath = "Builds/SHIFT-Packet-Run-WebGL.zip";

        [MenuItem("SHIFT/Build WebGL")]
        public static void Build()
        {
            Directory.CreateDirectory(OutputPath);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = OutputPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("WebGL build " + report.summary.result);

            // itch.io needs index.html in the root of the ZIP. Use forward slashes
            // in entry names even when the Unity editor runs on Windows.
            string temporary = ArchivePath + ".tmp";
            using (var file = File.Create(temporary))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
            {
                foreach (string source in Directory.GetFiles(OutputPath, "*", SearchOption.AllDirectories))
                {
                    string entry = source.Substring(OutputPath.Length + 1).Replace('\\', '/');
                    zip.CreateEntryFromFile(source, entry, System.IO.Compression.CompressionLevel.Optimal);
                }
            }
            if (File.Exists(ArchivePath)) File.Replace(temporary, ArchivePath, null);
            else File.Move(temporary, ArchivePath);
            Debug.Log("WebGL player and itch.io archive ready in " + Path.GetFullPath("Builds"));
        }
    }
}
