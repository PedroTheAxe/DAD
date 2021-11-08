// <auto-generated>
//     Generated by the protocol buffer compiler.  DO NOT EDIT!
//     source: protos/DIDAStorage.proto
// </auto-generated>
#pragma warning disable 0414, 1591
#region Designer generated code

using grpc = global::Grpc.Core;

/// <summary>
/// this service specifies how to access the storage 
/// </summary>
public static partial class DIDAStorageService
{
  static readonly string __ServiceName = "DIDAStorageService";

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
  static readonly grpc::Marshaller<global::DIDAReadRequest> __Marshaller_DIDAReadRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAReadRequest.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDARecordReply> __Marshaller_DIDARecordReply = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDARecordReply.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAWriteRequest> __Marshaller_DIDAWriteRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAWriteRequest.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAVersion> __Marshaller_DIDAVersion = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAVersion.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAUpdateIfRequest> __Marshaller_DIDAUpdateIfRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAUpdateIfRequest.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAListServerRequest> __Marshaller_DIDAListServerRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAListServerRequest.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAListServerReply> __Marshaller_DIDAListServerReply = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAListServerReply.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAUpdateServerIdRequest> __Marshaller_DIDAUpdateServerIdRequest = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAUpdateServerIdRequest.Parser));
  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Marshaller<global::DIDAUpdateServerIdReply> __Marshaller_DIDAUpdateServerIdReply = grpc::Marshallers.Create(__Helper_SerializeMessage, context => __Helper_DeserializeMessage(context, global::DIDAUpdateServerIdReply.Parser));

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Method<global::DIDAReadRequest, global::DIDARecordReply> __Method_read = new grpc::Method<global::DIDAReadRequest, global::DIDARecordReply>(
      grpc::MethodType.Unary,
      __ServiceName,
      "read",
      __Marshaller_DIDAReadRequest,
      __Marshaller_DIDARecordReply);

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Method<global::DIDAWriteRequest, global::DIDAVersion> __Method_write = new grpc::Method<global::DIDAWriteRequest, global::DIDAVersion>(
      grpc::MethodType.Unary,
      __ServiceName,
      "write",
      __Marshaller_DIDAWriteRequest,
      __Marshaller_DIDAVersion);

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Method<global::DIDAUpdateIfRequest, global::DIDAVersion> __Method_updateIfValueIs = new grpc::Method<global::DIDAUpdateIfRequest, global::DIDAVersion>(
      grpc::MethodType.Unary,
      __ServiceName,
      "updateIfValueIs",
      __Marshaller_DIDAUpdateIfRequest,
      __Marshaller_DIDAVersion);

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Method<global::DIDAListServerRequest, global::DIDAListServerReply> __Method_listServer = new grpc::Method<global::DIDAListServerRequest, global::DIDAListServerReply>(
      grpc::MethodType.Unary,
      __ServiceName,
      "listServer",
      __Marshaller_DIDAListServerRequest,
      __Marshaller_DIDAListServerReply);

  [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
  static readonly grpc::Method<global::DIDAUpdateServerIdRequest, global::DIDAUpdateServerIdReply> __Method_updateServerId = new grpc::Method<global::DIDAUpdateServerIdRequest, global::DIDAUpdateServerIdReply>(
      grpc::MethodType.Unary,
      __ServiceName,
      "updateServerId",
      __Marshaller_DIDAUpdateServerIdRequest,
      __Marshaller_DIDAUpdateServerIdReply);

  /// <summary>Service descriptor</summary>
  public static global::Google.Protobuf.Reflection.ServiceDescriptor Descriptor
  {
    get { return global::DIDAStorageReflection.Descriptor.Services[0]; }
  }

  /// <summary>Client for DIDAStorageService</summary>
  public partial class DIDAStorageServiceClient : grpc::ClientBase<DIDAStorageServiceClient>
  {
    /// <summary>Creates a new client for DIDAStorageService</summary>
    /// <param name="channel">The channel to use to make remote calls.</param>
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public DIDAStorageServiceClient(grpc::ChannelBase channel) : base(channel)
    {
    }
    /// <summary>Creates a new client for DIDAStorageService that uses a custom <c>CallInvoker</c>.</summary>
    /// <param name="callInvoker">The callInvoker to use to make remote calls.</param>
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public DIDAStorageServiceClient(grpc::CallInvoker callInvoker) : base(callInvoker)
    {
    }
    /// <summary>Protected parameterless constructor to allow creation of test doubles.</summary>
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    protected DIDAStorageServiceClient() : base()
    {
    }
    /// <summary>Protected constructor to allow creation of configured clients.</summary>
    /// <param name="configuration">The client configuration.</param>
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    protected DIDAStorageServiceClient(ClientBaseConfiguration configuration) : base(configuration)
    {
    }

    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::DIDARecordReply read(global::DIDAReadRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
    {
      return read(request, new grpc::CallOptions(headers, deadline, cancellationToken));
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::DIDARecordReply read(global::DIDAReadRequest request, grpc::CallOptions options)
    {
      return CallInvoker.BlockingUnaryCall(__Method_read, null, options, request);
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual grpc::AsyncUnaryCall<global::DIDARecordReply> readAsync(global::DIDAReadRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
    {
      return readAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual grpc::AsyncUnaryCall<global::DIDARecordReply> readAsync(global::DIDAReadRequest request, grpc::CallOptions options)
    {
      return CallInvoker.AsyncUnaryCall(__Method_read, null, options, request);
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::DIDAVersion write(global::DIDAWriteRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
    {
      return write(request, new grpc::CallOptions(headers, deadline, cancellationToken));
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::DIDAVersion write(global::DIDAWriteRequest request, grpc::CallOptions options)
    {
      return CallInvoker.BlockingUnaryCall(__Method_write, null, options, request);
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual grpc::AsyncUnaryCall<global::DIDAVersion> writeAsync(global::DIDAWriteRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
    {
      return writeAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual grpc::AsyncUnaryCall<global::DIDAVersion> writeAsync(global::DIDAWriteRequest request, grpc::CallOptions options)
    {
      return CallInvoker.AsyncUnaryCall(__Method_write, null, options, request);
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::DIDAVersion updateIfValueIs(global::DIDAUpdateIfRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
    {
      return updateIfValueIs(request, new grpc::CallOptions(headers, deadline, cancellationToken));
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::DIDAVersion updateIfValueIs(global::DIDAUpdateIfRequest request, grpc::CallOptions options)
    {
      return CallInvoker.BlockingUnaryCall(__Method_updateIfValueIs, null, options, request);
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual grpc::AsyncUnaryCall<global::DIDAVersion> updateIfValueIsAsync(global::DIDAUpdateIfRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
    {
      return updateIfValueIsAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual grpc::AsyncUnaryCall<global::DIDAVersion> updateIfValueIsAsync(global::DIDAUpdateIfRequest request, grpc::CallOptions options)
    {
      return CallInvoker.AsyncUnaryCall(__Method_updateIfValueIs, null, options, request);
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::DIDAListServerReply listServer(global::DIDAListServerRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
    {
      return listServer(request, new grpc::CallOptions(headers, deadline, cancellationToken));
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::DIDAListServerReply listServer(global::DIDAListServerRequest request, grpc::CallOptions options)
    {
      return CallInvoker.BlockingUnaryCall(__Method_listServer, null, options, request);
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual grpc::AsyncUnaryCall<global::DIDAListServerReply> listServerAsync(global::DIDAListServerRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
    {
      return listServerAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual grpc::AsyncUnaryCall<global::DIDAListServerReply> listServerAsync(global::DIDAListServerRequest request, grpc::CallOptions options)
    {
      return CallInvoker.AsyncUnaryCall(__Method_listServer, null, options, request);
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::DIDAUpdateServerIdReply updateServerId(global::DIDAUpdateServerIdRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
    {
      return updateServerId(request, new grpc::CallOptions(headers, deadline, cancellationToken));
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual global::DIDAUpdateServerIdReply updateServerId(global::DIDAUpdateServerIdRequest request, grpc::CallOptions options)
    {
      return CallInvoker.BlockingUnaryCall(__Method_updateServerId, null, options, request);
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual grpc::AsyncUnaryCall<global::DIDAUpdateServerIdReply> updateServerIdAsync(global::DIDAUpdateServerIdRequest request, grpc::Metadata headers = null, global::System.DateTime? deadline = null, global::System.Threading.CancellationToken cancellationToken = default(global::System.Threading.CancellationToken))
    {
      return updateServerIdAsync(request, new grpc::CallOptions(headers, deadline, cancellationToken));
    }
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    public virtual grpc::AsyncUnaryCall<global::DIDAUpdateServerIdReply> updateServerIdAsync(global::DIDAUpdateServerIdRequest request, grpc::CallOptions options)
    {
      return CallInvoker.AsyncUnaryCall(__Method_updateServerId, null, options, request);
    }
    /// <summary>Creates a new instance of client from given <c>ClientBaseConfiguration</c>.</summary>
    [global::System.CodeDom.Compiler.GeneratedCode("grpc_csharp_plugin", null)]
    protected override DIDAStorageServiceClient NewInstance(ClientBaseConfiguration configuration)
    {
      return new DIDAStorageServiceClient(configuration);
    }
  }

}
#endregion
