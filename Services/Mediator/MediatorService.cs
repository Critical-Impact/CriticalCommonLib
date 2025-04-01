using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CriticalCommonLib.Services.Mediator;

public class MediatorService : BackgroundService
{
    public ILogger<MediatorService> Logger { get; }
    private readonly object _addRemoveLock = new();
    private readonly Dictionary<object, DateTime> _lastErrorTime = new();
    private readonly ConcurrentQueue<MessageBase> _messageQueue = new();
    private readonly Dictionary<Type, HashSet<SubscriberAction>> _subscriberDict = new();
    private readonly SemaphoreSlim _signal = new(0);

    public MediatorService(ILogger<MediatorService> logger)
    {
        Logger = logger;
    }

    public void PrintSubscriberInfo()
    {
        foreach (var kvp in _subscriberDict.SelectMany(c => c.Value.Select(v => v))
            .DistinctBy(p => p.Subscriber).OrderBy(p => p.Subscriber.GetType().FullName, StringComparer.Ordinal).ToList())
        {
            var type = kvp.Subscriber.GetType().Name;
            var sub = kvp.Subscriber.ToString();
            Logger.LogInformation($"Subscriber {type}: {sub}");
            StringBuilder sb = new();
            sb.Append("=> ");
            foreach (var item in _subscriberDict.Where(item => item.Value.Any(v => v.Subscriber == kvp.Subscriber)).ToList())
            {
                sb.Append(item.Key.Name).Append(", ");
            }

            if (!string.Equals(sb.ToString(), "=> ", StringComparison.Ordinal))
            {
                Logger.LogInformation("{sb}", sb.ToString());
            }

            Logger.LogInformation("---");
        }
    }

    public void Publish<T>(T message) where T : MessageBase
    {
        if (message.KeepThreadContext)
        {
            ExecuteMessage(message);
        }
        else
        {
            _messageQueue.Enqueue(message);
            _signal.Release();
        }
    }

    public void Publish(List<MessageBase>? messages)
    {
        if (messages != null)
        {
            foreach (var message in messages)
            {
                if (message.KeepThreadContext)
                {
                    ExecuteMessage(message);
                }
                else
                {
                    _messageQueue.Enqueue(message);
                }
            }
            _signal.Release();
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await _signal.WaitAsync(stoppingToken);

            HashSet<MessageBase> processedMessages = [];
            while (_messageQueue.TryDequeue(out var message))
            {
                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                if (!processedMessages.Add(message)) { continue; }

                ExecuteMessage(message);
            }
        }
    }

    public override Task StartAsync(CancellationToken cancellationToken)
    {
        Logger.LogTrace("Starting service {Type} ({This})", GetType().Name, this);
        return base.StartAsync(cancellationToken);
    }

    public override Task StopAsync(CancellationToken cancellationToken)
    {
        var stopResult = base.StopAsync(cancellationToken);
        Logger.LogTrace("Stopping service {Type} ({This})", GetType().Name, this);
        _messageQueue.Clear();
        _signal.Dispose();
        return stopResult;
    }

    public void Subscribe<T>(IMediatorSubscriber subscriber, Action<T> action) where T : MessageBase
    {
        lock (_addRemoveLock)
        {
            _subscriberDict.TryAdd(typeof(T), new HashSet<SubscriberAction>());

            if (!_subscriberDict[typeof(T)].Add(new(subscriber, action)))
            {
                throw new InvalidOperationException("Already subscribed");
            }

            Logger.LogDebug("Subscriber added for message {Message}: {SubName}", typeof(T).Name, subscriber.GetType().Name);
        }
    }

    public void Unsubscribe<T>(IMediatorSubscriber subscriber) where T : MessageBase
    {
        lock (_addRemoveLock)
        {
            if (_subscriberDict.ContainsKey(typeof(T)))
            {
                _subscriberDict[typeof(T)].RemoveWhere(p => p.Subscriber == subscriber);
            }
        }
    }

    public void UnsubscribeAll(IMediatorSubscriber subscriber)
    {
        lock (_addRemoveLock)
        {
            foreach (Type kvp in _subscriberDict.Select(k => k.Key))
            {
                int unSubbed = _subscriberDict[kvp]?.RemoveWhere(p => p.Subscriber == subscriber) ?? 0;
                if (unSubbed > 0)
                {
                    Logger.LogDebug("{SubName} unsubscribed from {MessageName}", subscriber.GetType().Name, kvp.Name);
                }
            }
        }
    }

    private void ExecuteMessage(MessageBase message)
    {
        if (!_subscriberDict.TryGetValue(message.GetType(), out HashSet<SubscriberAction>? subscribers) || subscribers == null || !subscribers.Any()) return;

        HashSet<SubscriberAction> subscribersCopy;
        lock (_addRemoveLock)
        {
            subscribersCopy = subscribers?.Where(s => s.Subscriber != null).ToHashSet() ?? new HashSet<SubscriberAction>();
        }

        foreach (SubscriberAction subscriber in subscribersCopy)
        {
            try
            {
                typeof(MediatorService)
                    .GetMethod(nameof(ExecuteSubscriber), System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?
                    .MakeGenericMethod(message.GetType())
                    .Invoke(this, new object[] { subscriber, message });
            }
            catch (Exception ex)
            {
                if (_lastErrorTime.TryGetValue(subscriber, out var lastErrorTime) && lastErrorTime.Add(TimeSpan.FromSeconds(10)) > DateTime.UtcNow)
                    continue;

                Logger.LogError(ex, "Error executing {type} for subscriber {subscriber}", message.GetType().Name, subscriber.Subscriber.GetType().Name);
                _lastErrorTime[subscriber] = DateTime.UtcNow;
            }
        }
    }

    private void ExecuteSubscriber<T>(SubscriberAction subscriber, T message) where T : MessageBase
    {
        var isSameThread = message.KeepThreadContext ? "$" : string.Empty;
        ((Action<T>)subscriber.Action).Invoke(message);
    }

    private sealed class SubscriberAction
    {
        public SubscriberAction(IMediatorSubscriber subscriber, object action)
        {
            Subscriber = subscriber;
            Action = action;
        }

        public object Action { get; }
        public IMediatorSubscriber Subscriber { get; }
    }
}