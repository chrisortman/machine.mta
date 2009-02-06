using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Mta.Sagas;
using Machine.Container.Model;
using Machine.Container.Services;

namespace Machine.Mta.Dispatching
{
  public class MessageHandlerType
  {
    readonly Type _targetType;
    readonly Type _consumerType;

    public Type TargetType
    {
      get { return _targetType; }
    }

    public Type ConsumerType
    {
      get { return _consumerType; }
    }

    public Type TargetExpectsMessageOfType
    {
      get { return _consumerType.GetGenericArguments()[0]; }
    }

    public MessageHandlerType(Type targetType, Type consumerType)
    {
      _targetType = targetType;
      _consumerType = consumerType;
    }

    public override string ToString()
    {
      return "Invoke " + this.TargetType.FullName + " to handle " + this.TargetExpectsMessageOfType.FullName;
    }
  }

  public class HandlerDiscoverer
  {
    private readonly IMachineContainer _container;

    public HandlerDiscoverer(IMachineContainer container)
    {
      _container = container;
    }

    private IEnumerable<Type> TypesThatAreHandlers()
    {
      foreach (ServiceRegistration registration in _container.RegisteredServices)
      {
        if (registration.ServiceType.IsImplementationOfGenericType(typeof(IConsume<>)))
        {
          yield return registration.ServiceType;
        }
      }
    }

    public IEnumerable<MessageHandlerType> GetHandlerTypesFor(Type messageType)
    {
      List<MessageHandlerType> messageHandlerTypes = new List<MessageHandlerType>();

      foreach (Type handlerType in TypesThatAreHandlers())
      {
        IEnumerable<Type> handlerConsumes = handlerType.AllGenericVariations(typeof(IConsume<>)).BiggerThan(typeof(IConsume<>).MakeGenericType(messageType));
        Type smallerType = handlerConsumes.SmallerType();
        if (smallerType != null)
        {
          messageHandlerTypes.Add(new MessageHandlerType(handlerType, smallerType));
        }
      }

      return ApplyOrdering(messageType, messageHandlerTypes);
    }

    private IEnumerable<MessageHandlerType> ApplyOrdering(Type messageType, IEnumerable<MessageHandlerType> handlerTypes)
    {
      List<MessageHandlerType> remaining = new List<MessageHandlerType>(handlerTypes);
      List<MessageHandlerType> ordered = new List<MessageHandlerType>();
      foreach (Type handlerOfType in GetHandlerOrderFor(messageType))
      {
        foreach (MessageHandlerType messageHandlerType in new List<MessageHandlerType>(remaining))
        {
          if (handlerOfType.IsAssignableFrom(messageHandlerType.TargetType))
          {
            ordered.Add(messageHandlerType);
            remaining.Remove(messageHandlerType);
            break;
          }
        }
      }
      ordered.AddRange(remaining);
      return ordered;
    }

    private IEnumerable<Type> GetHandlerOrderFor(Type messageType)
    {
      object orderer = _container.Resolve.All(type => {
        return type.IsGenericlyCompatible(typeof(IProvideHandlerOrderFor<>).MakeGenericType(messageType));
      }).FirstOrDefault();
      if (orderer == null)
      {
        return new List<Type>();
      }
      IProvideHandlerOrderFor<IMessage> orderProvider = Invokers.CreateForHandlerOrderProvider(messageType, orderer);
      return orderProvider.GetHandlerOrder();
    }
  }

  public class MessageDispatcher : IMessageDispatcher
  {
    readonly IMachineContainer _container;
    readonly HandlerDiscoverer _handlerDiscoverer;
    readonly IMessageAspectsProvider _messageAspectsProvider;

    public MessageDispatcher(IMachineContainer container, IMessageAspectsProvider messageAspectsProvider)
    {
      _container = container;
      _messageAspectsProvider = messageAspectsProvider;
      _handlerDiscoverer = new HandlerDiscoverer(container);
    }

    private void Dispatch(IMessage message)
    {
      Logging.Dispatch(message);
      foreach (MessageHandlerType messageHandlerType in _handlerDiscoverer.GetHandlerTypesFor(message.GetType()))
      {
        object handler = _container.Resolve.Object(messageHandlerType.TargetType);
        IConsume<IMessage> invoker = Invokers.CreateForHandler(messageHandlerType.TargetExpectsMessageOfType, handler);
        HandlerInvocation invocation = messageHandlerType.ToInvocation(message, handler, invoker, _messageAspectsProvider.DefaultAspects());
        invocation.Continue();
      }
    }

    public void Dispatch(IMessage[] messages)
    {
      foreach (IMessage message in messages)
      {
        Dispatch(message);
      }
    }
  }

  public static class InvocationMappings
  {
    public static HandlerInvocation ToInvocation(this MessageHandlerType messageHandlerType, IMessage message, object handler, IConsume<IMessage> invoker, Queue<IMessageAspect> aspects)
    {
      return new HandlerInvocation(message, messageHandlerType.TargetExpectsMessageOfType, messageHandlerType.TargetType, handler, invoker, aspects);
    }
  }

  public class HandlerInvocation
  {
    readonly IMessage _message;
    readonly Type _messageType;
    readonly Type _handlerType;
    readonly object _handler;
    readonly Queue<IMessageAspect> _aspects;
    readonly IConsume<IMessage> _invoker;

    public IMessage Message
    {
      get { return _message; }
    }

    public Type MessageType
    {
      get { return _messageType; }
    }

    public Type HandlerType
    {
      get { return _handlerType; }
    }

    public object Handler
    {
      get { return _handler; }
    }

    public log4net.ILog HandlerLogger
    {
      get { return log4net.LogManager.GetLogger("Machine.Mta.Sagas.Handlers." + _handlerType.FullName); }
    }

    public HandlerInvocation(IMessage message, Type messageType, Type handlerType, object handler, IConsume<IMessage> invoker, Queue<IMessageAspect> aspects)
    {
      _message = message;
      _aspects = aspects;
      _messageType = messageType;
      _handlerType = handlerType;
      _handler = handler;
      _invoker = invoker;
    }

    public void Continue()
    {
      if (_aspects.Count > 0)
      {
        _aspects.Dequeue().Continue(this);
      }
      else
      {
        _invoker.Consume(_message);
      }
    }
  }

  public interface IMessageAspectsProvider
  {
    Queue<IMessageAspect> DefaultAspects();
  }

  public class DefaultMessageAspectsProvider : IMessageAspectsProvider
  {
    readonly IMachineContainer _container;

    public DefaultMessageAspectsProvider(IMachineContainer container)
    {
      _container = container;
    }

    protected virtual Type[] AspectTypes
    {
      get { return new[] { typeof(SagaAspect) }; }
    }

    public Queue<IMessageAspect> DefaultAspects()
    {
      Queue<IMessageAspect> aspects = new Queue<IMessageAspect>();
      foreach (Type type in this.AspectTypes)
      {
        aspects.Enqueue((IMessageAspect)_container.Resolve.Object(type));
      }
      return aspects;
    }
  }

  public interface IMessageAspect
  {
    void Continue(HandlerInvocation invocation);
  }
}