using System.Collections.Concurrent;
using Lumina.Core.Configuration;
using NSubstitute;

namespace Lumina.Core.Tests;

public sealed class JsonConfigurationStoreTests
{
    [Fact]
    public void LoadSettingsAsync_SyncWaitUnderSynchronizationContext_DoesNotDeadlock()
    {
        var tempDirectory = Path.Combine(Path.GetTempPath(), "Lumina.Core.Tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            File.WriteAllText(Path.Combine(tempDirectory, "settings.json"), """{"language":"zh-CN"}""");

            var keyStorage = Substitute.For<IKeyStorage>();
            var store = new JsonConfigurationStore(keyStorage, tempDirectory);

            var completed = false;
            Exception? exception = null;

            var thread = new Thread(() =>
            {
                try
                {
                    SynchronizationContext.SetSynchronizationContext(new NonPumpingSynchronizationContext());
                    _ = store.LoadSettingsAsync().GetAwaiter().GetResult();
                    completed = true;
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            })
            {
                IsBackground = true,
            };

            thread.Start();

            Assert.True(thread.Join(TimeSpan.FromSeconds(2)));
            Assert.True(completed);
            Assert.Null(exception);
        }
        finally
        {
            try
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
            catch
            {
            }
        }
    }

    private sealed class NonPumpingSynchronizationContext : SynchronizationContext
    {
        private readonly ConcurrentQueue<(SendOrPostCallback Callback, object? State)> _queue = new();

        public override void Post(SendOrPostCallback d, object? state)
        {
            _queue.Enqueue((d, state));
        }
    }
}
