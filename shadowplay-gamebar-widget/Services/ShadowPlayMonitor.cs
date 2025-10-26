using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ShadowPlayReminderWidget.Models;
using Windows.Storage;
using Windows.Storage.Search;

namespace ShadowPlayReminderWidget.Services
{
    public sealed class ShadowPlayMonitor : IDisposable
    {
        private const string ShadowPlayFolderGuid = "9343b833-e7af-42ea-8a61-31bc41eefe2b";
        private const string ShadowPlayFilePrefix = "Sha";

        private StorageFolder _shadowPlayFolder;
        private StorageFileQueryResult _query;
        private bool _initialized;
        private ShadowPlayState? _lastState;

        public event EventHandler<ShadowPlayStatusChangedEventArgs> StatusChanged;

        public async Task InitializeAsync()
        {
            if (_initialized)
            {
                return;
            }

            _shadowPlayFolder = await ResolveShadowPlayFolderAsync();
            ConfigureFileWatcher(_shadowPlayFolder);
            _initialized = true;
        }

        public async Task<ShadowPlayStatusChangedEventArgs> CheckStatusAsync(ShadowPlayTrigger trigger)
        {
            ShadowPlayStatusChangedEventArgs snapshot;

            try
            {
                if (!_initialized)
                {
                    await InitializeAsync();
                }

                var files = await _query.GetFilesAsync();
                var hasShadowPlayFiles = files.Any(file =>
                    file.Name.StartsWith(ShadowPlayFilePrefix, StringComparison.OrdinalIgnoreCase) &&
                    file.Name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));

                var state = hasShadowPlayFiles ? ShadowPlayState.Active : ShadowPlayState.Inactive;
                var message = hasShadowPlayFiles
                    ? "ShadowPlay activity detected."
                    : "ShadowPlay files not found—ShadowPlay appears to be off.";

                snapshot = new ShadowPlayStatusChangedEventArgs(
                    state,
                    message,
                    trigger,
                    DateTimeOffset.Now,
                    null,
                    _lastState != state);

                _lastState = state;
            }
            catch (Exception ex)
            {
                snapshot = new ShadowPlayStatusChangedEventArgs(
                    ShadowPlayState.Error,
                    $"Unable to check ShadowPlay files: {ex.Message}",
                    trigger,
                    DateTimeOffset.Now,
                    ex,
                    _lastState != ShadowPlayState.Error);
                _lastState = ShadowPlayState.Error;
            }

            StatusChanged?.Invoke(this, snapshot);
            return snapshot;
        }

        private void ConfigureFileWatcher(StorageFolder folder)
        {
            var queryOptions = new QueryOptions(CommonFileQuery.DefaultQuery, new[] { ".tmp" })
            {
                FolderDepth = FolderDepth.Shallow,
                IndexerOption = IndexerOption.DoNotUseIndexer
            };

            _query = folder.CreateFileQueryWithOptions(queryOptions);
            _query.ContentsChanged += QueryOnContentsChanged;

            // Prime the watcher so it begins tracking changes immediately.
            var _ = _query.GetFilesAsync();
        }

        private async void QueryOnContentsChanged(StorageFileQueryResult sender, object args)
        {
            await CheckStatusAsync(ShadowPlayTrigger.Watcher);
        }

        private static async Task<StorageFolder> ResolveShadowPlayFolderAsync()
        {
            var paths = UserDataPaths.GetDefault();
            var folderPath = Path.Combine(paths.Profile, "AppData", "Local", "Temp", ShadowPlayFolderGuid);

            try
            {
                return await StorageFolder.GetFolderFromPathAsync(folderPath);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Unable to access {folderPath}. Enable file system access for the widget under Settings > Privacy & security > File system. ({ex.Message})",
                    ex);
            }
        }

        public void Dispose()
        {
            if (_query != null)
            {
                _query.ContentsChanged -= QueryOnContentsChanged;
                _query = null;
            }

            _shadowPlayFolder = null;
            _initialized = false;
        }
    }
}
