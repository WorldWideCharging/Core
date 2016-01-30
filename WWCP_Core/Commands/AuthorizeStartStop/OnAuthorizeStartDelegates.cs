﻿/*
 * Copyright (c) 2014-2016 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP Core <https://github.com/GraphDefined/WWCP_Core>
 *
 * Licensed under the Affero GPL license, Version 3.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.gnu.org/licenses/agpl.html
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

#region Usings

using System;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.WWCP
{

    /// <summary>
    /// An event fired whenever an authentication token will be verified.
    /// </summary>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An EVSE operator identification.</param>
    /// <param name="AuthToken">A (RFID) user identification.</param>
    /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="QueryTimeout">An optional timeout for this request.</param>
    public delegate void OnAuthorizeStartDelegate(Object              Sender,
                                                  DateTime            Timestamp,
                                                  EventTracking_Id    EventTrackingId,
                                                  RoamingNetwork_Id   RoamingNetworkId,
                                                  EVSEOperator_Id     OperatorId,
                                                  Auth_Token          AuthToken,
                                                  ChargingProduct_Id  ChargingProductId,
                                                  ChargingSession_Id  SessionId,
                                                  TimeSpan?           QueryTimeout);


    /// <summary>
    /// An event fired whenever an authentication token had been verified.
    /// </summary>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An EVSE operator identification.</param>
    /// <param name="AuthToken">A (RFID) user identification.</param>
    /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="QueryTimeout">An optional timeout for this request.</param>
    /// <param name="Result">The authorize start result.</param>
    public delegate void OnAuthorizeStartedDelegate(Object              Sender,
                                                    DateTime            Timestamp,
                                                    EventTracking_Id    EventTrackingId,
                                                    RoamingNetwork_Id   RoamingNetworkId,
                                                    EVSEOperator_Id     OperatorId,
                                                    Auth_Token          AuthToken,
                                                    ChargingProduct_Id  ChargingProductId,
                                                    ChargingSession_Id  SessionId,
                                                    TimeSpan?           QueryTimeout,
                                                    AuthStartResult     Result);


    // ----------------------------------------------------------------------------------------------------------


    /// <summary>
    /// An event fired whenever an authentication token will be verified at the given EVSE.
    /// </summary>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An EVSE operator identification.</param>
    /// <param name="AuthToken">A (RFID) user identification.</param>
    /// <param name="EVSEId">The unique identification of an EVSE.</param>
    /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="QueryTimeout">An optional timeout for this request.</param>
    public delegate void OnAuthorizeEVSEStartDelegate(Object              Sender,
                                                      DateTime            Timestamp,
                                                      EventTracking_Id    EventTrackingId,
                                                      RoamingNetwork_Id   RoamingNetworkId,
                                                      EVSEOperator_Id     OperatorId,
                                                      Auth_Token          AuthToken,
                                                      EVSE_Id             EVSEId,
                                                      ChargingProduct_Id  ChargingProductId,
                                                      ChargingSession_Id  SessionId,
                                                      TimeSpan?           QueryTimeout);


    /// <summary>
    /// An event fired whenever an authentication token had been verified at the given EVSE.
    /// </summary>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An EVSE operator identification.</param>
    /// <param name="AuthToken">A (RFID) user identification.</param>
    /// <param name="EVSEId">The unique identification of an EVSE.</param>
    /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="QueryTimeout">An optional timeout for this request.</param>
    /// <param name="Result">The authorize start result.</param>
    public delegate void OnAuthorizeEVSEStartedDelegate(Object               Sender,
                                                        DateTime             Timestamp,
                                                        EventTracking_Id     EventTrackingId,
                                                        RoamingNetwork_Id    RoamingNetworkId,
                                                        EVSEOperator_Id      OperatorId,
                                                        Auth_Token           AuthToken,
                                                        EVSE_Id              EVSEId,
                                                        ChargingProduct_Id   ChargingProductId,
                                                        ChargingSession_Id   SessionId,
                                                        TimeSpan?            QueryTimeout,
                                                        AuthStartEVSEResult  Result);


    // ----------------------------------------------------------------------------------------------------------



    /// <summary>
    /// An event fired whenever an authentication token will be verified at the given charging station.
    /// </summary>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An EVSE operator identification.</param>
    /// <param name="AuthToken">A (RFID) user identification.</param>
    /// <param name="ChargingStationId">The unique identification of a charging station.</param>
    /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="QueryTimeout">An optional timeout for this request.</param>
    public delegate void OnAuthorizeChargingStationStartDelegate(Object              Sender,
                                                                 DateTime            Timestamp,
                                                                 EventTracking_Id    EventTrackingId,
                                                                 RoamingNetwork_Id   RoamingNetworkId,
                                                                 EVSEOperator_Id     OperatorId,
                                                                 Auth_Token          AuthToken,
                                                                 ChargingStation_Id  ChargingStationId,
                                                                 ChargingProduct_Id  ChargingProductId,
                                                                 ChargingSession_Id  SessionId,
                                                                 TimeSpan?           QueryTimeout);


    /// <summary>
    /// An event fired whenever an authentication token had been verified at the given charging station.
    /// </summary>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An EVSE operator identification.</param>
    /// <param name="AuthToken">A (RFID) user identification.</param>
    /// <param name="ChargingStationId">The unique identification of a charging station.</param>
    /// <param name="ChargingProductId">The unique identification of the choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="QueryTimeout">An optional timeout for this request.</param>
    /// <param name="Result">The authorize start result.</param>
    public delegate void OnAuthorizeChargingStationStartedDelegate(Object                          Sender,
                                                                   DateTime                        Timestamp,
                                                                   EventTracking_Id                EventTrackingId,
                                                                   RoamingNetwork_Id               RoamingNetworkId,
                                                                   EVSEOperator_Id                 OperatorId,
                                                                   Auth_Token                      AuthToken,
                                                                   ChargingStation_Id              ChargingStationId,
                                                                   ChargingProduct_Id              ChargingProductId,
                                                                   ChargingSession_Id              SessionId,
                                                                   TimeSpan?                       QueryTimeout,
                                                                   AuthStartChargingStationResult  Result);

}
