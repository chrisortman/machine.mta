using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

using Machine.Core.Utility;

namespace Machine.Mta.MessageInterfaces
{
  public class MessageInterfaceImplementations : IMessageInterfaceImplementationsLookup
  {
    readonly Dictionary<Type, Type> _interfaceToClass = new Dictionary<Type, Type>();
    readonly Dictionary<Type, Type> _classToInterface = new Dictionary<Type, Type>();
    readonly IMessageInterfaceImplementationFactory _messageInterfaceImplementationFactory;
    readonly ReaderWriterLock _lock = new ReaderWriterLock();
    bool _generated;

    public MessageInterfaceImplementations(IMessageInterfaceImplementationFactory messageInterfaceImplementationFactory)
    {
      _messageInterfaceImplementationFactory = messageInterfaceImplementationFactory;
    }

    public Type GetClassFor(Type type)
    {
      using (RWLock.AsReader(_lock))
      {
        GenerateIfNecessary();
        if (!_interfaceToClass.ContainsKey(type))
          throw new KeyNotFoundException(type.FullName);
        return _interfaceToClass[type];
      }
    }

    public Type GetInterfaceFor(Type type)
    {
      using (RWLock.AsReader(_lock))
      {
        GenerateIfNecessary();
        if (!_classToInterface.ContainsKey(type))
          throw new KeyNotFoundException(type.FullName);
        return _classToInterface[type];
      }
    }

    public Type GetClassOrInterfaceFor(Type type)
    {
      using (RWLock.AsReader(_lock))
      {
        GenerateIfNecessary();
        if (_interfaceToClass.ContainsKey(type))
          return _interfaceToClass[type];
        if (_classToInterface.ContainsKey(type))
          return _classToInterface[type];
        return null;
      }
    }

    public bool IsClassOrInterface(Type type)
    {
      using (RWLock.AsReader(_lock))
      {
        GenerateIfNecessary();
        return _interfaceToClass.ContainsKey(type) || _classToInterface.ContainsKey(type);
      }
    }

    static void GenerateIfNecessary()
    {
    }

    public void Initialize(IEnumerable<Type> messageTypes)
    {
      if (RWLock.UpgradeToWriterIf(_lock, () => !_generated))
      {
        foreach (var generated in _messageInterfaceImplementationFactory.ImplementMessageInterfaces(messageTypes.Where(type => type.IsInterface)))
        {
          _interfaceToClass[generated.Key] = generated.Value;
          _classToInterface[generated.Value] = generated.Key;
        }
        _generated = true;
      }
    }
  }
}