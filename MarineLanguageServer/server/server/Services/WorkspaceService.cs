using System.Collections.Concurrent;
using System.IO;
using Newtonsoft.Json.Linq;

namespace MarineLang.LanguageServerImpl.Services
{
    public class WorkspaceService
    {
        public const string MarineLangConfigFile = "marine-lang-config.json";

        private readonly ConcurrentDictionary<string, string> _fileBuffers = new ConcurrentDictionary<string, string>();

        private string _dumpPath;

        public string RootPath { get; private set; }

        public void UpdateMarineFileBuffer(string filePath, string buffer)
        {
            if (_fileBuffers.TryGetValue(filePath, out string current))
                _fileBuffers.TryUpdate(filePath, buffer, current);
            else
                _fileBuffers.TryAdd(filePath, buffer);
        }

        public string GetMarineFileBuffer(string filePath)
        {
            if (_fileBuffers.TryGetValue(filePath, out string buffer))
            {
                return buffer;
            }

            return null;
        }

        public void SetRootPath(string path)
        {
            RootPath = path;
        }

        internal void LoadConfiguration()
        {
            var configPath = $"{RootPath}\\{MarineLangConfigFile}";
            if (File.Exists(configPath))
            {
                var jObject = JObject.Parse(File.ReadAllText(configPath));
                if (jObject.TryGetValue("dumpPath", out JToken dumpPathToken))
                    _dumpPath = dumpPathToken.Value<string>();
            }
        }
    }
}