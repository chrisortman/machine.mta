using System;
using System.Collections.Generic;

using MassTransit.ServiceBus;

namespace Machine.Mta.Minimalistic
{
  public class Invoker<T> : IInvoker where T : class, IMessage
  {
    #region IInvoker Members
    public void Dispatch(IMessage message, object handler)
    {
      Consumes<T>.All genericHandler = (Consumes<T>.All)handler;
      genericHandler.Consume((T)message);
    }
    #endregion
  }
  
  public interface IInvoker
  {
    void Dispatch(IMessage message, object handler);
  }
  
  public static class Invokers
  {
    public static IInvoker CreateFor(Type messageType)
    {
      return (IInvoker)Activator.CreateInstance(typeof(Invoker<>).MakeGenericType(messageType));
    }
  }
}