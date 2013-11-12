﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Loader
{
    public class FileWatcher : IFileWatcher
    {
        private readonly string _path;
        private readonly HashSet<string> _files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, HashSet<string>> _directories = new ConcurrentDictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);

        private readonly FileSystemWatcher _watcher;

        public static readonly IFileWatcher Noop = new NoopWatcher();

        public FileWatcher(string path)
        {
            _path = path;
            _watcher = new FileSystemWatcher(path);
            _watcher.IncludeSubdirectories = true;
            _watcher.EnableRaisingEvents = true;

            _watcher.Changed += OnWatcherChanged;
            _watcher.Renamed += OnRenamed;
            _watcher.Deleted += OnWatcherChanged;
            _watcher.Created += OnWatcherChanged;
        }

        public event Action OnChanged;

        public void WatchDirectory(string path, string extension)
        {
            var extensions = _directories.GetOrAdd(path, _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase));

            extensions.Add(extension);
        }

        public bool WatchFile(string path)
        {
            return _files.Add(path);
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            ReportPathChanged(e.OldFullPath, e.ChangeType);
            ReportPathChanged(e.FullPath, e.ChangeType);
        }

        private void OnWatcherChanged(object sender, FileSystemEventArgs e)
        {
            ReportPathChanged(e.FullPath, e.ChangeType);
        }

        private void ReportPathChanged(string path, WatcherChangeTypes changeType)
        {
            if (HasChanged(path))
            {
                Trace.TraceInformation("{0} -> {1}", changeType, path);

                if (OnChanged != null)
                {
                    OnChanged();
                }
            }
        }

        private bool HasChanged(string path)
        {
            if (_files.Contains(path))
            {
                return true;
            }

            HashSet<string> extensions;
            if (_directories.TryGetValue(path, out extensions) ||
                _directories.TryGetValue(Path.GetDirectoryName(path), out extensions))
            {
                return extensions.Contains(Path.GetExtension(path));
            }

            return false;
        }

        private class NoopWatcher : IFileWatcher
        {
            public bool WatchFile(string path)
            {
                return true;
            }

            public event Action OnChanged;

            public void WatchDirectory(string path, string extension)
            {
            }
        }
    }
}