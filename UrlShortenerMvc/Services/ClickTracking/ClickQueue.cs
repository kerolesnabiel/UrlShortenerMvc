using System.Threading.Channels;

namespace UrlShortenerMvc.Services.ClickTracking
{
    public interface IClickQueue
    {
        ValueTask EnqueueAsync(ClickEvent click, CancellationToken cancellationToken);
        ValueTask<ClickEvent> DequeueAsync(CancellationToken cancellationToken);
    }

    public sealed class ClickQueue : IClickQueue
    {
        private readonly Channel<ClickEvent> _channel = Channel.CreateBounded<ClickEvent>(
            new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = false
            });

        public ValueTask EnqueueAsync(ClickEvent click, CancellationToken cancellationToken)
        {
            return _channel.Writer.WriteAsync(click, cancellationToken);
        }

        public ValueTask<ClickEvent> DequeueAsync(CancellationToken cancellationToken)
        {
            return _channel.Reader.ReadAsync(cancellationToken);
        }
    }
}