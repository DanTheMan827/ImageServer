namespace ImagePlayer.Services
{
    /// <summary>
    /// Manages a collection of <see cref="ImageCycler"/> instances associated with specific image paths.
    /// </summary>
    public class ImageCyclerContainer : IDisposable
    {
        private readonly Dictionary<string, ImageCycler> Cyclers = new();
        private readonly ImageWatcher Watcher;

        /// <summary>
        /// Initializes a new instance of the <see cref="ImageCyclerContainer"/> class.
        /// </summary>
        /// <param name="watcher">The <see cref="ImageWatcher"/> instance to be used for monitoring image file changes.</param>
        public ImageCyclerContainer(ImageWatcher watcher)
        {
            Watcher = watcher;
        }

        /// <summary>
        /// Gets an <see cref="ImageCycler"/> instance associated with the specified image path.
        /// If an instance already exists, it is returned; otherwise, a new one is created.
        /// </summary>
        /// <param name="path">The image path for which to get or create an <see cref="ImageCycler"/>.</param>
        /// <returns>An <see cref="ImageCycler"/> instance associated with the specified image path.</returns>
        public ImageCycler GetCycler(string path)
        {
            lock (Cyclers)
            {
                ImageCycler cycler;

                if (Cyclers.ContainsKey(path))
                {
                    cycler = Cyclers[path];
                }
                else
                {
                    cycler = new ImageCycler(Watcher, path);
                    Cyclers.Add(path, cycler);
                }

                cycler.ReferenceCount++;

                return cycler;
            }
        }

        /// <summary>
        /// Releases an <see cref="ImageCycler"/> instance associated with the specified image path.
        /// If the reference count reaches zero, the instance is disposed and removed from the container.
        /// </summary>
        /// <param name="path">The image path for which to release the associated <see cref="ImageCycler"/>.</param>
        public void ReleaseCycler(string path)
        {
            lock (Cyclers)
            {
                if (Cyclers.ContainsKey(path))
                {
                    var cycler = Cyclers[path];

                    cycler.ReferenceCount--;

                    if (cycler.ReferenceCount == 0)
                    {
                        cycler.Dispose();
                        Cyclers.Remove(path);
                    }
                }
            }
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="ImageCyclerContainer"/>.
        /// Disposes all associated <see cref="ImageCycler"/> instances.
        /// </summary>
        public void Dispose()
        {
            foreach (var item in Cyclers.Values)
            {
                item.Dispose();
            }

            Cyclers.Clear();
        }
    }
}