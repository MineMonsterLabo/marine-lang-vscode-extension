using System.Collections.Concurrent;

namespace server
{
    public class MarineLangWorkspaceService
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