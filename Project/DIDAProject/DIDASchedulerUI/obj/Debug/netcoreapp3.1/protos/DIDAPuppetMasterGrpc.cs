// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: protos/DIDAPuppetMaster.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

public static partial class DIDAPuppetMasterService
{
  static readonly string __ServiceName = "DIDAPuppetMasterService";

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static void __Helper_SerializeMessage(global::Google.Protobuf.IMessage message, grpc::SerializationContext context)
  {
    #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
    if (message is global::Google.Protobuf.IBufferMessage)
    {
      context.SetPayloadLength(message.CalculateSize());
      global::Google.Protobuf.MessageExtensions.WriteTo(message, context.GetBufferWriter());
      context.Complete();
      return;
    }
    #endif
    context.Complete(global::Google.Protobuf.MessageExtensions.ToByteArray(message));
  }

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static class __Helper_MessageCache<T>
  {
    public static readonly bool IsBufferMessage = global::System.Reflection.IntrospectionExtensions.GetTypeInfo(typeof(global::Google.Protobuf.IBufferMessage)).IsAssignableFrom(typeof(T));
  }

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static T __Helper_DeserializeMessage<T>(grpc::DeserializationContext context, global::Google.Protobuf.MessageParser<T> parser) where T : global::Google.Protobuf.IMessage<T>
  {
    #if !GRPC_DISABLE_PROTOBUF_BUFFER_SERIALIZATION
    if (__Helper_MessageCache<T>.IsBufferMessage)
    {
      return parser.ParseFrom(context.PayloadAsReadOnlySequence());
    }
    #endif
    return parser.ParseFrom(context.PayloadAsNewBuffer());
  }

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAFileSendRequest> __Marshaller_DIDAFileSendRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAFileSendRequest.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAFileSendReply> __Marshaller_DIDAFileSendReply = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAFileSendReply.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAPostInitRequest> __Marshaller_DIDAPostInitRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAPostInitRequest.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAPostInitReply> __Marshaller_DIDAPostInitReply = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAPostInitReply.Parser));

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Method<global::DIDAFileSendRequest, global::DIDAFileSendReply> __Method_sendFile = new grpc::Method<global::DIDAFileSendRequest, global::DIDAFileSendReply>(
      grpc::MethodType.Unary,
      __ServiceName,
      "sendFile",
      __Marshaller_DIDAFileSendRequest,
      __Marshaller_DIDAFileSendReply);

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Method<global::DIDAPostInitRequest, global::DIDAPostInitReply> __Method_sendPostInit = new grpc::Method<global::DIDAPostInitRequest, global::DIDAPostInitReply>(
      grpc::MethodType.Unary,
      __ServiceName,
      "sendPostInit",
      __Marshaller_DIDAPostInitRequest,
      __Marshaller_DIDAPostInitReply);

  /// <summary>Service descriptor</summary>
  public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
  {
    get { return global::DIDAPuppetMasterReflection.Descriptor.Services[0]; }
  }

  /// <summary>Base class for server-side implementations of DIDAPuppetMasterService</summary>
  [grpc::BindServiceMethod(typeof(DIDAPuppetMasterService), "BindService")]
  public abstract partial class DIDAPuppetMasterServiceBase
  {
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::System.Threading.Tasks.Task<global::DIDAFileSendReply> sendFile(global::DIDAFileSendRequest request, grpc::ServerCallContext context)
    {
      throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::System.Threading.Tasks.Task<global::DIDAPostInitReply> sendPostInit(global::DIDAPostInitRequest request, grpc::ServerCallContext context)
    {
      throw new grpc::RpcException(new grpc::Status(grpc::StatusCode.Unimplemented, ""));
    }

  }

  /// <summary>Creates service definition that can be registered with a server</summary>
  /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  public static grpc::ServerServiceDefinition BindService(DIDAPuppetMasterServiceBase serviceImpl)
  {
    return grpc::ServerServiceDefinition.CreateBuilder()
        .AddMethod(__Method_sendFile, serviceImpl.sendFile)
        .AddMethod(__Method_sendPostInit, serviceImpl.sendPostInit).Build();
  }

  /// <summary>Register service method with a service binder with or without implementation. Useful when customizing the  service binding logic.
  /// Note: this method is part of an experimental API that can change or be removed without any prior notice.</summary>
  /// <param name="serviceBinder">Service methods will be bound by calling <c>AddMethod</c> on this object.</param>
  /// <param name="serviceImpl">An object implementing the server-side handling logic.</param>
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  public static void BindService(grpc::ServiceBinderBase serviceBinder, DIDAPuppetMasterServiceBase serviceImpl)
  {
    serviceBinder.AddMethod(__Method_sendFile, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::DIDAFileSendRequest, global::DIDAFileSendReply>(serviceImpl.sendFile));
    serviceBinder.AddMethod(__Method_sendPostInit, serviceImpl == null ? null : new grpc::UnaryServerMethod<global::DIDAPostInitRequest, global::DIDAPostInitReply>(serviceImpl.sendPostInit));
  }

}
#endregion