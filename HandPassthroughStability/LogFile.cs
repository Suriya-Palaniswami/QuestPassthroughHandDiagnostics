// Assets/HandPassthroughStability/Runtime/Diagnostics/LogFile.cs
using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace HPS.Stability.Diagnostics
{
    public static class LogFile
    {
        static string _dir;
        static string _path;
        static object _lock = new object();
        static bool _inited;

        public static string CurrentPath => _path;

        public static void Init(string customPrefix = "hand_passthrough_test")
        {
            if (_inited) return;
            _dir = Path.Combine(Application.persistentDataPath, "logs");
            Directory.CreateDirectory(_dir);
            var ts = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            _path = Path.Combine(_dir, $"{customPrefix}_{ts}.log");
            _inited = true;
            WriteLine($"[INIT] Log started: {_path}");
            WriteLine($"[ENV] Unity {Application.unityVersion} | {SystemInfo.deviceModel} | {SystemInfo.operatingSystem}");
        }

        public static void WriteLine(string line)
        {
            if (!_inited) Init();
            lock (_lock)
            {
                var ts = DateTime.UtcNow.ToString("HH:mm:ss.fff");
                File.AppendAllText(_path, $"[{ts}] {line}\n", Encoding.UTF8);
            }
        }
    }
}
