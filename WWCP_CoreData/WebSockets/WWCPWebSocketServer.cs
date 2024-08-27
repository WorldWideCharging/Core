﻿/*
 * Copyright (c) 2014-2024 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP WWCP <https://github.com/OpenChargingCloud/WWCP_WWCP>
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System.Reflection;
using System.Collections.Concurrent;
using System.Security.Authentication;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;
//using org.GraphDefined.Vanaheimr.Hermod.WebSocket;
using org.GraphDefined.Vanaheimr.Hermod.Sockets;

using cloud.charging.open.protocols.WWCP.NetworkingNode;
using cloud.charging.open.protocols.WWCP;

#endregion

namespace cloud.charging.open.protocols.WWCP.WebSockets
{

    //public delegate Task ProcessJSONMessage(DateTime                                                          MessageTimestamp,
    //                                        org.GraphDefined.Vanaheimr.Hermod.WebSocket.IWebSocketConnection  WebSocketConnection,
    //                                        NetworkingNode_Id?                                                sourceNodeId,
    //                                        JArray                                                            JSONMessage,
    //                                        EventTracking_Id                                                  EventTrackingId,
    //                                        CancellationToken                                                 CancellationToken);


    /// <summary>
    /// The WWCP HTTP Web Socket server.
    /// </summary>
    public partial class WWCPWebSocketServer : org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServer,
                                               IWWCPWebSocketServer
    {

        #region Data

        /// <summary>
        /// The default HTTP server name.
        /// </summary>
        public  const           String                                                                               DefaultHTTPServiceName            = $"GraphDefined WWCP Web Socket Server";

        /// <summary>
        /// The default HTTP server TCP port.
        /// </summary>
        public static readonly  IPPort                                                                               DefaultHTTPServerPort             = IPPort.Parse(2010);

        /// <summary>
        /// The default HTTP server URI prefix.
        /// </summary>
        public static readonly  HTTPPath                                                                             DefaultURLPrefix                  = HTTPPath.Parse("/");

        protected readonly      Dictionary<String, MethodInfo>                                                       incomingMessageProcessorsLookup   = [];
        protected readonly      ConcurrentDictionary<NetworkingNode_Id, Tuple<org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection, DateTime>>  connectedNetworkingNodes          = [];
        protected readonly      ConcurrentDictionary<NetworkingNode_Id, NetworkingNode_Id>                           reachableViaNetworkingHubs        = [];
        //protected readonly      ConcurrentDictionary<Request_Id, SendRequestState>                                   requests                          = [];

        public const            String                                                                               LogfileName                       = "CSMSWSServer.log";

        #endregion

        #region Properties

        ///// <summary>
        ///// The parent WWCP adapter.
        ///// </summary>
        //public WWCPAdapter                                       WWCPAdapter              { get; }

        /// <summary>
        /// The enumeration of all connected networking nodes.
        /// </summary>
        public IEnumerable<NetworkingNode_Id>                    NetworkingNodeIds
            => connectedNetworkingNodes.Keys;

        ///// <summary>
        ///// Require a HTTP Basic Authentication of all networking nodes.
        ///// </summary>
        //public Boolean                                           RequireAuthentication    { get; }

        ///// <summary>
        ///// The JSON formatting to use.
        ///// </summary>
        //public Formatting                                        JSONFormatting           { get; set; }
        //    = Formatting.None;

        /// <summary>
        /// The request timeout for messages sent by this HTTP WebSocket server.
        /// </summary>
        public TimeSpan?                                         RequestTimeout           { get; set; }

        public INetworkingNode NetworkingNode { get; }

        #endregion

        #region Events

        #region Common Connection Management

        /// <summary>
        /// An event sent whenever the HTTP connection switched successfully to web socket.
        /// </summary>
        public event OnNetworkingNodeNewWebSocketConnectionDelegate?  OnNetworkingNodeNewWebSocketConnection;

        /// <summary>
        /// An event sent whenever a web socket close frame was received.
        /// </summary>
        public event OnNetworkingNodeCloseMessageReceivedDelegate?    OnNetworkingNodeCloseMessageReceived;

        /// <summary>
        /// An event sent whenever a TCP connection was closed.
        /// </summary>
        public event OnNetworkingNodeTCPConnectionClosedDelegate?     OnNetworkingNodeTCPConnectionClosed;

        #endregion


        /// <summary>
        /// An event sent whenever a JSON message was sent.
        /// </summary>
        public event     OnWebSocketServerJSONMessageSentDelegate?         OnJSONMessageSent;

        /// <summary>
        /// An event sent whenever a JSON message was received.
        /// </summary>
        public event     OnWebSocketServerJSONMessageReceivedDelegate?     OnJSONMessageReceived;


        /// <summary>
        /// An event sent whenever a binary message was sent.
        /// </summary>
        public new event OnWebSocketServerBinaryMessageSentDelegate?       OnBinaryMessageSent;

        /// <summary>
        /// An event sent whenever a binary message was received.
        /// </summary>
        public new event OnWebSocketServerBinaryMessageReceivedDelegate?   OnBinaryMessageReceived;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new WWCP HTTP Web Socket server.
        /// </summary>
        /// <param name="HTTPServiceName">An optional identification string for the HTTP service.</param>
        /// <param name="IPAddress">An IP address to listen on.</param>
        /// <param name="TCPPort">An optional TCP port for the HTTP server.</param>
        /// <param name="Description">An optional description of this HTTP Web Socket service.</param>
        /// 
        /// <param name="RequireAuthentication">Require a HTTP Basic Authentication of all charging boxes.</param>
        /// 
        /// <param name="DNSClient">An optional DNS client to use.</param>
        /// <param name="AutoStart">Start the server immediately.</param>
        public WWCPWebSocketServer(INetworkingNode                                                 NetworkingNode,

                                   String?                                                         HTTPServiceName              = DefaultHTTPServiceName,
                                   IIPAddress?                                                     IPAddress                    = null,
                                   IPPort?                                                         TCPPort                      = null,
                                   I18NString?                                                     Description                  = null,

                                   Boolean?                                                        RequireAuthentication        = true,
                                   IEnumerable<String>?                                            SecWebSocketProtocols        = null,
                                   Boolean                                                         DisableWebSocketPings        = false,
                                   TimeSpan?                                                       WebSocketPingEvery           = null,
                                   TimeSpan?                                                       SlowNetworkSimulationDelay   = null,

                                   Func<X509Certificate2>?                                         ServerCertificateSelector    = null,
                                   RemoteTLSClientCertificateValidationHandler<org.GraphDefined.Vanaheimr.Hermod.WebSocket.IWebSocketServer>?  ClientCertificateValidator   = null,
                                   LocalCertificateSelectionHandler?                               LocalCertificateSelector     = null,
                                   SslProtocols?                                                   AllowedTLSProtocols          = null,
                                   Boolean?                                                        ClientCertificateRequired    = null,
                                   Boolean?                                                        CheckCertificateRevocation   = null,

                                   ServerThreadNameCreatorDelegate?                                ServerThreadNameCreator      = null,
                                   ServerThreadPriorityDelegate?                                   ServerThreadPrioritySetter   = null,
                                   Boolean?                                                        ServerThreadIsBackground     = null,
                                   ConnectionIdBuilder?                                            ConnectionIdBuilder          = null,
                                   TimeSpan?                                                       ConnectionTimeout            = null,
                                   UInt32?                                                         MaxClientConnections         = null,

                                   DNSClient?                                                      DNSClient                    = null,
                                   Boolean                                                         AutoStart                    = true)

            : base(IPAddress,
                   TCPPort         ?? IPPort.Auto,
                   HTTPServiceName ?? DefaultHTTPServiceName,
                   Description,

                   RequireAuthentication,
                   SecWebSocketProtocols,
                   DisableWebSocketPings,
                   WebSocketPingEvery,
                   SlowNetworkSimulationDelay,

                   ServerCertificateSelector,
                   ClientCertificateValidator,
                   LocalCertificateSelector,
                   AllowedTLSProtocols,
                   ClientCertificateRequired,
                   CheckCertificateRevocation,

                   ServerThreadNameCreator,
                   ServerThreadPrioritySetter,
                   ServerThreadIsBackground,
                   ConnectionIdBuilder,
                   ConnectionTimeout,
                   MaxClientConnections,

                   DNSClient,
                   false)

        {

            this.NetworkingNode                  = NetworkingNode;

            //this.Logger                          = new ChargePointwebsocketClient.CPClientLogger(this,
            //                                                                                LoggingPath,
            //                                                                                LoggingContext,
            //                                                                                LogfileCreator);

            base.OnValidateTCPConnection        += ValidateTCPConnection;
            base.OnValidateWebSocketConnection  += ValidateWebSocketConnection;
            base.OnNewWebSocketConnection       += ProcessNewWebSocketConnection;
            base.OnCloseMessageReceived         += ProcessCloseMessage;

            base.OnTextMessageReceived          += ProcessTextMessage;
            base.OnBinaryMessageReceived        += ProcessBinaryMessage;

            base.OnPingMessageReceived          += (timestamp, server, connection, frame, eventTrackingId, pingMessage, ct) => {
                                                       DebugX.Log($"HTTP Web Socket Server '{connection.RemoteSocket}' Ping received:   '{frame.Payload.ToUTF8String()}'");
                                                       return Task.CompletedTask;
                                                   };

            base.OnPongMessageReceived          += (timestamp, server, connection, frame, eventTrackingId, pingMessage, ct) => {
                                                       DebugX.Log($"HTTP Web Socket Server '{connection.RemoteSocket}' Pong received:   '{frame.Payload.ToUTF8String()}'");
                                                       return Task.CompletedTask;
                                                   };

            base.OnCloseMessageReceived         += (timestamp, server, connection, frame, eventTrackingId, closingStatusCode, closingReason, ct) => {
                                                       DebugX.Log($"HTTP Web Socket Server '{connection.RemoteSocket}' Close received:  '{closingStatusCode}', '{closingReason ?? ""}'");
                                                       return Task.CompletedTask;
                                                   };

            if (AutoStart)
                Start();

        }

        #endregion


        #region AddOrUpdateHTTPBasicAuth (NetworkingNodeId, Password)

        /// <summary>
        /// Add the given HTTP Basic Authentication password for the given networking node.
        /// </summary>
        /// <param name="NetworkingNodeId">The unique identification of the networking node.</param>
        /// <param name="Password">The password of the charging station.</param>
        public HTTPBasicAuthentication AddOrUpdateHTTPBasicAuth(NetworkingNode_Id  NetworkingNodeId,
                                                                String             Password)

            => AddOrUpdateHTTPBasicAuth(
                   NetworkingNodeId.ToString(),
                   Password
               );

        #endregion

        #region RemoveHTTPBasicAuth      (NetworkingNodeId)

        /// <summary>
        /// Remove the given HTTP Basic Authentication for the given networking node.
        /// </summary>
        /// <param name="NetworkingNodeId">The unique identification of the networking node.</param>
        public Boolean RemoveHTTPBasicAuth(NetworkingNode_Id NetworkingNodeId)

            => RemoveHTTPBasicAuth(
                   NetworkingNodeId.ToString()
               );

        #endregion


        // Connection management...

        #region (protected) ValidateTCPConnection         (LogTimestamp, Server, Connection, EventTrackingId, CancellationToken)

        private Task<ConnectionFilterResponse> ValidateTCPConnection(DateTime                      LogTimestamp,
                                                                     org.GraphDefined.Vanaheimr.Hermod.WebSocket.IWebSocketServer              Server,
                                                                     System.Net.Sockets.TcpClient  Connection,
                                                                     EventTracking_Id              EventTrackingId,
                                                                     CancellationToken             CancellationToken)
        {

            return Task.FromResult(ConnectionFilterResponse.Accepted());

        }

        #endregion

        #region (protected) ValidateWebSocketConnection   (LogTimestamp, Server, Connection, EventTrackingId, CancellationToken)

        private Task<HTTPResponse?> ValidateWebSocketConnection(DateTime                   LogTimestamp,
                                                                org.GraphDefined.Vanaheimr.Hermod.WebSocket.IWebSocketServer           Server,
                                                                org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection  Connection,
                                                                EventTracking_Id           EventTrackingId,
                                                                CancellationToken          CancellationToken)
        {

            #region Verify 'Sec-WebSocket-Protocol'...

            if (Connection.HTTPRequest?.SecWebSocketProtocol is null ||
                Connection.HTTPRequest?.SecWebSocketProtocol.Any() == false)
            {

                DebugX.Log($"{nameof(WWCPWebSocketServer)} connection from {Connection.RemoteSocket}: Missing 'Sec-WebSocket-Protocol' HTTP header!");

                return Task.FromResult<HTTPResponse?>(
                           new HTTPResponse.Builder() {
                               HTTPStatusCode  = HTTPStatusCode.BadRequest,
                               Server          = HTTPServiceName,
                               Date            = Timestamp.Now,
                               ContentType     = HTTPContentType.Application.JSON_UTF8,
                               Content         = JSONObject.Create(
                                                     new JProperty("description",
                                                     JSONObject.Create(
                                                         new JProperty("en", "Missing 'Sec-WebSocket-Protocol' HTTP header!")
                                                     ))).ToUTF8Bytes(),
                               Connection      = "close"
                           }.AsImmutable);

            }
            else if (!SecWebSocketProtocols.Overlaps(Connection.HTTPRequest?.SecWebSocketProtocol ?? Array.Empty<String>()))
            {

                var error = $"This WebSocket service only supports {SecWebSocketProtocols.Select(id => $"'{id}'").AggregateWith(", ")}!";

                DebugX.Log($"{nameof(WWCPWebSocketServer)} connection from {Connection.RemoteSocket}: {error}");

                return Task.FromResult<HTTPResponse?>(
                           new HTTPResponse.Builder() {
                               HTTPStatusCode  = HTTPStatusCode.BadRequest,
                               Server          = HTTPServiceName,
                               Date            = Timestamp.Now,
                               ContentType     = HTTPContentType.Application.JSON_UTF8,
                               Content         = JSONObject.Create(
                                                     new JProperty("description",
                                                         JSONObject.Create(
                                                             new JProperty("en", error)
                                                     ))).ToUTF8Bytes(),
                               Connection      = "close"
                           }.AsImmutable);

            }

            #endregion

            #region Verify HTTP Authentication

            if (RequireAuthentication)
            {

                if (Connection.HTTPRequest?.Authorization is HTTPBasicAuthentication basicAuthentication)
                {

                    if (ClientLogins.TryGetValue(basicAuthentication.Username, out var password) &&
                        basicAuthentication.Password == password)
                    {
                        DebugX.Log($"{nameof(WWCPWebSocketServer)} connection from {Connection.RemoteSocket} using authorization: '{basicAuthentication.Username}' / '{basicAuthentication.Password}'");
                        return Task.FromResult<HTTPResponse?>(null);
                    }
                    else
                        DebugX.Log($"{nameof(WWCPWebSocketServer)} connection from {Connection.RemoteSocket} invalid authorization: '{basicAuthentication.Username}' / '{basicAuthentication.Password}'!");

                }
                else
                    DebugX.Log($"{nameof(WWCPWebSocketServer)} connection from {Connection.RemoteSocket} missing authorization!");

                return Task.FromResult<HTTPResponse?>(
                           new HTTPResponse.Builder() {
                               HTTPStatusCode  = HTTPStatusCode.Unauthorized,
                               Server          = HTTPServiceName,
                               Date            = Timestamp.Now,
                               Connection      = "close"
                           }.AsImmutable
                       );

            }

            #endregion

            return Task.FromResult<HTTPResponse?>(null);

        }

        #endregion

        #region (protected) ProcessNewWebSocketConnection (LogTimestamp, Server, Connection, SharedSubprotocols, EventTrackingId, CancellationToken)

        protected async Task ProcessNewWebSocketConnection(DateTime                   LogTimestamp,
                                                           org.GraphDefined.Vanaheimr.Hermod.WebSocket.IWebSocketServer           Server,
                                                           org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection  Connection,
                                                           IEnumerable<String>        SharedSubprotocols,
                                                           EventTracking_Id           EventTrackingId,
                                                           CancellationToken          CancellationToken)
        {

            if (Connection.HTTPRequest is null)
                return;

            NetworkingNode_Id? networkingNodeId = null;

            #region Parse TLS Client Certificate CommonName, or...

            // We already validated and therefore trust this certificate!
            if (Connection.HTTPRequest.ClientCertificate is not null)
            {

                var x509CommonName = Connection.HTTPRequest.ClientCertificate.GetNameInfo(X509NameType.SimpleName, forIssuer: false);

                if (NetworkingNode_Id.TryParse(x509CommonName, out var networkingNodeId1))
                {
                    networkingNodeId = networkingNodeId1;
                }

            }

            #endregion

            #region ...check HTTP Basic Authentication, or...

            else if (Connection.HTTPRequest.Authorization is HTTPBasicAuthentication httpBasicAuthentication &&
                     NetworkingNode_Id.TryParse(httpBasicAuthentication.Username, out var networkingNodeId2))
            {
                networkingNodeId = networkingNodeId2;
            }

            #endregion


            //ToDo: This might be a DOS attack vector!

            #region ...try to get the NetworkingNodeId from the HTTP request path suffix

            else
            {

                var path = Connection.HTTPRequest.Path.ToString();

                if (NetworkingNode_Id.TryParse(path[(path.LastIndexOf('/') + 1)..],
                    out var networkingNodeId3))
                {
                    networkingNodeId = networkingNodeId3;
                }

            }

            #endregion


            if (networkingNodeId.HasValue)
            {

                #region Store the NetworkingNodeId within the HTTP Web Socket connection

                Connection.TryAddCustomData(
                               WebSocketKeys.NetworkingNodeId,
                               networkingNodeId.Value
                           );

                #endregion

                #region Register new NetworkingNode

                if (!connectedNetworkingNodes.TryAdd(networkingNodeId.Value,
                                                     new Tuple<org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection, DateTime>(
                                                         Connection,
                                                         Timestamp.Now
                                                     )))
                {

                    DebugX.Log($"{nameof(WWCPWebSocketServer)} Duplicate networking node '{networkingNodeId.Value}' detected: Trying to close old one!");

                    if (connectedNetworkingNodes.TryRemove(networkingNodeId.Value, out var oldConnection))
                    {
                        try
                        {
                            await oldConnection.Item1.Close(
                                      org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketFrame.ClosingStatusCode.NormalClosure,
                                      "Newer connection detected!",
                                      CancellationToken
                                  );
                        }
                        catch (Exception e)
                        {
                            DebugX.Log($"{nameof(WWCPWebSocketServer)} Closing old HTTP Web Socket connection from {oldConnection.Item1.RemoteSocket} failed: {e.Message}");
                        }
                    }

                    connectedNetworkingNodes.TryAdd(networkingNodeId.Value,
                                                    new Tuple<org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection, DateTime>(
                                                        Connection,
                                                        Timestamp.Now
                                                    ));

                }

                #endregion


                #region Store the Networking Mode within the HTTP Web Socket connection

                if (Connection.HTTPRequest.TryGetHeaderField(WebSocketKeys.X_WWCP_NetworkingMode, out var networkingModeString) &&
                    Enum.TryParse<NetworkingMode>(networkingModeString?.ToString(), out var networkingMode))
                {
                    Connection.TryAddCustomData(
                                   WebSocketKeys.NetworkingMode,
                                   networkingMode
                               );
                }

                #endregion


                #region Send OnNewNetworkingNodeWSConnection event

                await LogEvent(
                          OnNetworkingNodeNewWebSocketConnection,
                          loggingDelegate => loggingDelegate.Invoke(
                              LogTimestamp,
                              this,
                              Connection,
                              networkingNodeId.Value,
                              SharedSubprotocols,
                              EventTrackingId,
                              CancellationToken
                          )
                      );

                #endregion

            }

            #region else: Close connection

            else
            {

                DebugX.Log($"{nameof(WWCPWebSocketServer)} Could not get NetworkingNodeId from HTTP Web Socket connection ({Connection.RemoteSocket}): Closing connection!");

                try
                {
                    await Connection.Close(
                              org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketFrame.ClosingStatusCode.PolicyViolation,
                              "Could not get NetworkingNodeId from HTTP Web Socket connection!",
                              CancellationToken
                          );
                }
                catch (Exception e)
                {
                    DebugX.Log($"{nameof(WWCPWebSocketServer)} Closing HTTP Web Socket connection ({Connection.RemoteSocket}) failed: {e.Message}");
                }

            }

            #endregion

        }

        #endregion

        #region (protected) ProcessCloseMessage           (LogTimestamp, Server, Connection, Frame, EventTrackingId, StatusCode, Reason, CancellationToken)

        protected async Task ProcessCloseMessage(DateTime                          LogTimestamp,
                                                 org.GraphDefined.Vanaheimr.Hermod.WebSocket.IWebSocketServer                  Server,
                                                 org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection         Connection,
                                                 org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketFrame                    Frame,
                                                 EventTracking_Id                  EventTrackingId,
                                                 org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketFrame.ClosingStatusCode  StatusCode,
                                                 String?                           Reason,
                                                 CancellationToken                 CancellationToken)
        {

            if (Connection.TryGetCustomDataAs<NetworkingNode_Id>(WebSocketKeys.NetworkingNodeId, out var networkingNodeId))
            {

                connectedNetworkingNodes.TryRemove(networkingNodeId, out _);

                #region Send OnNetworkingNodeCloseMessageReceived event

                var logger = OnNetworkingNodeCloseMessageReceived;
                if (logger is not null)
                {

                    try
                    {
                        await Task.WhenAll(logger.GetInvocationList().
                                                  OfType<OnNetworkingNodeCloseMessageReceivedDelegate>().
                                                  Select(loggingDelegate => loggingDelegate.Invoke(LogTimestamp,
                                                                                                   this,
                                                                                                   Connection,
                                                                                                   networkingNodeId,
                                                                                                   EventTrackingId,
                                                                                                   StatusCode,
                                                                                                   Reason,
                                                                                                   CancellationToken)).
                                                  ToArray());

                    }
                    catch (Exception e)
                    {
                        await HandleErrors(
                                  nameof(WWCPWebSocketServer),
                                  nameof(OnNetworkingNodeCloseMessageReceived),
                                  e
                              );
                    }

                }

                #endregion

            }

        }

        #endregion


        // Receive data...

        #region (protected) ProcessTextMessage   (RequestTimestamp, Server, WebSocketConnection, Frame, EventTrackingId, TextMessage,   CancellationToken)

        /// <summary>
        /// Process a HTTP Web Socket text message.
        /// </summary>
        /// <param name="RequestTimestamp">The timestamp of the request.</param>
        /// <param name="Server">The HTTP Web Socket server.</param>
        /// <param name="WebSocketConnection">The HTTP Web Socket connection.</param>
        /// <param name="Frame">The HTTP Web Socket frame.</param>
        /// <param name="EventTrackingId">An optional event tracking identification.</param>
        /// <param name="TextMessage">The received text message.</param>
        /// <param name="CancellationToken">The cancellation token.</param>
        public async Task ProcessTextMessage(DateTime                   RequestTimestamp,
                                             org.GraphDefined.Vanaheimr.Hermod.WebSocket.IWebSocketServer           Server,
                                             org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection  WebSocketConnection,
                                             org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketFrame             Frame,
                                             EventTracking_Id           EventTrackingId,
                                             String                     TextMessage,
                                             CancellationToken          CancellationToken)
        {

            try
            {

                var sourceNodeId = WebSocketConnection.TryGetCustomDataAs<NetworkingNode_Id>(WebSocketKeys.NetworkingNodeId);

                #region Initial checks

                TextMessage = TextMessage.Trim();

                if (TextMessage == "[]" ||
                    TextMessage.IsNullOrEmpty())
                {

                    await HandleErrors(
                              nameof(WWCPWebSocketServer),
                              nameof(ProcessTextMessage),
                              $"Received an empty text message from {(
                                   sourceNodeId.HasValue
                                       ? $"'{sourceNodeId}' ({WebSocketConnection.RemoteSocket})"
                                       : $"'{WebSocketConnection.RemoteSocket}"
                              )}'!"
                          );

                    return;

                }

                #endregion


                var jsonMessage = JArray.Parse(TextMessage);

                await LogEvent(
                          OnJSONMessageReceived,
                          loggingDelegate => loggingDelegate.Invoke(
                              Timestamp.Now,
                              this,
                              WebSocketConnection,
                              EventTrackingId,
                              RequestTimestamp,
                              sourceNodeId ?? NetworkingNode_Id.Zero,
                              jsonMessage,
                              CancellationToken
                          )
                      );

                //await WWCPAdapter.IN.ProcessJSONMessage(
                //          RequestTimestamp,
                //          WebSocketConnection,
                //          sourceNodeId,
                //          jsonMessage,
                //          EventTrackingId,
                //          CancellationToken
                //      );

            }
            catch (Exception e)
            {
                await HandleErrors(
                          nameof(WWCPWebSocketServer),
                          nameof(ProcessTextMessage),
                          e
                      );
            }

        }

        #endregion

        #region (protected) ProcessBinaryMessage (RequestTimestamp, Server, WebSocketConnection, Frame, EventTrackingId, BinaryMessage, CancellationToken)

        /// <summary>
        /// Process a HTTP Web Socket binary message.
        /// </summary>
        /// <param name="RequestTimestamp">The timestamp of the request.</param>
        /// <param name="Server">The HTTP Web Socket server.</param>
        /// <param name="WebSocketConnection">The HTTP Web Socket connection.</param>
        /// <param name="Frame">The HTTP Web Socket frame.</param>
        /// <param name="EventTrackingId">An optional event tracking identification.</param>
        /// <param name="BinaryMessage">The received binary message.</param>
        /// <param name="CancellationToken">The cancellation token.</param>
        public async Task ProcessBinaryMessage(DateTime                   RequestTimestamp,
                                               org.GraphDefined.Vanaheimr.Hermod.WebSocket.IWebSocketServer           Server,
                                               org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection  WebSocketConnection,
                                               org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketFrame             Frame,
                                               EventTracking_Id           EventTrackingId,
                                               Byte[]                     BinaryMessage,
                                               CancellationToken          CancellationToken)
        {

            try
            {

                var sourceNodeId = WebSocketConnection.TryGetCustomDataAs<NetworkingNode_Id>(WebSocketKeys.NetworkingNodeId);

                #region Initial checks

                if (BinaryMessage.Length == 0)
                {

                    await HandleErrors(
                              nameof(WWCPWebSocketServer),
                              nameof(ProcessTextMessage),
                              $"Received an empty binary message from {(
                                   sourceNodeId.HasValue
                                       ? $"'{sourceNodeId}' ({WebSocketConnection.RemoteSocket})"
                                       : $"'{WebSocketConnection.RemoteSocket}"
                              )}'!"
                          );

                    return;

                }

                #endregion


                await LogEvent(
                          OnBinaryMessageReceived,
                          loggingDelegate => loggingDelegate.Invoke(
                              Timestamp.Now,
                              this,
                              WebSocketConnection,
                              EventTrackingId,
                              RequestTimestamp,
                              sourceNodeId ?? NetworkingNode_Id.Zero,
                              BinaryMessage,
                              CancellationToken
                          )
                      );

                //await WWCPAdapter.IN.ProcessBinaryMessage(
                //          RequestTimestamp,
                //          WebSocketConnection,
                //          sourceNodeId,
                //          BinaryMessage,
                //          EventTrackingId,
                //          CancellationToken
                //      );

            }
            catch (Exception e)
            {
                await HandleErrors(
                          nameof(WWCPWebSocketServer),
                          nameof(ProcessBinaryMessage),
                          e
                      );
            }

        }

        #endregion


        private IEnumerable<org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection> LookupNetworkingNode(NetworkingNode_Id NetworkingNodeId)
        {

            if (NetworkingNodeId == NetworkingNode_Id.Zero)
                return [];

            if (NetworkingNodeId == NetworkingNode_Id.Broadcast)
                return WebSocketConnections;

            var lookUpNetworkingNodeId = NetworkingNodeId;

            if (NetworkingNode.Routing.LookupNetworkingNode(lookUpNetworkingNodeId, out var reachability) &&
                reachability is not null)
            {
                lookUpNetworkingNodeId = reachability.DestinationId;
            }

            if (reachableViaNetworkingHubs.TryGetValue(lookUpNetworkingNodeId, out var networkingHubId))
            {
                lookUpNetworkingNodeId = networkingHubId;
                return WebSocketConnections.Where (connection => connection.TryGetCustomDataAs<NetworkingNode_Id>(WebSocketKeys.NetworkingNodeId) == lookUpNetworkingNodeId);
            }

            return WebSocketConnections.Where(connection => connection.TryGetCustomDataAs<NetworkingNode_Id>(WebSocketKeys.NetworkingNodeId) == lookUpNetworkingNodeId).ToArray();

        }

        public void AddStaticRouting(NetworkingNode_Id DestinationId,
                                     NetworkingNode_Id NetworkingHubId)
        {

            reachableViaNetworkingHubs.TryAdd(DestinationId,
                                              NetworkingHubId);

        }

        public void RemoveStaticRouting(NetworkingNode_Id DestinationId,
                                        NetworkingNode_Id NetworkingHubId)
        {

            reachableViaNetworkingHubs.TryRemove(new KeyValuePair<NetworkingNode_Id, NetworkingNode_Id>(DestinationId, NetworkingHubId));

        }



        protected Task SendOnJSONMessageSent(DateTime                                                                Timestamp,
                                             IWWCPWebSocketServer                                                    Server,
                                             org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection   WebSocketConnection,
                                             EventTracking_Id                                                        EventTrackingId,
                                             DateTime                                                                MessageTimestamp,
                                             JArray                                                                  Message,
                                             org.GraphDefined.Vanaheimr.Hermod.WebSocket.SentStatus                  SentStatus,
                                             CancellationToken                                                       CancellationToken)

            => LogEvent(
                   OnJSONMessageSent,
                   loggingDelegate => loggingDelegate.Invoke(
                       Timestamp,
                       Server,
                       WebSocketConnection,
                       EventTrackingId,
                       MessageTimestamp,
                       Message,
                       SentStatus,
                       CancellationToken
                   )
               );


        #region SendJSONRequest         (JSONRequestMessage)

        /// <summary>
        /// Send (and forget) the given JSON OCPP request message.
        /// </summary>
        /// <param name="JSONRequestMessage">A JSON OCPP request message.</param>
        public async Task<SentMessageResult> SendJSONMessage(org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection WebSocketConnection,
                                                             JArray             JSONMessage,
                                                             DateTime           RequestTimestamp,
                                                             NetworkingNode_Id  NetworkingNodeId,
                                                             EventTracking_Id   EventTrackingId,
                                                             CancellationToken  CancellationToken = default)
        {

            try
            {

                var sentStatus = await SendTextMessage(
                                           WebSocketConnection,
                                           JSONMessage.ToString(Formatting.None),
                                           EventTrackingId,
                                           CancellationToken
                                       );

                await LogEvent(
                          OnJSONMessageSent,
                          loggingDelegate => loggingDelegate.Invoke(
                              Timestamp.Now,
                              this,
                              WebSocketConnection,
                              EventTrackingId,
                              RequestTimestamp,
                              JSONMessage,
                              sentStatus,
                              CancellationToken
                          )
                      );

                if (sentStatus == org.GraphDefined.Vanaheimr.Hermod.WebSocket.SentStatus.Success)
                    return SentMessageResult.Success(WebSocketConnection);

                return SentMessageResult.UnknownClient();

            }
            catch (Exception e)
            {
                return SentMessageResult.TransmissionFailed(e);
            }

        }

        #endregion

        #region SendJSONRequest         (JSONRequestMessage)

        /// <summary>
        /// Send (and forget) the given JSON OCPP request message.
        /// </summary>
        /// <param name="JSONRequestMessage">A JSON OCPP request message.</param>
        public async Task<SentMessageResult> SendBinaryMessage(org.GraphDefined.Vanaheimr.Hermod.WebSocket.WebSocketServerConnection WebSocketConnection,
                                                               Byte[]             BinaryMessage,
                                                               DateTime           RequestTimestamp,
                                                               NetworkingNode_Id  NetworkingNodeId,
                                                               EventTracking_Id   EventTrackingId,
                                                               CancellationToken  CancellationToken = default)
        {

            try
            {

                var sentStatus = await SendBinaryMessage(
                                           WebSocketConnection,
                                           BinaryMessage,
                                           EventTrackingId,
                                           CancellationToken
                                       );

                await LogEvent(
                          OnBinaryMessageSent,
                          loggingDelegate => loggingDelegate.Invoke(
                              Timestamp.Now,
                              this,
                              WebSocketConnection,
                              EventTrackingId,
                              RequestTimestamp,
                              BinaryMessage,
                              sentStatus,
                              CancellationToken
                          )
                      );

                if (sentStatus == org.GraphDefined.Vanaheimr.Hermod.WebSocket.SentStatus.Success)
                    return SentMessageResult.Success(WebSocketConnection);

                return SentMessageResult.UnknownClient();

            }
            catch (Exception e)
            {
                return SentMessageResult.TransmissionFailed(e);
            }

        }

        #endregion





        #region (private) LogEvent(Logger, LogHandler, ...)

        private async Task LogEvent<TDelegate>(TDelegate?                                         Logger,
                                               Func<TDelegate, Task>                              LogHandler,
                                               [CallerArgumentExpression(nameof(Logger))] String  EventName     = "",
                                               [CallerMemberName()]                       String  WWCPCommand   = "")

            where TDelegate : Delegate

        {
            if (Logger is not null)
            {
                try
                {

                    await Task.WhenAll(
                              Logger.GetInvocationList().
                                     OfType<TDelegate>().
                                     Select(LogHandler)
                          );

                }
                catch (Exception e)
                {
                    await HandleErrors(nameof(WWCPWebSocketServer), $"{WWCPCommand}.{EventName}", e);
                }
            }
        }

        #endregion

        #region (private) HandleErrors(Module, Caller, ErrorResponse)

        private Task HandleErrors(String  Module,
                                  String  Caller,
                                  String  ErrorResponse)
        {

            DebugX.Log($"{Module}.{Caller}: {ErrorResponse}");

            return Task.CompletedTask;

        }

        #endregion

        #region (private) HandleErrors(Module, Caller, ExceptionOccured)

        private Task HandleErrors(String     Module,
                                  String     Caller,
                                  Exception  ExceptionOccured)
        {

            DebugX.LogException(ExceptionOccured, $"{Module}.{Caller}");

            return Task.CompletedTask;

        }

        #endregion


    }

}
