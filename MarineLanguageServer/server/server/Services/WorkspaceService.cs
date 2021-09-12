using System.Collections.Concurrent;

namespace MarineLang.LanguageServerImpl.Services
{
    public class WorkspaceService
    {
        private ConcurrentDictionary<string, string> _fileBuffers = new ConcurrentDictionary<string, string>();

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
    }
}