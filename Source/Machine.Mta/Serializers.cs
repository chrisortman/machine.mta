using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

using Newtonsoft.Json;

using Machine.Mta.InterfacesAsMessages;

namespace Machine.Mta
{
  public class Serializers
  {
    static BinaryFormatter _binaryFormatter;
    static JsonSerializer _jsonSerializer;

    static Serializers()
    { 
      _binaryFormatter = new BinaryFormatter();
      _binaryFormatter.Binder = new EndpointNameSerializationBinder(new MessageInterfaceSerializationBinder());
      _jsonSerializer = new JsonSerializer();
      _jsonSerializer.Converters.Add(new EndpointAddressJsonConverter());
      _jsonSerializer.Converters.Add(new ExceptionJsonConverter());
    }

    public static BinaryFormatter Binary
    {
      get { return _binaryFormatter; }
      set { _binaryFormatter = value; }
    }

    public static JsonSerializer Json
    {
      get { return _jsonSerializer; }
      set { _jsonSerializer = value; }
    }

    public static Func<JsonSerializer> NewJson = () =>
    {
      JsonSerializer newSerializer = new JsonSerializer();
      newSerializer.DefaultValueHandling = _jsonSerializer.DefaultValueHandling;
      newSerializer.MissingMemberHandling = _jsonSerializer.MissingMemberHandling;
      newSerializer.NullValueHandling = _jsonSerializer.NullValueHandling;
      newSerializer.ObjectCreationHandling = _jsonSerializer.ObjectCreationHandling;
      newSerializer.ReferenceLoopHandling = _jsonSerializer.ReferenceLoopHandling;
      foreach (JsonConverter converter in _jsonSerializer.Converters)
      {
        newSerializer.Converters.Add(converter);
      }
      return newSerializer;
    };
  }
  public class EndpointNameSerializationBinder : SerializationBinder
  {
    readonly SerializationBinder _binder;

    public EndpointNameSerializationBinder(SerializationBinder binder)
    {
      _binder = binder;
    }

    public override Type BindToType(string assemblyName, string typeName)
    {
      if (typeName == "Machine.Mta.EndpointName")
      {
        return typeof(EndpointAddress);
      }
      return _binder.BindToType(assemblyName, typeName);
    }
  }
}
