using System;
using System.Collections.Generic;
using System.Messaging;

using Machine.Mta.Endpoints;

namespace Machine.Mta.Transports.Msmq
{
  public class MsmqEndpointFactory : IEndpointFactory
  {
    readonly MsmqTransactionManager _transactionManager;

    public MsmqEndpointFactory(MsmqTransactionManager transactionManager)
    {
      _transactionManager = transactionManager;
    }

    public IEndpoint CreateEndpoint(EndpointName name)
    {
      MessageQueue queue = new MessageQueue(name.ToPath(), QueueAccessMode.SendAndReceive);
      return new MsmqEndpoint(name, queue, _transactionManager);
    }
  }
  public static class EndpointNameHelpers
  {
    public static string ToPath(this EndpointName name)
    {
      return String.Format(@"FormatName:DIRECT=OS:{0}\Private$\{1}", name.Address, name.Name);
    }
  }
}
