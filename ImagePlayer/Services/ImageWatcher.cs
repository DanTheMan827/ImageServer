namespace ImagePlayer.Services
{
    /// <summary>
    /// Monitors changes to image files in a specified directory and its subdirectories.
    /// </summary>
    public class ImageWatcher : IDisposable
    {
        private static readonly DateTime UnixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        private Uri BaseUri { get; set; }
        private Dictionary<string, Uri> Files { get; set; } = new();

        /// <summary>
        /// Gets the base path being monitored.
        /// </summary>
        public string BasePath { get; private set; }

        /// <summary>
        /// Gets or sets the list of <see cref="FileSystemWatcher"/> instances used to monitor the directory.
        /// </summary>
        public List<FileSystemWatcher> Watchers { get; set; } = new();

        /// <summary>
        /// Event triggered when the list of monitored files is updated.
        /// </summary>
        public delegate void FilesUpdated(KeyValuePair<string, Uri>[] files);

        /// <summary>
        /// Event triggered when the list of monitored files is updated.
        /// </summary>
        public event FilesUpdated OnFilesUpdated;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageWatcher"/> class.
        /// </summary>
        /// <param name="baseUri">The base URI used to construct image URIs.</param>
        /// <param name="path">The base path of the directory to monitor.</param>
        /// <param name="filters">File filters for image types (default is "*.jpg|*.gif|*.svg|*.png").</param>
        public ImageWatcher(Uri baseUri, string path, string filters = "*.jpg|*.gif|*.svg|*.png")
        {
            BaseUri = baseUri;
            BasePath = Path.GetFullPath(path);

            foreach (var filter in filters.Split("|"))
            {
                var watcher = new FileSystemWatcher(Path.GetFullPath(path), filter);
                watcher.Created += this.Watcher_Created;
                watcher.Changed += this.Watcher_Changed;
                watcher.Renamed += this.Watcher_Renamed;
                watcher.Deleted += this.Watcher_Deleted;
                watcher.IncludeSubdirectories = true;
                watcher.EnableRaisingEvents = true;
                Watchers.Add(watcher);

                lock (Files)
                {
                    foreach (var item in Directory.GetFiles(BasePath, filter, SearchOption.AllDirectories))
                    {
                        UpdateFile(item);
                    }
                }
            }
        }

        private string GetRelativePath(string path) => path.Substring(BasePath.Length + 1);

        private Uri GetUri(string path) => new Uri(BaseUri, $"{GetRelativePath(path)}?{(File.GetLastWriteTimeUtc(path) - UnixEpoch).TotalSeconds}");

        /// <summary>
        /// Gets an array of key-value pairs representing the current monitored files and their URIs.
        /// </summary>
        /// <returns>An array of key-value pairs representing file paths and their corresponding URIs.</returns>
        public KeyValuePair<string, Uri>[] GetFiles()
        {
            lock (Files)
            {
                return Files.ToArray();
            }
        }

        private void UpdateFile(string fullPath)
        {
            var uri = GetUri(fullPath);
            var key = GetRelativePath(fullPath);

            if (Files.ContainsKey(key))
            {
                Files[key] = uri;
            }
            else
            {
                Files.Add(key, uri);
            }
        }

        private void Watcher_Deleted(object sender, FileSystemEventArgs e)
        {
            var key = GetRelativePath(e.FullPath);

            lock (Files)
            {
                if (Files.ContainsKey(key))
                {
                    Files.Remove(key);
                }

                OnFilesUpdated?.Invoke(Files.ToArray());
            }
        }

        private void Watcher_Renamed(object sender, RenamedEventArgs e)
        {
            var oldKey = GetRelativePath(e.OldFullPath);

            lock (Files)
            {
                if (Files.ContainsKey(oldKey))
                {
                    Files.Remove(oldKey);
                }

                UpdateFile(e.FullPath);

                OnFilesUpdated?.Invoke(Files.ToArray());
            }
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            lock (Files)
            {
                UpdateFile(e.FullPath);

                OnFilesUpdated?.Invoke(Files.ToArray());
            }
        }

        private void Watcher_Created(object sender, FileSystemEventArgs e)
        {
            lock (Files)
            {
                UpdateFile(e.FullPath);

                OnFilesUpdated?.Invoke(Files.ToArray());
            }
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="ImageWatcher"/>.
        /// </summary>
        public void Dispose()
        {
            lock (Watchers)
            {
                foreach (var watcher in Watchers)
                {
                    watcher.Dispose();
                }
            }

            Watchers.Clear();
        }
    }
}