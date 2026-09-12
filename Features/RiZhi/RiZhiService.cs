using System;
using System.Collections.Generic;
using System.Diagnostics;



namespace EVEBox.Features.RiZhi
{
    public class RiZhiService
    {
        private readonly List<string> _operationLog = new List<string>();
        private const int MaxLogEntries = 999;
        private readonly object _lock = new object();

        public IReadOnlyList<string> GetLogs()
        {
            lock (_lock)
            {
                return new List<string>(_operationLog).AsReadOnly();
            }
        }

        public int LogCount
        {
            get
            {
                lock (_lock)
                {
                    return _operationLog.Count;
                }
            }
        }

        public void Log(string operation, string status = "", string details = "")
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string entry = $"[{timestamp}] {operation}";
            if (!string.IsNullOrEmpty(status)) entry += $" - {status}";
            if (!string.IsNullOrEmpty(details)) entry += $" - {details}";

            lock (_lock)
            {
                _operationLog.Add(entry);
                if (_operationLog.Count > MaxLogEntries)
                    _operationLog.RemoveAt(0);
            }

            Debug.WriteLine(entry);
        }

        public void Log(string message)
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            string entry = $"[{timestamp}] {message}";

            lock (_lock)
            {
                _operationLog.Add(entry);
                if (_operationLog.Count > MaxLogEntries)
                    _operationLog.RemoveAt(0);
            }

            Debug.WriteLine(entry);
        }

        public void Clear()
        {
            lock (_lock)
            {
                _operationLog.Clear();
            }
        }

        public string GetSummary()
        {
            lock (_lock)
            {
                if (_operationLog.Count == 0)
                    return "暂无日志";

                string lastEntry = _operationLog[_operationLog.Count - 1];
                return $"最后记录: {lastEntry}";
            }
        }

        public List<string> GetLastLogs(int count)
        {
            lock (_lock)
            {
                int start = Math.Max(0, _operationLog.Count - count);
                return _operationLog.GetRange(start, _operationLog.Count - start);
            }
        }
    }
}