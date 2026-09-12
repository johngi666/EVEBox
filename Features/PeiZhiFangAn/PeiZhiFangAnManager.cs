using EVEBox.Common.PeiZhi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;



namespace EVEBox.Features.PeiZhiFangAn
{
    public class PeiZhiFangAnManager
    {
        private readonly PeiZhiManager _configManager;
        private List<FangAn> _schemes;

        public PeiZhiFangAnManager()
        {
            _configManager = new PeiZhiManager();
            Load();
        }

        public List<FangAn> GetAll()
        {
            return _schemes.OrderByDescending(s => s.LastUsedTime).ThenBy(s => s.Name).ToList();
        }

        public FangAn GetById(string id)
        {
            return _schemes.FirstOrDefault(s => s.Id == id);
        }

        public bool Add(FangAn scheme)
        {
            if (string.IsNullOrWhiteSpace(scheme.Name)) return false;
            if (string.IsNullOrWhiteSpace(scheme.FolderPath)) return false;
            if (!Directory.Exists(scheme.FolderPath)) return false;
            if (_schemes.Any(s => s.Name.Equals(scheme.Name, StringComparison.OrdinalIgnoreCase))) return false;

            _schemes.Add(scheme);
            Save();
            return true;
        }

        public bool Update(FangAn scheme)
        {
            var existing = GetById(scheme.Id);
            if (existing == null) return false;

            existing.Name = scheme.Name;
            existing.FolderPath = scheme.FolderPath;
            existing.Description = scheme.Description;
            Save();
            return true;
        }

        public bool Remove(string id)
        {
            var scheme = GetById(id);
            if (scheme == null) return false;

            _schemes.Remove(scheme);
            Save();
            return true;
        }

        public void UpdateLastUsed(string id)
        {
            var scheme = GetById(id);
            if (scheme != null)
            {
                scheme.LastUsedTime = DateTime.Now;
                Save();
            }
        }

        private void Load()
        {
            var config = _configManager.Config;
            _schemes = config.ConfigSchemes ?? new List<FangAn>();
        }

        private void Save()
        {
            var config = _configManager.Config;
            config.ConfigSchemes = _schemes;
            _configManager.Save();
        }
    }
}