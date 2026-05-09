using System.Threading.Channels;

namespace Mosaik.Services.Ai
{
    // ADR-012 — on-demand AI extraction trigger. Singleton in-memory queue.
    // Controller: await _queue.EnqueueAsync(extractionId) → AiExtractionWorker işler.
    // Bounded(100): taşarsa BoundedChannelFullMode.Wait ile backpressure (leak yok).
    public sealed class AiPipelineQueue
    {
        private readonly Channel<int> _channel;

        public AiPipelineQueue()
        {
            _channel = Channel.CreateBounded<int>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });
        }

        public ValueTask EnqueueAsync(int extractionId, CancellationToken ct = default)
            => _channel.Writer.WriteAsync(extractionId, ct);

        public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct)
            => _channel.Reader.ReadAllAsync(ct);
    }
}
