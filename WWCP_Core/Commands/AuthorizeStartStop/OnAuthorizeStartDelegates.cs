﻿/*
 * Copyright (c) 2014-2020 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP Core <https://github.com/OpenChargingCloud/WWCP_Core>
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
using System.Threading.Tasks;
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.WWCP
{

    #region OnAuthorizeStartRequest/-Response

    /// <summary>
    /// An event fired whenever an authentication token will be verified for charging.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An Charging Station Operator identification.</param>
    /// <param name="LocalAuthentication">An user identification.</param>
    /// <param name="ChargingProduct">The choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    public delegate Task OnAuthorizeStartRequestDelegate (DateTime                      LogTimestamp,
                                                          DateTime                      RequestTimestamp,
                                                          Object                        Sender,
                                                          String                        SenderId,
                                                          EventTracking_Id              EventTrackingId,
                                                          RoamingNetwork_Id             RoamingNetworkId,
                                                          EMPRoamingProvider_Id?        EMPRoamingProviderId,
                                                          CSORoamingProvider_Id?        CSORoamingProviderId,
                                                          ChargingStationOperator_Id?   OperatorId,
                                                          LocalAuthentication           LocalAuthentication,
                                                          ChargingProduct               ChargingProduct,
                                                          ChargingSession_Id?           SessionId,
                                                          TimeSpan?                     RequestTimeout);


    /// <summary>
    /// An event fired whenever an authentication token had been verified for charging.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An Charging Station Operator identification.</param>
    /// <param name="LocalAuthentication">An user identification.</param>
    /// <param name="ChargingProduct">The choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    /// <param name="Result">The authorize start result.</param>
    /// <param name="Runtime">The runtime of the request.</param>
    public delegate Task OnAuthorizeStartResponseDelegate(DateTime                      LogTimestamp,
                                                          DateTime                      RequestTimestamp,
                                                          Object                        Sender,
                                                          String                        SenderId,
                                                          EventTracking_Id              EventTrackingId,
                                                          RoamingNetwork_Id             RoamingNetworkId,
                                                          EMPRoamingProvider_Id?        EMPRoamingProviderId,
                                                          CSORoamingProvider_Id?        CSORoamingProviderId,
                                                          ChargingStationOperator_Id?   OperatorId,
                                                          LocalAuthentication           LocalAuthentication,
                                                          ChargingProduct               ChargingProduct,
                                                          ChargingSession_Id?           SessionId,
                                                          TimeSpan?                     RequestTimeout,
                                                          AuthStartResult               Result,
                                                          TimeSpan                      Runtime);

    #endregion

    #region OnAuthorizeEVSEStartRequest/-Response

    /// <summary>
    /// An event fired whenever an authentication token will be verified for charging at the given EVSE.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An Charging Station Operator identification.</param>
    /// <param name="LocalAuthentication">An user identification.</param>
    /// <param name="EVSEId">The unique identification of an EVSE.</param>
    /// <param name="ChargingProduct">The choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="ISendAuthorizeStartStops">The ISendAuthorizeStartStops entities to be asked.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    public delegate Task OnAuthorizeEVSEStartRequestDelegate (DateTime                               LogTimestamp,
                                                              DateTime                               RequestTimestamp,
                                                              Object                                 Sender,
                                                              String                                 SenderId,
                                                              EventTracking_Id                       EventTrackingId,
                                                              RoamingNetwork_Id                      RoamingNetworkId,
                                                              EMPRoamingProvider_Id?                 EMPRoamingProviderId,
                                                              CSORoamingProvider_Id?                 CSORoamingProviderId,
                                                              ChargingStationOperator_Id?            OperatorId,
                                                              LocalAuthentication                    LocalAuthentication,
                                                              EVSE_Id                                EVSEId,
                                                              ChargingProduct                        ChargingProduct,
                                                              ChargingSession_Id?                    SessionId,
                                                              IEnumerable<ISendAuthorizeStartStop>   ISendAuthorizeStartStops,
                                                              TimeSpan?                              RequestTimeout);


    /// <summary>
    /// An event fired whenever an authentication token had been verified for charging at the given EVSE.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An Charging Station Operator identification.</param>
    /// <param name="LocalAuthentication">An user identification.</param>
    /// <param name="EVSEId">The unique identification of an EVSE.</param>
    /// <param name="ChargingProduct">The choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="ISendAuthorizeStartStops">The ISendAuthorizeStartStops entities to be asked.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    /// <param name="Result">The authorize start result.</param>
    /// <param name="Runtime">The runtime of the request.</param>
    public delegate Task OnAuthorizeEVSEStartResponseDelegate(DateTime                               LogTimestamp,
                                                              DateTime                               RequestTimestamp,
                                                              Object                                 Sender,
                                                              String                                 SenderId,
                                                              EventTracking_Id                       EventTrackingId,
                                                              RoamingNetwork_Id                      RoamingNetworkId,
                                                              EMPRoamingProvider_Id?                 EMPRoamingProviderId,
                                                              CSORoamingProvider_Id?                 CSORoamingProviderId,
                                                              ChargingStationOperator_Id?            OperatorId,
                                                              LocalAuthentication                    LocalAuthentication,
                                                              EVSE_Id                                EVSEId,
                                                              ChargingProduct                        ChargingProduct,
                                                              ChargingSession_Id?                    SessionId,
                                                              IEnumerable<ISendAuthorizeStartStop>   ISendAuthorizeStartStops,
                                                              TimeSpan?                              RequestTimeout,
                                                              AuthStartEVSEResult                    Result,
                                                              TimeSpan                               Runtime);

    #endregion

    #region OnAuthorizeChargingStationStartRequest/-Response

    /// <summary>
    /// An event fired whenever an authentication token will be verified for charging at the given charging station.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An Charging Station Operator identification.</param>
    /// <param name="LocalAuthentication">An user identification.</param>
    /// <param name="ChargingStationId">The unique identification of a charging station.</param>
    /// <param name="ChargingProduct">The choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    public delegate Task OnAuthorizeChargingStationStartRequestDelegate (DateTime                      LogTimestamp,
                                                                         DateTime                      RequestTimestamp,
                                                                         Object                        Sender,
                                                                         String                        SenderId,
                                                                         EventTracking_Id              EventTrackingId,
                                                                         RoamingNetwork_Id             RoamingNetworkId,
                                                                         EMPRoamingProvider_Id?        EMPRoamingProviderId,
                                                                         CSORoamingProvider_Id?        CSORoamingProviderId,
                                                                         ChargingStationOperator_Id?   OperatorId,
                                                                         LocalAuthentication           LocalAuthentication,
                                                                         ChargingStation_Id            ChargingStationId,
                                                                         ChargingProduct               ChargingProduct,
                                                                         ChargingSession_Id?           SessionId,
                                                                         TimeSpan?                     RequestTimeout);


    /// <summary>
    /// An event fired whenever an authentication token had been verified for charging at the given charging station.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An Charging Station Operator identification.</param>
    /// <param name="LocalAuthentication">An user identification.</param>
    /// <param name="ChargingStationId">The unique identification of a charging station.</param>
    /// <param name="ChargingProduct">The choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    /// <param name="Result">The authorize start result.</param>
    /// <param name="Runtime">The runtime of the request.</param>
    public delegate Task OnAuthorizeChargingStationStartResponseDelegate(DateTime                         LogTimestamp,
                                                                         DateTime                         RequestTimestamp,
                                                                         Object                           Sender,
                                                                         String                           SenderId,
                                                                         EventTracking_Id                 EventTrackingId,
                                                                         RoamingNetwork_Id                RoamingNetworkId,
                                                                         EMPRoamingProvider_Id?           EMPRoamingProviderId,
                                                                         CSORoamingProvider_Id?           CSORoamingProviderId,
                                                                         ChargingStationOperator_Id?      OperatorId,
                                                                         LocalAuthentication              LocalAuthentication,
                                                                         ChargingStation_Id               ChargingStationId,
                                                                         ChargingProduct                  ChargingProduct,
                                                                         ChargingSession_Id?              SessionId,
                                                                         TimeSpan?                        RequestTimeout,
                                                                         AuthStartChargingStationResult   Result,
                                                                         TimeSpan                         Runtime);

    #endregion

    #region OnAuthorizeChargingPoolStartRequest/-Response

    /// <summary>
    /// An event fired whenever an authentication token will be verified for charging at the given charging pool.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An Charging Station Operator identification.</param>
    /// <param name="LocalAuthentication">An user identification.</param>
    /// <param name="ChargingPoolId">The unique identification of a charging pool.</param>
    /// <param name="ChargingProduct">The choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    public delegate Task OnAuthorizeChargingPoolStartRequestDelegate (DateTime                      LogTimestamp,
                                                                      DateTime                      RequestTimestamp,
                                                                      Object                        Sender,
                                                                      String                        SenderId,
                                                                      EventTracking_Id              EventTrackingId,
                                                                      RoamingNetwork_Id             RoamingNetworkId,
                                                                      EMPRoamingProvider_Id?        EMPRoamingProviderId,
                                                                      CSORoamingProvider_Id?        CSORoamingProviderId,
                                                                      ChargingStationOperator_Id?   OperatorId,
                                                                      LocalAuthentication           LocalAuthentication,
                                                                      ChargingPool_Id               ChargingPoolId,
                                                                      ChargingProduct               ChargingProduct,
                                                                      ChargingSession_Id?           SessionId,
                                                                      TimeSpan?                     RequestTimeout);


    /// <summary>
    /// An event fired whenever an authentication token had been verified for charging at the given charging pool.
    /// </summary>
    /// <param name="Timestamp">The timestamp of the request.</param>
    /// <param name="Sender">The sender of the request.</param>
    /// <param name="EventTrackingId">An unique event tracking identification for correlating this request with other events.</param>
    /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
    /// <param name="OperatorId">An Charging Station Operator identification.</param>
    /// <param name="LocalAuthentication">An user identification.</param>
    /// <param name="ChargingPoolId">The unique identification of a charging pool.</param>
    /// <param name="ChargingProduct">The choosen charging product.</param>
    /// <param name="SessionId">The unique identification for this charging session.</param>
    /// <param name="RequestTimeout">An optional timeout for this request.</param>
    /// <param name="Result">The authorize start result.</param>
    /// <param name="Runtime">The runtime of the request.</param>
    public delegate Task OnAuthorizeChargingPoolStartResponseDelegate(DateTime                      LogTimestamp,
                                                                      DateTime                      RequestTimestamp,
                                                                      Object                        Sender,
                                                                      String                        SenderId,
                                                                      EventTracking_Id              EventTrackingId,
                                                                      RoamingNetwork_Id             RoamingNetworkId,
                                                                      EMPRoamingProvider_Id?        EMPRoamingProviderId,
                                                                      CSORoamingProvider_Id?        CSORoamingProviderId,
                                                                      ChargingStationOperator_Id?   OperatorId,
                                                                      LocalAuthentication           LocalAuthentication,
                                                                      ChargingPool_Id               ChargingPoolId,
                                                                      ChargingProduct               ChargingProduct,
                                                                      ChargingSession_Id?           SessionId,
                                                                      TimeSpan?                     RequestTimeout,
                                                                      AuthStartChargingPoolResult   Result,
                                                                      TimeSpan                      Runtime);

    #endregion

}
