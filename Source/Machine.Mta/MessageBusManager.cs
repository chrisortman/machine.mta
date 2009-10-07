using System;
using System.Collections.Generic;
using System.Linq;

using Machine.Core;
using Machine.Container;
using Machine.Mta.Dispatching;
using Machine.Utility.ThreadPool;

namespace Machine.Mta
{
  public class MessageBusManager : IMessageBusManager, IDisposable
  {
    static readonly ThreadPoolConfiguration _defaultThreadPoolConfiguration = new ThreadPoolConfiguration(1, 1);
    readonly IMachineContainer _container;
    readonly IMessageBusFactory _messageBusFactory;
    readonly List<IMessageBus> _buses = new List<IMessageBus>();

    public IMessageBus DefaultBus
    {
      get { return _buses.First(); }
    }

    public MessageBusManager(IMessageBusFactory messageBusFactory, IMachineContainer container)
    {
      _messageBusFactory = messageBusFactory;
      _container = container;
    }

    public IMessageBus AddSendOnlyMessageBus()
    {
      throw new NotImplementedException();
    }

    public IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress)
    {
      return AddMessageBus(address, poisonAddress, new AllHandlersInContainer(_container), _defaultThreadPoolConfiguration);
    }

    public IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, ThreadPoolConfiguration threadPoolConfiguration)
    {
      return AddMessageBus(address, poisonAddress, new AllHandlersInContainer(_container), threadPoolConfiguration);
    }
    
    IMessageBus AddMessageBus(EndpointAddress address, EndpointAddress poisonAddress, IProvideHandlerTypes handlerTypes, ThreadPoolConfiguration threadPoolConfiguration)
    {
      IMessageBus bus = _messageBusFactory.CreateMessageBus(address, poisonAddress, handlerTypes, threadPoolConfiguration);
      _buses.Add(bus);
      return bus;
    }

    public void EachBus(Action<IMessageBus> action)
    {
      _buses.Each(action);
    }

    public void Dispose()
    {
      EachBus(b => b.Dispose());
    }
  }
}
