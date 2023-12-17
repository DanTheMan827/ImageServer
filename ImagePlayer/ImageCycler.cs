using ImagePlayer.Services;
using System.Text.RegularExpressions;
using System.Timers;

namespace ImagePlayer
{
    /// <summary>
    /// Represents an image cycler that cycles through a collection of image URIs based on a specified timer.
    /// </summary>
    public class ImageCycler : IDisposable
    {
        private string[] Uris = new string[] { };
        private int CurrentIndex { get; set; }
        private readonly System.Timers.Timer timer = new();
        private readonly ImageWatcher imageWatcher;
        private string ImagePath { get; set; }
        internal int ReferenceCount { get; set; } = 0;

        /// <summary>
        /// Event triggered when the current displayed image changes.
        /// </summary>
        public delegate void ImageChanged(string? imagePath);

        /// <summary>
        /// Event triggered when the current displayed image changes.
        /// </summary>
        public event ImageChanged OnImageChanged;

        /// <summary>
        /// Gets the path of the currently displayed image.
        /// </summary>
        public string? CurrentImage
        {
            get
            {
                lock (Uris)
                {
                    var index = CurrentIndex;
                    return index < Uris.Length ? Uris[index] : null;
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCycler"/> class.
        /// </summary>
        /// <param name="watcher">An instance of <see cref="ImageWatcher"/> used to monitor image file changes.</param>
        /// <param name="path">The base path for images to be displayed.</param>
        public ImageCycler(ImageWatcher watcher, string path)
        {
            ImagePath = path;
            imageWatcher = watcher;
            imageWatcher.OnFilesUpdated += Watcher_OnFilesUpdated;
            Watcher_OnFilesUpdated(imageWatcher.GetFiles());
            timer.Elapsed += Timer_Elapsed;
            timer.Interval = 10000; // Default interval is set to 10 seconds.
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="ImageCycler"/>.
        /// </summary>
        public void Dispose()
        {
            timer.Stop();
            imageWatcher.OnFilesUpdated -= Watcher_OnFilesUpdated;
            timer.Dispose();
        }

        private void Watcher_OnFilesUpdated(KeyValuePair<string, Uri>[] files)
        {
            lock (Uris)
            {
                timer.Stop();

                // Filter files based on the specified ImagePath and order them.
                Uris = files
                    .Where(item => item.Key.StartsWith(ImagePath))
                    .Select(item => item.Value.PathAndQuery)
                    .OrderBy(e => e)
                    .ToArray();

                // If no files match the specified ImagePath, fallback to files starting with "default".
                if (Uris.Length == 0)
                {
                    Uris = files
                        .Where(item => item.Key.StartsWith("default"))
                        .Select(item => item.Value.PathAndQuery)
                        .OrderBy(e => e)
                        .ToArray();
                }

                CurrentIndex = -1;
                Timer_Elapsed(null, null);
                timer.Start();
            }
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs? e)
        {
            // Move to the next image index or loop back to the beginning.
            CurrentIndex = CurrentIndex < Uris.Length - 1 ? CurrentIndex + 1 : 0;

            // Define a regex pattern to extract time information from the image URL.
            var timeRegex = new Regex(@"-(\d+)sec\.[a-zA-Z]+(?:\?.*)?$");

            if (Uris.Length > CurrentIndex)
            {
                var url = Uris[CurrentIndex];
                var match = timeRegex.Match(url);

                // If the regex matches, update the timer interval based on the extracted time.
                if (match.Success)
                {
                    timer.Interval = int.Parse(match.Groups[1].Value) * 1000;
                }
                else
                {
                    // If no match, use the default interval (10 seconds).
                    timer.Interval = 10000;
                }
            }

            lock (Uris)
            {
                // Trigger the event to notify that the displayed image has changed.
                OnImageChanged?.Invoke(CurrentIndex < Uris.Length ? Uris[CurrentIndex] : null);
            }
        }
    }
}