using EVEBox.Common.Config;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;



namespace EVEBox.Features.ConfigScheme
{
    public class ConfigSchemeManager
    {
        private readonly ConfigManager _configManager;
        private List<Scheme> _schemes;

        public ConfigSchemeManager()
        {
            _configManager = new ConfigManager();
            Load();
        }

        public List<Scheme> GetAll()
        {
            return _schemes.OrderByDescending(s => s.LastUsedTime).ThenBy(s => s.Name).ToList();
        }

        public Scheme GetById(string id)
        {
            return _schemes.FirstOrDefault(s => s.Id == id);
        }

        public bool Add(Scheme scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme.Name)) return false;
            if (string.IsNullOrWhiteSpace(scheme.FolderPath)) return false;
            if (!Directory.Exists(scheme.FolderPath)) return false;
            if (_schemes.Any(s => s.Name.Equals(scheme.Name, StringComparison.OrdinalIgnoreCase))) return false;

            _schemes.Add(scheme);
            Save();
            return true;
        }

        public bool Update(Scheme scheme)
        {
            Scheme existing = GetById(scheme.Id);
            if (existing == null) return false;

            existing.Name = scheme.Name;
            existing.FolderPath = scheme.FolderPath;
            existing.Description = scheme.Description;
            Save();
            return true;
        }

        public bool Remove(string id)
        {
            Scheme scheme = GetById(id);
            if (scheme == null) return false;

            _schemes.Remove(scheme);
            Save();
            return true;
        }

        public void UpdateLastUsed(string id)
        {
            Scheme scheme = GetById(id);
            if (scheme != null)
            {
                scheme.LastUsedTime = DateTime.Now;
                Save();
            }
        }

        private void Load()
        {
            var config = _configManager.Config;
            _schemes = config.ConfigSchemes ?? new List<Scheme>();
        }

        private void Save()
        {
            var config = _configManager.Config;
            config.ConfigSchemes = _schemes;
            _configManager.Save();
        }
    }
}