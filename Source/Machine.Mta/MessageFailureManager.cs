using System;
using System.Collections.Generic;

namespace Machine.Mta
{
  public class MessageFailureManager : IMessageFailureManager
  {
    readonly Dictionary<Guid, List<Exception>> _errors = new Dictionary<Guid, List<Exception>>();
    readonly object _lock = new object();

    public virtual void RecordFailure(EndpointAddress address, TransportMessage transportMessage, Exception error)
    {
      lock (_lock)
      {
        Guid id = transportMessage.Id;
        if (!_errors.ContainsKey(id))
        {
          _errors[id] = new List<Exception>();
        }
        _errors[id].Add(error);
      }
      if (Failure != null)
      {
        Failure(address, transportMessage, error);
      }
    }

    public virtual bool IsPoison(TransportMessage transportMessage)
    {
      lock (_lock)
      {
        Guid id = transportMessage.Id;
        bool hasErrors = _errors.ContainsKey(id);
        if (hasErrors)
        {
          _errors.Remove(id);
        }
        return hasErrors;
      }
    }

    public static Action<EndpointAddress, TransportMessage, Exception> Failure;
  }

  public interface IMessageFailureManager
  {
    void RecordFailure(EndpointAddress address, TransportMessage transportMessage, Exception error);
    bool IsPoison(TransportMessage transportMessage);
  }
}