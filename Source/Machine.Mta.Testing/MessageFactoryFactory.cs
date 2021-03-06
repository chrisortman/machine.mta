using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using NServiceBus;

namespace Machine.Mta.Testing
{
  public static class MessageFactoryFactory
  {
    public static IMessageFactory NewFactoryForMessages(params Type[] messageTypes)
    {
      var registerer = new MessageRegisterer();
      registerer.AddMessageTypes(typeof(IMessage));
      registerer.AddMessageTypes(messageTypes);
      var factory = new MessageFactory();
      factory.Initialize(messageTypes);
      return factory;
    }

    public static IMessageFactory NewFactoryForMessagesInAssemblies(params Assembly[] assemblies)
    {
      return NewFactoryForMessages(assemblies.SelectMany(a => a.GetTypes()).Where(type => typeof (IMessage).IsAssignableFrom(type)).ToArray());
    }

    public static IMessageFactory MessageFactory
    {
      get { return _messageFactory(); }
    }

    static Func<IMessageFactory> _messageFactory;

    public static void SetMessageFactoryFactory(Func<IMessageFactory> factory)
    {
      _messageFactory = factory;
    }
  }
}
