using System;
using System.Collections.Generic;
using System.Transactions;
using RabbitMQ.Client;

namespace NServiceBus.Unicast.Transport.RabbitMQ
{
  public class RabbitMqEnlistment : IEnlistmentNotification
  {
    readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(RabbitMqEnlistment));
    readonly OpenedSession _openedSession;

    public RabbitMqEnlistment(OpenedSession openedSession)
    {
      _openedSession = openedSession;
    }

    public void Prepare(PreparingEnlistment preparingEnlistment)
    {
      _log.Info("Prepared");
      preparingEnlistment.Prepared();
    }

    public void Commit(Enlistment enlistment)
    {
      _log.Info("Commit");
      _openedSession.Dispose();
      enlistment.Done();
    }

    public void Rollback(Enlistment enlistment)
    {
      _log.Info("Rollback");
      _openedSession.Dispose();
      enlistment.Done();
    }

    public void InDoubt(Enlistment enlistment)
    {
      _log.Info("Doubt");
    }
  }

  public class ConnectionProvider
  {
    readonly log4net.ILog _log = log4net.LogManager.GetLogger(typeof(ConnectionProvider));
    readonly ConnectionFactory _connectionFactory;
    [ThreadStatic]
    static Dictionary<string, OpenedSession> _state;

    public ConnectionProvider()
    {
      _connectionFactory = new ConnectionFactory();
      _connectionFactory.Parameters.UserName = ConnectionParameters.DefaultUser;
      _connectionFactory.Parameters.Password = ConnectionParameters.DefaultPass;
      _connectionFactory.Parameters.VirtualHost = ConnectionParameters.DefaultVHost;
    }

    public OpenedSession Open(string brokerAddress, bool transactional)
    {
      if (!transactional)
      {
        return OpenNew(brokerAddress);
      }
      if (_state == null)
      {
        _state = new Dictionary<string, OpenedSession>();
      }
      if (_state.ContainsKey(brokerAddress))
      {
        return _state[brokerAddress].AddRef();
      }
      _log.Debug("Opening " + brokerAddress);
      var opened = _state[brokerAddress] = OpenNew(brokerAddress);
      opened.Disposed += (sender, e) =>
      {
        _log.Debug("Closing " + brokerAddress);
        _state.Remove(brokerAddress);
      };
      if (Transaction.Current != null)
      {
        Transaction.Current.EnlistVolatile(new RabbitMqEnlistment(opened), EnlistmentOptions.None);
        opened.AddRef();
      }
      return opened.AddRef();
    }

    OpenedSession OpenNew(string brokerAddress)
    {
      var connection = _connectionFactory.CreateConnection(brokerAddress);
      var model = connection.CreateModel();
      return new OpenedSession(connection, model);
    }
  }

  public class OpenedSession : IDisposable
  {
    readonly IConnection _connection;
    readonly IModel _model;
    Int32 _refs;
    public event EventHandler Disposed;

    public OpenedSession(IConnection connection, IModel model)
    {
      _connection = connection;
      _model = model;
    }

    public OpenedSession AddRef()
    {
      _refs++;
      return this;
    }

    public void Dispose()
    {
      if (--_refs == 0)
      {
        _model.Dispose();
        _connection.Dispose();
        Disposed(this, EventArgs.Empty);
      }
    }

    public IModel Model()
    {
      return _model;
    }
  }
}
