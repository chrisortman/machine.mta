using System;
using NServiceBus;

namespace Machine.Mta
{
  public class MessageBusProxy : IMessageBus
  {
    readonly IMessageBusManager _messageBusManager;

    public MessageBusProxy(IMessageBusManager messageBusManager)
    {
      _messageBusManager = messageBusManager;
    }

    public void Dispose()
    {
      // We are disposed by the MessageBusManager
      // All(x => x.Dispose());
    }

    public void Start()
    {
      All(x => x.Start());
    }

    public void Send<T>(params T[] messages) where T : IMessage
    {
      CurrentBus().Send(messages);
    }

    public void Send<T>(EndpointAddress destination, params T[] messages) where T : IMessage
    {
      CurrentBus().Send(destination, messages);
    }

    public void Send<T>(EndpointAddress destination, string correlationId, params T[] messages) where T : IMessage
    {
      CurrentBus().Send(destination, correlationId, messages);
    }

    public void Send(EndpointAddress destination, MessagePayload payload)
    {
      CurrentBus().Send(destination, payload);
    }

    public void SendLocal<T>(params T[] messages) where T : IMessage
    {
      CurrentBus().SendLocal(messages);
    }

    public void Stop()
    {
      All(x => x.Stop());
    }

    public IRequestReplyBuilder Request<T>(params T[] messages) where T : IMessage
    {
      return CurrentBus().Request(messages);
    }

    public void Reply<T>(params T[] messages) where T : IMessage
    {
      CurrentBus().Reply(messages);
    }

    public void Publish<T>(params T[] messages) where T : IMessage
    {
      CurrentBus().Publish(messages);
    }

    private IMessageBus CurrentBus()
    {
      return CurrentMessageBus.Current ?? _messageBusManager.DefaultBus;
    }

    private void All(Action<IMessageBus> action)
    {
      _messageBusManager.EachBus(action);
    }
  }
}