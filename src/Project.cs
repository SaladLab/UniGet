using System;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace UniGet
{
    internal class Project
    {
#pragma warning disable 0649

        public string Id;

        public class Dependency
        {
            public string Version;
            public string Source;
            public bool NoSample;
            public List<string> Include;
            public List<string> Exclude;
        }

        public Dictionary<string, Dependency> Dependencies;

        public List<JToken> Files;

#pragma warning restore 0649

        // Loader

        public static Project Load(string filePath)
        {
            var json = LoadJson(filePath, new HashSet<string>());
            return json.ToObject<Project>();
        }

        private static JObject LoadJson(string filePath, HashSet<string> openSet)
        {
            if (openSet.Add(Path.GetFullPath(filePath)) == false)
                throw new InvalidDataException("#base repeats!");

            var json = JObject.Parse(File.ReadAllText(filePath));

            var baseFile = json.GetValue("#base");
            if (baseFile == null)
                return json;

            json.Remove("#base");

            var baseJson = LoadJson(Path.Combine(Path.GetDirectoryName(filePath), baseFile.ToString()), openSet);
            baseJson.Merge(json);
            return baseJson;
        }
    }
}
