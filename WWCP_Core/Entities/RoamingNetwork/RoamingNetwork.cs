﻿/*
 * Copyright (c) 2014-2015 GraphDefined GmbH
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
using System.Linq;
using System.Collections.Generic;
using System.Collections.Concurrent;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Illias.Votes;
using org.GraphDefined.Vanaheimr.Styx.Arrows;

using org.GraphDefined.WWCP;
using System.Threading.Tasks;
using System.Threading;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace org.GraphDefined.WWCP
{

    /// <summary>
    /// A Electric Vehicle Roaming Network is a service abstraction to allow multiple
    /// independent roaming services to be delivered over the same infrastructure.
    /// This can e.g. be a differentation of service levels (Premiun, Normal,
    /// Discount) or allow a simplified testing (Production, QA, Testing, ...)
    /// The RoamingNetwork class also provides access to all registered electric
    /// vehicle charging entities within an EV Roaming Network.
    /// </summary>
    public class RoamingNetwork : AEMobilityEntity<RoamingNetwork_Id>,
                                  IEquatable<RoamingNetwork>, IComparable<RoamingNetwork>, IComparable,
                                  IEnumerable<IEntity>,
                                  IStatus<RoamingNetworkStatusType>
    {

        #region Data

        public static readonly TimeSpan MaxReservationDuration              = TimeSpan.FromMinutes(15);

        private readonly ConcurrentDictionary<EVSEOperator_Id,               EVSEOperator>               _EVSEOperators;
        private readonly ConcurrentDictionary<EVSP_Id,                       EVSP>                       _EVServiceProviders;
        private readonly ConcurrentDictionary<RoamingProvider_Id,            RoamingProvider>            _RoamingProviders;
        private readonly ConcurrentDictionary<NavigationServiceProvider_Id,  NavigationServiceProvider>  _SearchProviders;

        private readonly ConcurrentDictionary<ChargingReservation_Id, ChargingReservation>               _ChargingReservations;

        private readonly Dictionary<UInt32,             IAuthServices>     AuthenticationServices;
        private readonly Dictionary<ChargingSession_Id, IAuthServices>     SessionIdAuthenticatorCache;

        #endregion

        #region Properties

        #region AuthorizatorId

        private readonly Authorizator_Id _AuthorizatorId;

        public Authorizator_Id AuthorizatorId
        {
            get
            {
                return _AuthorizatorId;
            }
        }

        #endregion


        #region AllTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> AllTokens
        {
            get
            {
                return AuthenticationServices.SelectMany(vv => vv.Value.AllTokens);
            }
        }

        #endregion

        #region AuthorizedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> AuthorizedTokens
        {
            get
            {
                return AuthenticationServices.SelectMany(vv => vv.Value.AuthorizedTokens);
            }
        }

        #endregion

        #region NotAuthorizedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> NotAuthorizedTokens
        {
            get
            {
                return AuthenticationServices.SelectMany(vv => vv.Value.NotAuthorizedTokens);
            }
        }

        #endregion

        #region BlockedTokens

        public IEnumerable<KeyValuePair<Auth_Token, TokenAuthorizationResultType>> BlockedTokens
        {
            get
            {
                return AuthenticationServices.SelectMany(vv => vv.Value.BlockedTokens);
            }
        }

        #endregion


        #region Description

        private I18NString _Description;

        /// <summary>
        /// An optional additional (multi-language) description of the RoamingNetwork.
        /// </summary>
        [Mandatory]
        public I18NString Description
        {

            get
            {
                return _Description;
            }

            set
            {
                SetProperty<I18NString>(ref _Description, value);
            }

        }

        #endregion


        #region Status

        /// <summary>
        /// The current roaming network status.
        /// </summary>
        [Optional]
        public Timestamped<RoamingNetworkStatusType> Status
        {
            get
            {
                return _StatusHistory.Peek();
            }
        }

        #endregion

        #region StatusHistory

        private Stack<Timestamped<RoamingNetworkStatusType>> _StatusHistory;

        /// <summary>
        /// The roaming network status history.
        /// </summary>
        [Optional]
        public IEnumerable<Timestamped<RoamingNetworkStatusType>> StatusHistory
        {
            get
            {
                return _StatusHistory.OrderByDescending(v => v.Timestamp);
            }
        }

        #endregion

        #region StatusAggregationDelegate

        private Func<EVSEOperatorStatusReport, RoamingNetworkStatusType> _StatusAggregationDelegate;

        /// <summary>
        /// A delegate called to aggregate the dynamic status of all subordinated EVSE operators.
        /// </summary>
        public Func<EVSEOperatorStatusReport, RoamingNetworkStatusType> StatusAggregationDelegate
        {

            get
            {
                return _StatusAggregationDelegate;
            }

            set
            {
                _StatusAggregationDelegate = value;
            }

        }

        #endregion


        #region AdminStatus

        /// <summary>
        /// The current roaming network admin status.
        /// </summary>
        [Optional]
        public Timestamped<RoamingNetworkAdminStatusType> AdminStatus
        {
            get
            {
                return _AdminStatusHistory.Peek();
            }
        }

        #endregion

        #region AdminStatusHistory

        private Stack<Timestamped<RoamingNetworkAdminStatusType>> _AdminStatusHistory;

        /// <summary>
        /// The roaming network admin status history.
        /// </summary>
        [Optional]
        public IEnumerable<Timestamped<RoamingNetworkAdminStatusType>> AdminStatusHistory
        {
            get
            {
                return _AdminStatusHistory.OrderByDescending(v => v.Timestamp);
            }
        }

        #endregion

        #region AdminStatusAggregationDelegate

        private Func<EVSEOperatorAdminStatusReport, RoamingNetworkAdminStatusType> _AdminStatusAggregationDelegate;

        /// <summary>
        /// A delegate called to aggregate the admin status of all subordinated EVSE operators.
        /// </summary>
        public Func<EVSEOperatorAdminStatusReport, RoamingNetworkAdminStatusType> AdminStatusAggregationDelegate
        {

            get
            {
                return _AdminStatusAggregationDelegate;
            }

            set
            {
                _AdminStatusAggregationDelegate = value;
            }

        }

        #endregion


        #region EVSEOperators

        /// <summary>
        /// Return all EVSE operators registered within this roaming network.
        /// </summary>
        public IEnumerable<EVSEOperator> EVSEOperators
        {
            get
            {
                return _EVSEOperators.Select(KVP => KVP.Value);
            }
        }

        #endregion

        #region EVServiceProviders

        /// <summary>
        /// Return all EV service providers registered within this roaming network.
        /// </summary>
        public IEnumerable<EVSP> EVServiceProviders
        {
            get
            {
                return _EVServiceProviders.Select(KVP => KVP.Value);
            }
        }

        #endregion

        #region RoamingProviders

        /// <summary>
        /// Return all roaming providers registered within this roaming network.
        /// </summary>
        public IEnumerable<RoamingProvider> RoamingProviders
        {
            get
            {
                return _RoamingProviders.Values;
            }
        }

        #endregion

        #region SearchProviders

        /// <summary>
        /// Return all search providers registered within this roaming network.
        /// </summary>
        public IEnumerable<NavigationServiceProvider> SearchProviders
        {
            get
            {
                return _SearchProviders.Select(KVP => KVP.Value);
            }
        }

        #endregion


        #region ChargingPools

        /// <summary>
        /// Return all charging pools registered within this roaming network.
        /// </summary>
        public IEnumerable<ChargingPool> ChargingPools
        {
            get
            {
                return _EVSEOperators.SelectMany(evseoperator => evseoperator.Value);
            }
        }

        #endregion

        #region ChargingPoolStatus

        /// <summary>
        /// Return the status of all charging pools registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<ChargingPool_Id, IEnumerable<Timestamped<ChargingPoolStatusType>>>> ChargingPoolStatus
        {
            get
            {

                return _EVSEOperators.Values.
                           SelectMany(evseoperator =>
                               evseoperator.Select(pool =>

                                           new KeyValuePair<ChargingPool_Id, IEnumerable<Timestamped<ChargingPoolStatusType>>>(
                                               pool.Id,
                                               pool.StatusSchedule)

                                       ));

            }
        }

        #endregion

        #region ChargingStations

        /// <summary>
        /// Return all charging stations registered within this roaming network.
        /// </summary>
        public IEnumerable<ChargingStation> ChargingStations
        {
            get
            {
                return _EVSEOperators.SelectMany(evseoperator => evseoperator.Value.SelectMany(pool => pool));
            }
        }

        #endregion

        #region ChargingStationStatus

        /// <summary>
        /// Return the status of all charging stations registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<ChargingStation_Id, IEnumerable<Timestamped<ChargingStationStatusType>>>> ChargingStationStatus
        {
            get
            {

                return _EVSEOperators.Values.
                           SelectMany(evseoperator =>
                               evseoperator.SelectMany(pool =>
                                   pool.Select(station =>

                                           new KeyValuePair<ChargingStation_Id, IEnumerable<Timestamped<ChargingStationStatusType>>>(
                                               station.Id,
                                               station.StatusSchedule)

                                       )));

            }
        }

        #endregion

        #region EVSEs

        /// <summary>
        /// Return all EVSEs registered within this roaming network.
        /// </summary>
        public IEnumerable<EVSE> EVSEs
        {
            get
            {
                return _EVSEOperators.SelectMany(evseoperator => evseoperator.Value.SelectMany(pool => pool.SelectMany(station => station)));
            }
        }

        #endregion

        #region EVSEStatus

        /// <summary>
        /// Return the status of all EVSEs registered within this roaming network.
        /// </summary>
        public IEnumerable<KeyValuePair<EVSE_Id, IEnumerable<Timestamped<EVSEStatusType>>>> EVSEStatus
        {
            get
            {

                return _EVSEOperators.Values.
                           SelectMany(evseoperator =>
                               evseoperator.SelectMany(pool =>
                                   pool.SelectMany(station =>
                                       station.Select(evse =>

                                           new KeyValuePair<EVSE_Id, IEnumerable<Timestamped<EVSEStatusType>>>(
                                               evse.Id,
                                               evse.StatusSchedule)

                                       ))));

            }
        }

        #endregion

        #region ChargingReservations

        /// <summary>
        /// Return all charging reservations registered within this roaming network.
        /// </summary>
        public IEnumerable<ChargingReservation> ChargingReservations
        {
            get
            {
                return _ChargingReservations.Select(kvp => kvp.Value);
            }
        }

        #endregion

        #endregion

        #region Events

        // RoamingNetwork events

        #region OnAggregatedStatusChanged

        /// <summary>
        /// A delegate called whenever the aggregated dynamic status changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="RoamingNetwork">The updated roaming network.</param>
        /// <param name="OldStatus">The old timestamped status of the roaming network.</param>
        /// <param name="NewStatus">The new timestamped status of the roaming network.</param>
        public delegate void OnAggregatedStatusChangedDelegate(DateTime Timestamp, RoamingNetwork RoamingNetwork, Timestamped<RoamingNetworkStatusType> OldStatus, Timestamped<RoamingNetworkStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the aggregated dynamic status changed.
        /// </summary>
        public event OnAggregatedStatusChangedDelegate OnAggregatedStatusChanged;

        #endregion

        #region OnAggregatedAdminStatusChanged

        /// <summary>
        /// A delegate called whenever the aggregated admin status changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="RoamingNetwork">The updated roaming network.</param>
        /// <param name="OldStatus">The old timestamped status of the roaming network.</param>
        /// <param name="NewStatus">The new timestamped status of the roaming network.</param>
        public delegate void OnAggregatedAdminStatusChangedDelegate(DateTime Timestamp, RoamingNetwork RoamingNetwork, Timestamped<RoamingNetworkAdminStatusType> OldStatus, Timestamped<RoamingNetworkAdminStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the aggregated admin status changed.
        /// </summary>
        public event OnAggregatedAdminStatusChangedDelegate OnAggregatedAdminStatusChanged;

        #endregion

        #region EVSEOperatorAddition

        private readonly IVotingNotificator<DateTime, RoamingNetwork, EVSEOperator, Boolean> EVSEOperatorAddition;

        /// <summary>
        /// Called whenever an EVSEOperator will be or was added.
        /// </summary>
        public IVotingSender<DateTime, RoamingNetwork, EVSEOperator, Boolean> OnEVSEOperatorAddition
        {
            get
            {
                return EVSEOperatorAddition;
            }
        }

        #endregion

        #region EVSEOperatorRemoval

        private readonly IVotingNotificator<DateTime, RoamingNetwork, EVSEOperator, Boolean> EVSEOperatorRemoval;

        /// <summary>
        /// Called whenever an EVSEOperator will be or was removed.
        /// </summary>
        public IVotingSender<DateTime, RoamingNetwork, EVSEOperator, Boolean> OnEVSEOperatorRemoval
        {
            get
            {
                return EVSEOperatorRemoval;
            }
        }

        #endregion


        #region EVServiceProviderAddition

        private readonly IVotingNotificator<RoamingNetwork, EVSP, Boolean> EVServiceProviderAddition;

        /// <summary>
        /// Called whenever an EVServiceProvider will be or was added.
        /// </summary>
        public IVotingSender<RoamingNetwork, EVSP, Boolean> OnEVServiceProviderAddition
        {
            get
            {
                return EVServiceProviderAddition;
            }
        }

        #endregion

        #region EVServiceProviderRemoval

        private readonly IVotingNotificator<RoamingNetwork, EVSP, Boolean> EVServiceProviderRemoval;

        /// <summary>
        /// Called whenever an EVServiceProvider will be or was removed.
        /// </summary>
        public IVotingSender<RoamingNetwork, EVSP, Boolean> OnEVServiceProviderRemoval
        {
            get
            {
                return EVServiceProviderRemoval;
            }
        }

        #endregion


        #region RoamingProviderAddition

        private readonly IVotingNotificator<RoamingNetwork, RoamingProvider, Boolean> RoamingProviderAddition;

        /// <summary>
        /// Called whenever a RoamingProvider will be or was added.
        /// </summary>
        public IVotingSender<RoamingNetwork, RoamingProvider, Boolean> OnRoamingProviderAddition
        {
            get
            {
                return RoamingProviderAddition;
            }
        }

        #endregion

        #region RoamingProviderRemoval

        private readonly IVotingNotificator<RoamingNetwork, RoamingProvider, Boolean> RoamingProviderRemoval;

        /// <summary>
        /// Called whenever a RoamingProvider will be or was removed.
        /// </summary>
        public IVotingSender<RoamingNetwork, RoamingProvider, Boolean> OnRoamingProviderRemoval
        {
            get
            {
                return RoamingProviderRemoval;
            }
        }

        #endregion


        #region SearchProviderAddition

        private readonly IVotingNotificator<RoamingNetwork, NavigationServiceProvider, Boolean> SearchProviderAddition;

        /// <summary>
        /// Called whenever an SearchProvider will be or was added.
        /// </summary>
        public IVotingSender<RoamingNetwork, NavigationServiceProvider, Boolean> OnSearchProviderAddition
        {
            get
            {
                return SearchProviderAddition;
            }
        }

        #endregion

        #region SearchProviderRemoval

        private readonly IVotingNotificator<RoamingNetwork, NavigationServiceProvider, Boolean> SearchProviderRemoval;

        /// <summary>
        /// Called whenever an SearchProvider will be or was removed.
        /// </summary>
        public IVotingSender<RoamingNetwork, NavigationServiceProvider, Boolean> OnSearchProviderRemoval
        {
            get
            {
                return SearchProviderRemoval;
            }
        }

        #endregion


        // EVSEOperator events

        #region OnEVSEOperatorDataChanged

        /// <summary>
        /// A delegate called whenever the static data of any subordinated EVSE operator changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSEOperator">The updated evse operator.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        public delegate void OnEVSEOperatorDataChangedDelegate(DateTime Timestamp, EVSEOperator EVSEOperator, String PropertyName, Object OldValue, Object NewValue);

        /// <summary>
        /// An event fired whenever the static data of any subordinated EVSE operator changed.
        /// </summary>
        public event OnEVSEOperatorDataChangedDelegate OnEVSEOperatorDataChanged;

        #endregion

        #region OnAggregatedEVSEOperatorStatusChanged

        /// <summary>
        /// A delegate called whenever the aggregated dynamic status of any subordinated EVSE operator changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSEOperator">The updated EVSE operator.</param>
        /// <param name="OldStatus">The old timestamped status of the EVSE operator.</param>
        /// <param name="NewStatus">The new timestamped status of the EVSE operator.</param>
        public delegate void OnAggregatedEVSEOperatorStatusChangedDelegate(DateTime Timestamp, EVSEOperator EVSEOperator, Timestamped<EVSEOperatorStatusType> OldStatus, Timestamped<EVSEOperatorStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the aggregated dynamic status of any subordinated EVSE operator changed.
        /// </summary>
        public event OnAggregatedEVSEOperatorStatusChangedDelegate OnAggregatedEVSEOperatorStatusChanged;

        #endregion

        #region OnAggregatedEVSEOperatorAdminStatusChanged

        /// <summary>
        /// A delegate called whenever the aggregated admin status of any subordinated EVSE operator changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSEOperator">The updated EVSE operator.</param>
        /// <param name="OldStatus">The old timestamped status of the EVSE operator.</param>
        /// <param name="NewStatus">The new timestamped status of the EVSE operator.</param>
        public delegate void OnAggregatedEVSEOperatorAdminStatusChangedDelegate(DateTime Timestamp, EVSEOperator EVSEOperator, Timestamped<EVSEOperatorAdminStatusType> OldStatus, Timestamped<EVSEOperatorAdminStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the aggregated admin status of any subordinated EVSE operator changed.
        /// </summary>
        public event OnAggregatedEVSEOperatorAdminStatusChangedDelegate OnAggregatedEVSEOperatorAdminStatusChanged;

        #endregion

        #region ChargingPoolAddition

        internal readonly IVotingNotificator<DateTime, EVSEOperator, ChargingPool, Boolean> ChargingPoolAddition;

        /// <summary>
        /// Called whenever an EVS pool will be or was added.
        /// </summary>
        public IVotingSender<DateTime, EVSEOperator, ChargingPool, Boolean> OnChargingPoolAddition
        {
            get
            {
                return ChargingPoolAddition;
            }
        }

        #endregion

        #region ChargingPoolRemoval

        internal readonly IVotingNotificator<DateTime, EVSEOperator, ChargingPool, Boolean> ChargingPoolRemoval;

        /// <summary>
        /// Called whenever an EVS pool will be or was removed.
        /// </summary>
        public IVotingSender<DateTime, EVSEOperator, ChargingPool, Boolean> OnChargingPoolRemoval
        {
            get
            {
                return ChargingPoolRemoval;
            }
        }

        #endregion


        // ChargingPool events

        #region OnChargingPoolDataChanged

        /// <summary>
        /// A delegate called whenever the static data of any subordinated charging pool changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingPool">The updated charging pool.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        public delegate void OnChargingPoolDataChangedDelegate(DateTime Timestamp, ChargingPool ChargingPool, String PropertyName, Object OldValue, Object NewValue);

        /// <summary>
        /// An event fired whenever the static data of any subordinated charging pool changed.
        /// </summary>
        public event OnChargingPoolDataChangedDelegate OnChargingPoolDataChanged;

        #endregion

        #region OnAggregatedChargingPoolStatusChanged

        /// <summary>
        /// A delegate called whenever the aggregated dynamic status of any subordinated charging pool changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingPool">The updated charging pool.</param>
        /// <param name="OldStatus">The old timestamped status of the charging pool.</param>
        /// <param name="NewStatus">The new timestamped status of the charging pool.</param>
        public delegate void OnAggregatedChargingPoolStatusChangedDelegate(DateTime Timestamp, ChargingPool ChargingPool, Timestamped<ChargingPoolStatusType> OldStatus, Timestamped<ChargingPoolStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the aggregated dynamic status of any subordinated charging pool changed.
        /// </summary>
        public event OnAggregatedChargingPoolStatusChangedDelegate OnAggregatedChargingPoolStatusChanged;

        #endregion

        #region OnAggregatedChargingPoolAdminStatusChanged

        /// <summary>
        /// A delegate called whenever the aggregated admin status of any subordinated charging pool changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingPool">The updated charging pool.</param>
        /// <param name="OldStatus">The old timestamped status of the charging pool.</param>
        /// <param name="NewStatus">The new timestamped status of the charging pool.</param>
        public delegate void OnAggregatedChargingPoolAdminStatusChangedDelegate(DateTime Timestamp, ChargingPool ChargingPool, Timestamped<ChargingPoolAdminStatusType> OldStatus, Timestamped<ChargingPoolAdminStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the aggregated admin status of any subordinated charging pool changed.
        /// </summary>
        public event OnAggregatedChargingPoolAdminStatusChangedDelegate OnAggregatedChargingPoolAdminStatusChanged;

        #endregion

        #region OnChargingPoolAdminDiff

        public delegate void OnChargingPoolAdminDiffDelegate(ChargingPoolAdminStatusDiff StatusDiff);

        /// <summary>
        /// An event fired whenever a charging station admin status diff was received.
        /// </summary>
        public event OnChargingPoolAdminDiffDelegate OnChargingPoolAdminDiff;

        #endregion

        #region OnChargingPoolReserved

        /// <summary>
        /// A delegate called whenever a charging pool was reserved.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this reservation was created.</param>
        /// <param name="Reservation">The charging reservation.</param>
        public delegate void OnChargingPoolReservedDelegate(DateTime Timestamp, ChargingReservation Reservation);

        /// <summary>
        /// An event fired whenever a charging pool was reserved.
        /// </summary>
        public event OnChargingPoolReservedDelegate OnChargingPoolReserved;

        #endregion

        #region ChargingStationAddition

        internal readonly IVotingNotificator<DateTime, ChargingPool, ChargingStation, Boolean> ChargingStationAddition;

        /// <summary>
        /// Called whenever a charging station will be or was added.
        /// </summary>
        public IVotingSender<DateTime, ChargingPool, ChargingStation, Boolean> OnChargingStationAddition
        {
            get
            {
                return ChargingStationAddition;
            }
        }

        #endregion

        #region ChargingStationRemoval

        internal readonly IVotingNotificator<DateTime, ChargingPool, ChargingStation, Boolean> ChargingStationRemoval;

        /// <summary>
        /// Called whenever a charging station will be or was removed.
        /// </summary>
        public IVotingSender<DateTime, ChargingPool, ChargingStation, Boolean> OnChargingStationRemoval
        {
            get
            {
                return ChargingStationRemoval;
            }
        }

        #endregion


        // ChargingStation events

        #region OnChargingStationDataChanged

        /// <summary>
        /// A delegate called whenever the static data of any subordinated charging station changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingStation">The updated charging station.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        public delegate void OnChargingStationDataChangedDelegate(DateTime Timestamp, ChargingStation ChargingStation, String PropertyName, Object OldValue, Object NewValue);

        /// <summary>
        /// An event fired whenever the static data of any subordinated charging station changed.
        /// </summary>
        public event OnChargingStationDataChangedDelegate OnChargingStationDataChanged;

        #endregion

        #region OnChargingStationAdminStatusChanged

        /// <summary>
        /// A delegate called whenever the admin status of any subordinated charging station changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingStation">The updated charging station.</param>
        /// <param name="OldStatus">The old timestamped admin status of the charging station.</param>
        /// <param name="NewStatus">The new timestamped admin status of the charging station.</param>
        public delegate void OnChargingStationAdminStatusChangedDelegate(DateTime Timestamp, ChargingStation ChargingStation, Timestamped<ChargingStationAdminStatusType> OldStatus, Timestamped<ChargingStationAdminStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the admin status of any subordinated ChargingStation changed.
        /// </summary>
        public event OnChargingStationAdminStatusChangedDelegate OnChargingStationAdminStatusChanged;

        #endregion

        #region OnChargingStationAdminDiff

        public delegate void OnChargingStationAdminDiffDelegate(ChargingStationAdminStatusDiff StatusDiff);

        /// <summary>
        /// An event fired whenever a charging station admin status diff was received.
        /// </summary>
        public event OnChargingStationAdminDiffDelegate OnChargingStationAdminDiff;

        #endregion

        #region OnAggregatedChargingStationStatusChanged

        /// <summary>
        /// A delegate called whenever the aggregated dynamic status of any subordinated charging station changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingStation">The updated charging station.</param>
        /// <param name="OldStatus">The old timestamped status of the charging station.</param>
        /// <param name="NewStatus">The new timestamped status of the charging station.</param>
        public delegate void OnAggregatedChargingStationStatusChangedDelegate(DateTime Timestamp, ChargingStation ChargingStation, Timestamped<ChargingStationStatusType> OldStatus, Timestamped<ChargingStationStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the aggregated dynamic status of any subordinated charging station changed.
        /// </summary>
        public event OnAggregatedChargingStationStatusChangedDelegate OnAggregatedChargingStationStatusChanged;

        #endregion

        #region OnAggregatedChargingStationAdminStatusChanged

        /// <summary>
        /// A delegate called whenever the aggregated admin status of any subordinated charging station changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingStation">The updated charging station.</param>
        /// <param name="OldStatus">The old timestamped status of the charging station.</param>
        /// <param name="NewStatus">The new timestamped status of the charging station.</param>
        public delegate void OnAggregatedChargingStationAdminStatusChangedDelegate(DateTime Timestamp, ChargingStation ChargingStation, Timestamped<ChargingStationAdminStatusType> OldStatus, Timestamped<ChargingStationAdminStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the aggregated admin status of any subordinated charging station changed.
        /// </summary>
        public event OnAggregatedChargingStationAdminStatusChangedDelegate OnAggregatedChargingStationAdminStatusChanged;

        #endregion

        #region OnChargingStationReserved

        /// <summary>
        /// A delegate called whenever a charging station was reserved.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this reservation was created.</param>
        /// <param name="Reservation">The charging reservation.</param>
        public delegate void OnChargingStationReservedDelegate(DateTime Timestamp, ChargingReservation Reservation);

        /// <summary>
        /// An event fired whenever a charging station was reserved.
        /// </summary>
        public event OnChargingStationReservedDelegate OnChargingStationReserved;

        #endregion

        #region EVSEAddition

        internal readonly IVotingNotificator<DateTime, ChargingStation, EVSE, Boolean> EVSEAddition;

        /// <summary>
        /// Called whenever an EVSE will be or was added.
        /// </summary>
        public IVotingSender<DateTime, ChargingStation, EVSE, Boolean> OnEVSEAddition
        {
            get
            {
                return EVSEAddition;
            }
        }

        #endregion

        #region EVSERemoval

        internal readonly IVotingNotificator<DateTime, ChargingStation, EVSE, Boolean> EVSERemoval;

        /// <summary>
        /// Called whenever an EVSE will be or was removed.
        /// </summary>
        public IVotingSender<DateTime, ChargingStation, EVSE, Boolean> OnEVSERemoval
        {
            get
            {
                return EVSERemoval;
            }
        }

        #endregion


        // EVSE events

        #region OnEVSEDataChanged

        /// <summary>
        /// A delegate called whenever the static data of any subordinated EVSE changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSE">The updated EVSE.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        public delegate void OnEVSEDataChangedDelegate(DateTime Timestamp, EVSE EVSE, String PropertyName, Object OldValue, Object NewValue);

        /// <summary>
        /// An event fired whenever the static data of any subordinated EVSE changed.
        /// </summary>
        public event OnEVSEDataChangedDelegate OnEVSEDataChanged;

        #endregion

        #region OnEVSEStatusChanged

        /// <summary>
        /// A delegate called whenever the dynamic status of any subordinated EVSE changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSE">The updated EVSE.</param>
        /// <param name="OldStatus">The old timestamped status of the EVSE.</param>
        /// <param name="NewStatus">The new timestamped status of the EVSE.</param>
        public delegate void OnEVSEStatusChangedDelegate(DateTime Timestamp, EVSE EVSE, Timestamped<EVSEStatusType> OldStatus, Timestamped<EVSEStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the dynamic status of any subordinated EVSE changed.
        /// </summary>
        public event OnEVSEStatusChangedDelegate OnEVSEStatusChanged;

        #endregion

        #region OnEVSEStatusDiff

        public delegate void OnEVSEStatusDiffDelegate(EVSEStatusDiff StatusDiff);

        /// <summary>
        /// An event fired whenever a EVSE status diff was received.
        /// </summary>
        public event OnEVSEStatusDiffDelegate OnEVSEStatusDiff;

        #endregion

        #region OnEVSEAdminStatusChanged

        /// <summary>
        /// A delegate called whenever the admin status of any subordinated EVSE changed.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSE">The updated EVSE.</param>
        /// <param name="OldStatus">The old timestamped admin status of the EVSE.</param>
        /// <param name="NewStatus">The new timestamped admin status of the EVSE.</param>
        public delegate void OnEVSEAdminStatusChangedDelegate(DateTime Timestamp, EVSE EVSE, Timestamped<EVSEAdminStatusType> OldStatus, Timestamped<EVSEAdminStatusType> NewStatus);

        /// <summary>
        /// An event fired whenever the admin status of any subordinated EVSE changed.
        /// </summary>
        public event OnEVSEAdminStatusChangedDelegate OnEVSEAdminStatusChanged;

        #endregion

        #region SocketOutletAddition

        internal readonly IVotingNotificator<DateTime, EVSE, SocketOutlet, Boolean> SocketOutletAddition;

        /// <summary>
        /// Called whenever a socket outlet will be or was added.
        /// </summary>
        public IVotingSender<DateTime, EVSE, SocketOutlet, Boolean> OnSocketOutletAddition
        {
            get
            {
                return SocketOutletAddition;
            }
        }

        #endregion

        #region SocketOutletRemoval

        internal readonly IVotingNotificator<DateTime, EVSE, SocketOutlet, Boolean> SocketOutletRemoval;

        /// <summary>
        /// Called whenever a socket outlet will be or was removed.
        /// </summary>
        public IVotingSender<DateTime, EVSE, SocketOutlet, Boolean> OnSocketOutletRemoval
        {
            get
            {
                return SocketOutletRemoval;
            }
        }

        #endregion

        #region OnEVSEReserved

        /// <summary>
        /// A delegate called whenever an EVSE was reserved.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this reservation was created.</param>
        /// <param name="Reservation">The charging reservation.</param>
        public delegate void OnEVSEReservedDelegate(DateTime Timestamp, ChargingReservation Reservation);

        /// <summary>
        /// An event fired whenever an EVSE was reserved.
        /// </summary>
        public event OnEVSEReservedDelegate OnEVSEReserved;

        #endregion


        #region OnRemoteStart

        /// <summary>
        /// An event fired whenever a remote start command was received.
        /// </summary>
        public event OnRemoteStartDelegate OnRemoteStart;

        #endregion

        #region OnRemoteStop

        /// <summary>
        /// An event fired whenever a remote stop command was received.
        /// </summary>
        public event OnRemoteStopDelegate OnRemoteStop;

        #endregion

        #region OnSendChargeDetailRecord

        /// <summary>
        /// An event fired whenever a charge detail record was received.
        /// </summary>
        public event SendChargeDetailRecordDelegate OnSendChargeDetailRecord;

        #endregion

        #region OnFilterCDRRecords

        public delegate SendCDRResult OnFilterCDRRecordsDelegate(Authorizator_Id AuthorizatorId, AuthInfo AuthInfo, ChargingSession_Id PartnerSessionId);

        /// <summary>
        /// An event fired whenever a CDR needs to be filtered.
        /// </summary>
        public event OnFilterCDRRecordsDelegate OnFilterCDRRecords;

        #endregion


        // Reservation events

        #region OnReservationExpired

        /// <summary>
        /// A delegate called whenever a reservation expired.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this reservation expired.</param>
        /// <param name="Reservation">The charging reservation.</param>
        public delegate void OnReservationExpiredDelegate(DateTime Timestamp, ChargingReservation Reservation);

        /// <summary>
        /// An event fired whenever a reservation expired.
        /// </summary>
        public event OnReservationExpiredDelegate OnReservationExpired;

        #endregion

        #region OnReservationDeleted

        /// <summary>
        /// A delegate called whenever a reservation was deleted.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this reservation was deleted.</param>
        /// <param name="Reservation">The charging reservation.</param>
        public delegate void OnReservationDeletedDelegate(DateTime Timestamp, ChargingReservation Reservation);

        /// <summary>
        /// An event fired whenever a reservation was deleted.
        /// </summary>
        public event OnReservationDeletedDelegate OnReservationDeleted;

        #endregion

        #endregion

        #region Constructor(s)

        #region RoamingNetwork()

        /// <summary>
        /// Create a new roaming network having a random
        /// roaming network identification.
        /// </summary>
        public RoamingNetwork()
            : this(RoamingNetwork_Id.New)
        { }

        #endregion

        #region RoamingNetwork(Id, AuthorizatorId = null)

        /// <summary>
        /// Create a new roaming network having the given
        /// unique roaming network identification.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming network.</param>
        /// <param name="AuthorizatorId">The unique identification for the Auth service.</param>
        public RoamingNetwork(RoamingNetwork_Id  Id,
                              Authorizator_Id    AuthorizatorId = null)

            : base(Id)

        {

            #region Init data and properties

            this._EVSEOperators         = new ConcurrentDictionary<EVSEOperator_Id,              EVSEOperator>();
            this._EVServiceProviders    = new ConcurrentDictionary<EVSP_Id,                      EVSP>();
            this._RoamingProviders      = new ConcurrentDictionary<RoamingProvider_Id,           RoamingProvider>();
            this._SearchProviders       = new ConcurrentDictionary<NavigationServiceProvider_Id, NavigationServiceProvider>();

            this._ChargingReservations  = new ConcurrentDictionary<ChargingReservation_Id, ChargingReservation>();

            this._Description           = new I18NString();

            #endregion

            this._AuthorizatorId              = (AuthorizatorId == null) ? Authorizator_Id.Parse("GraphDefined E-Mobility Gateway") : AuthorizatorId;
            this.AuthenticationServices       = new Dictionary<UInt32,                 IAuthServices>();
            this.SessionIdAuthenticatorCache  = new Dictionary<ChargingSession_Id,     IAuthServices>();


            #region Init events

            // RoamingNetwork events
            this.EVSEOperatorAddition       = new VotingNotificator<DateTime, RoamingNetwork,  EVSEOperator,              Boolean>(() => new VetoVote(), true);
            this.EVSEOperatorRemoval        = new VotingNotificator<DateTime, RoamingNetwork,  EVSEOperator,              Boolean>(() => new VetoVote(), true);

            this.EVServiceProviderAddition  = new VotingNotificator<RoamingNetwork,  EVSP,         Boolean>(() => new VetoVote(), true);
            this.EVServiceProviderRemoval   = new VotingNotificator<RoamingNetwork,  EVSP,         Boolean>(() => new VetoVote(), true);

            this.RoamingProviderAddition    = new VotingNotificator<RoamingNetwork,  RoamingProvider,           Boolean>(() => new VetoVote(), true);
            this.RoamingProviderRemoval     = new VotingNotificator<RoamingNetwork,  RoamingProvider,           Boolean>(() => new VetoVote(), true);

            this.SearchProviderAddition     = new VotingNotificator<RoamingNetwork,  NavigationServiceProvider, Boolean>(() => new VetoVote(), true);
            this.SearchProviderRemoval      = new VotingNotificator<RoamingNetwork,  NavigationServiceProvider, Boolean>(() => new VetoVote(), true);

            // EVSEOperator events
            this.ChargingPoolAddition       = new VotingNotificator<DateTime, EVSEOperator,    ChargingPool,              Boolean>(() => new VetoVote(), true);
            this.ChargingPoolRemoval        = new VotingNotificator<DateTime, EVSEOperator,    ChargingPool,              Boolean>(() => new VetoVote(), true);

            // ChargingPool events
            this.ChargingStationAddition    = new VotingNotificator<DateTime, ChargingPool,    ChargingStation,           Boolean>(() => new VetoVote(), true);
            this.ChargingStationRemoval     = new VotingNotificator<DateTime, ChargingPool,    ChargingStation,           Boolean>(() => new VetoVote(), true);

            // ChargingStation events
            this.EVSEAddition               = new VotingNotificator<DateTime, ChargingStation, EVSE,                      Boolean>(() => new VetoVote(), true);
            this.EVSERemoval                = new VotingNotificator<DateTime, ChargingStation, EVSE,                      Boolean>(() => new VetoVote(), true);

            // EVSE events
            this.SocketOutletAddition       = new VotingNotificator<DateTime, EVSE,            SocketOutlet,              Boolean>(() => new VetoVote(), true);
            this.SocketOutletRemoval        = new VotingNotificator<DateTime, EVSE,            SocketOutlet,              Boolean>(() => new VetoVote(), true);

            #endregion

        }

        #endregion

        #endregion


        #region EVSE operator methods

        #region CreateNewEVSEOperator(EVSEOperatorId, Name = null, Description = null, Configurator = null, OnSuccess = null, OnError = null)

        /// <summary>
        /// Create and register a new EVSE operator having the given
        /// unique EVSE operator identification.
        /// </summary>
        /// <param name="EVSEOperatorId">The unique identification of the new EVSE operator.</param>
        /// <param name="Name">The offical (multi-language) name of the EVSE Operator.</param>
        /// <param name="Description">An optional (multi-language) description of the EVSE Operator.</param>
        /// <param name="Configurator">An optional delegate to configure the new EVSE operator before its successful creation.</param>
        /// <param name="OnSuccess">An optional delegate to configure the new EVSE operator after its successful creation.</param>
        /// <param name="OnError">An optional delegate to be called whenever the creation of the EVSE operator failed.</param>
        public EVSEOperator CreateNewEVSEOperator(EVSEOperator_Id                          EVSEOperatorId,
                                                  I18NString                               Name           = null,
                                                  I18NString                               Description    = null,
                                                  Action<EVSEOperator>                     Configurator   = null,
                                                  Action<EVSEOperator>                     OnSuccess      = null,
                                                  Action<RoamingNetwork, EVSEOperator_Id>  OnError        = null)
        {

            #region Initial checks

            if (EVSEOperatorId == null)
                throw new ArgumentNullException("EVSEOperatorId", "The given EVSE operator identification must not be null!");

            if (_EVSEOperators.ContainsKey(EVSEOperatorId))
                throw new EVSEOperatorAlreadyExists(EVSEOperatorId, this.Id);

            #endregion

            var _EVSEOperator = new EVSEOperator(EVSEOperatorId, Name, Description, this);

            if (Configurator != null)
                Configurator(_EVSEOperator);

            if (EVSEOperatorAddition.SendVoting(DateTime.Now, this, _EVSEOperator))
            {
                if (_EVSEOperators.TryAdd(EVSEOperatorId, _EVSEOperator))
                {

                    _EVSEOperator.OnEVSEDataChanged                             += (Timestamp, EVSE, PropertyName, OldValue, NewValue)
                                                                                    => UpdateEVSEData(Timestamp, EVSE, PropertyName, OldValue, NewValue);

                    _EVSEOperator.OnEVSEStatusChanged                           += (Timestamp, EVSE, OldStatus, NewStatus)
                                                                                    => UpdateEVSEStatus(Timestamp, EVSE, OldStatus, NewStatus);

                    _EVSEOperator.OnEVSEAdminStatusChanged                      += (Timestamp, EVSE, OldStatus, NewStatus)
                                                                                    => UpdateEVSEAdminStatus(Timestamp, EVSE, OldStatus, NewStatus);


                    _EVSEOperator.OnChargingStationDataChanged                  += (Timestamp, ChargingStation, PropertyName, OldValue, NewValue)
                                                                                    => UpdateChargingStationData(Timestamp, ChargingStation, PropertyName, OldValue, NewValue);

                    _EVSEOperator.OnChargingStationAdminStatusChanged           += (Timestamp, ChargingStation, OldStatus, NewStatus)
                                                                                    => UpdateChargingStationAdminStatus(Timestamp, ChargingStation, OldStatus, NewStatus);

                    _EVSEOperator.OnAggregatedChargingStationStatusChanged      += (Timestamp, ChargingStation, OldStatus, NewStatus)
                                                                                    => UpdateAggregatedChargingStationStatus(Timestamp, ChargingStation, OldStatus, NewStatus);

                    _EVSEOperator.OnAggregatedChargingStationAdminStatusChanged += (Timestamp, ChargingStation, OldStatus, NewStatus)
                                                                                    => UpdateChargingStationAdminStatus(Timestamp, ChargingStation, OldStatus, NewStatus);


                    _EVSEOperator.OnChargingPoolDataChanged                     += (Timestamp, ChargingPool, PropertyName, OldValue, NewValue)
                                                                                    => UpdateChargingPoolData(Timestamp, ChargingPool, PropertyName, OldValue, NewValue);

                    _EVSEOperator.OnChargingPoolAdminStatusChanged              += (Timestamp, ChargingPool, OldStatus, NewStatus)
                                                                                    => UpdateChargingPoolAdminStatus(Timestamp, ChargingPool, OldStatus, NewStatus);

                    _EVSEOperator.OnAggregatedChargingPoolStatusChanged         += (Timestamp, ChargingPool, OldStatus, NewStatus)
                                                                                    => UpdateChargingPoolStatus(Timestamp, ChargingPool, OldStatus, NewStatus);

                    _EVSEOperator.OnAggregatedChargingPoolAdminStatusChanged    += (Timestamp, ChargingPool, OldStatus, NewStatus)
                                                                                    => UpdateChargingPoolAdminStatus(Timestamp, ChargingPool, OldStatus, NewStatus);


                    _EVSEOperator.OnPropertyChanged                             += (Timestamp, Sender, PropertyName, OldValue, NewValue)
                                                                                    => UpdateEVSEOperatorData(Timestamp, Sender as EVSEOperator, PropertyName, OldValue, NewValue);

                    _EVSEOperator.OnAggregatedStatusChanged                     += (Timestamp, EVSEOperator, OldStatus, NewStatus)
                                                                                    => UpdateStatus(Timestamp, EVSEOperator, OldStatus, NewStatus);

                    _EVSEOperator.OnAggregatedAdminStatusChanged                += (Timestamp, EVSEOperator, OldStatus, NewStatus)
                                                                                    => UpdateAdminStatus(Timestamp, EVSEOperator, OldStatus, NewStatus);


                    OnSuccess.FailSafeInvoke(_EVSEOperator);
                    EVSEOperatorAddition.SendNotification(DateTime.Now, this, _EVSEOperator);
                    return _EVSEOperator;

                }
            }

            throw new Exception();

        }

        #endregion

        #region ContainsEVSEOperator(EVSEOperator)

        /// <summary>
        /// Check if the given EVSEOperator is already present within the roaming network.
        /// </summary>
        /// <param name="EVSEOperator">An EVSE operator.</param>
        public Boolean ContainsEVSEOperator(EVSEOperator EVSEOperator)
        {
            return _EVSEOperators.ContainsKey(EVSEOperator.Id);
        }

        #endregion

        #region ContainsEVSEOperator(EVSEOperatorId)

        /// <summary>
        /// Check if the given EVSEOperator identification is already present within the roaming network.
        /// </summary>
        /// <param name="EVSEOperatorId">The unique identification of the EVSE operator.</param>
        public Boolean ContainsEVSEOperator(EVSEOperator_Id EVSEOperatorId)
        {
            return _EVSEOperators.ContainsKey(EVSEOperatorId);
        }

        #endregion

        #region GetEVSEOperatorbyId(EVSEOperatorId)

        public EVSEOperator GetEVSEOperatorbyId(EVSEOperator_Id EVSEOperatorId)
        {

            EVSEOperator _EVSEOperator = null;

            if (_EVSEOperators.TryGetValue(EVSEOperatorId, out _EVSEOperator))
                return _EVSEOperator;

            return null;

        }

        #endregion

        #region TryGetEVSEOperatorbyId(EVSEOperatorId, out EVSEOperator)

        public Boolean TryGetEVSEOperatorbyId(EVSEOperator_Id EVSEOperatorId, out EVSEOperator EVSEOperator)
        {
            return _EVSEOperators.TryGetValue(EVSEOperatorId, out EVSEOperator);
        }

        #endregion

        #region RemoveEVSEOperator(EVSEOperatorId)

        public EVSEOperator RemoveEVSEOperator(EVSEOperator_Id EVSEOperatorId)
        {

            EVSEOperator _EVSEOperator = null;

            if (_EVSEOperators.TryRemove(EVSEOperatorId, out _EVSEOperator))
                return _EVSEOperator;

            return null;

        }

        #endregion

        #region TryRemoveEVSEOperator(EVSEOperatorId, out EVSEOperator)

        public Boolean TryRemoveEVSEOperator(EVSEOperator_Id EVSEOperatorId, out EVSEOperator EVSEOperator)
        {
            return _EVSEOperators.TryRemove(EVSEOperatorId, out EVSEOperator);
        }

        #endregion

        #endregion

        #region CreateNewEVServiceProvider(EVServiceProviderId, Action = null)

        /// <summary>
        /// Create and register a new electric vehicle service provider having the given
        /// unique electric vehicle service provider identification.
        /// </summary>
        /// <param name="EVServiceProviderId">The unique identification of the new roaming provider.</param>
        /// <param name="Action">An optional delegate to configure the new roaming provider after its creation.</param>
        public EVSP CreateNewEVServiceProvider(EVSP_Id       EVServiceProviderId,
                                               Action<EVSP>  Action  = null)
        {

            #region Initial checks

            if (EVServiceProviderId == null)
                throw new ArgumentNullException("EVServiceProviderId", "The given electric vehicle service provider identification must not be null!");

            if (_EVServiceProviders.ContainsKey(EVServiceProviderId))
                throw new EVServiceProviderAlreadyExists(EVServiceProviderId, this.Id);

            #endregion

            var _EVServiceProvider = new EVSP(EVServiceProviderId, this);

            Action.FailSafeInvoke(_EVServiceProvider);

            if (EVServiceProviderAddition.SendVoting(this, _EVServiceProvider))
            {
                if (_EVServiceProviders.TryAdd(EVServiceProviderId, _EVServiceProvider))
                {
                    EVServiceProviderAddition.SendNotification(this, _EVServiceProvider);
                    return _EVServiceProvider;
                }
            }

            throw new Exception();

        }

        #endregion

        #region CreateNewRoamingProvider(RoamingProviderId, EMobilityService, Action = null)

        /// <summary>
        /// Create and register a new electric vehicle roaming provider having the given
        /// unique electric vehicle roaming provider identification.
        /// </summary>
        /// <param name="RoamingProviderId">The unique identification of the new roaming provider.</param>
        /// <param name="EMobilityService">The attached E-Mobility service.</param>
        /// <param name="Action">An optional delegate to configure the new roaming provider after its creation.</param>
        public RoamingProvider CreateNewRoamingProvider(RoamingProvider_Id       RoamingProviderId,
                                                        IAuthServices            EMobilityService,
                                                        Action<RoamingProvider>  Action = null)
        {

            #region Initial checks

            if (RoamingProviderId == null)
                throw new ArgumentNullException("RoamingProviderId", "The given roaming provider identification must not be null!");

            if (_RoamingProviders.ContainsKey(RoamingProviderId))
                throw new RoamingProviderAlreadyExists(RoamingProviderId, this.Id);

            #endregion

            var _RoamingProvider = new RoamingProvider(RoamingProviderId, this, EMobilityService);

            Action.FailSafeInvoke(_RoamingProvider);

            if (RoamingProviderAddition.SendVoting(this, _RoamingProvider))
            {
                if (_RoamingProviders.TryAdd(RoamingProviderId, _RoamingProvider))
                {
                    RoamingProviderAddition.SendNotification(this, _RoamingProvider);
                    return _RoamingProvider;
                }
            }

            throw new Exception();

        }

        #endregion

        #region CreateNewSearchProvider(NavigationServiceProviderId, Action = null)

        /// <summary>
        /// Create and register a new navigation service provider having the given
        /// unique identification.
        /// </summary>
        /// <param name="NavigationServiceProviderId">The unique identification of the new search provider.</param>
        /// <param name="Action">An optional delegate to configure the new search provider after its creation.</param>
        public NavigationServiceProvider CreateNewSearchProvider(NavigationServiceProvider_Id       NavigationServiceProviderId,
                                                                 Action<NavigationServiceProvider>  Action = null)
        {

            #region Initial checks

            if (NavigationServiceProviderId == null)
                throw new ArgumentNullException("NavigationServiceProviderId", "The given navigation service provider identification must not be null!");

            if (_SearchProviders.ContainsKey(NavigationServiceProviderId))
                throw new SearchProviderAlreadyExists(NavigationServiceProviderId, this.Id);

            #endregion

            var _SearchProvider = new NavigationServiceProvider(NavigationServiceProviderId, this);

            Action.FailSafeInvoke(_SearchProvider);

            if (SearchProviderAddition.SendVoting(this, _SearchProvider))
            {
                if (_SearchProviders.TryAdd(NavigationServiceProviderId, _SearchProvider))
                {
                    SearchProviderAddition.SendNotification(this, _SearchProvider);
                    return _SearchProvider;
                }
            }

            throw new Exception();

        }

        #endregion


        #region EVSE methods

        #region ContainsEVSE(EVSE)

        /// <summary>
        /// Check if the given EVSE is already present within the roaming network.
        /// </summary>
        /// <param name="EVSE">An EVSE.</param>
        public Boolean ContainsEVSE(EVSE EVSE)
        {
            return _EVSEOperators.Values.Any(evseoperator => evseoperator.ContainsEVSE(EVSE.Id));
        }

        #endregion

        #region ContainsEVSE(EVSEId)

        /// <summary>
        /// Check if the given EVSE identification is already present within the roaming network.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification.</param>
        public Boolean ContainsEVSE(EVSE_Id EVSEId)
        {
            return _EVSEOperators.Values.Any(evseoperator => evseoperator.ContainsEVSE(EVSEId));
        }

        #endregion

        #region GetEVSEbyId(EVSEId)

        public EVSE GetEVSEbyId(EVSE_Id EVSEId)
        {

            EVSE _EVSE = null;

            foreach (var EVSEOperator in _EVSEOperators.Values)
                if (EVSEOperator.TryGetEVSEbyId(EVSEId, out _EVSE))
                    return _EVSE;

            return null;

        }

        #endregion

        #region TryGetEVSEbyId(EVSEId, out EVSE)

        public Boolean TryGetEVSEbyId(EVSE_Id EVSEId, out EVSE EVSE)
        {

            foreach (var EVSEOperator in _EVSEOperators.Values)
                if (EVSEOperator.TryGetEVSEbyId(EVSEId, out EVSE))
                    return true;

            EVSE = null;
            return false;

        }

        #endregion


        #region ReserveEVSE(...)

        public async Task<ReservationResult> ReserveEVSE(DateTime                Timestamp,
                                                         CancellationToken       CancellationToken,
                                                         EVSP_Id                 ProviderId,
                                                         ChargingReservation_Id  ReservationId,
                                                         DateTime?               StartTime,
                                                         TimeSpan?               Duration,
                                                         EVSE_Id                 EVSEId,
                                                         ChargingProduct_Id      ChargingProductId  = null,
                                                         IEnumerable<Auth_Token> RFIDIds            = null,
                                                         IEnumerable<eMA_Id>     eMAIds             = null,
                                                         IEnumerable<UInt32>     PINs               = null)
        {

            #region Try to remove an existing reservation if this is an update!

            if (ReservationId != null)
            {

                ChargingReservation _Reservation = null;

                if (!_ChargingReservations.TryRemove(ReservationId, out _Reservation))
                    return ReservationResult.UnknownChargingReservationId;

            }

            #endregion

            EVSE _EVSE;

            if (!TryGetEVSEbyId(EVSEId, out _EVSE))
                return ReservationResult.UnknownEVSE;

            var result = await _EVSE.Reserve(Timestamp,
                                             CancellationToken,
                                             ProviderId,
                                             ReservationId,
                                             StartTime,
                                             Duration,
                                             ChargingProductId,
                                             RFIDIds,
                                             eMAIds,
                                             PINs);

            if (result.Result == ReservationResultType.Success)
                _ChargingReservations.TryAdd(result.Reservation.Id, result.Reservation);

            return result;

        }

        #endregion

        #endregion

        #region ChargingStation methods

        #region ContainsChargingStation(ChargingStation)

        /// <summary>
        /// Check if the given ChargingStation is already present within the roaming network.
        /// </summary>
        /// <param name="ChargingStation">An ChargingStation.</param>
        public Boolean ContainsChargingStation(ChargingStation ChargingStation)
        {
            return _EVSEOperators.Values.Any(evseoperator => evseoperator.ContainsChargingStation(ChargingStation.Id));
        }

        #endregion

        #region ContainsChargingStation(ChargingStationId)

        /// <summary>
        /// Check if the given ChargingStation identification is already present within the roaming network.
        /// </summary>
        /// <param name="ChargingStationId">An ChargingStation identification.</param>
        public Boolean ContainsChargingStation(ChargingStation_Id ChargingStationId)
        {
            return _EVSEOperators.Values.Any(evseoperator => evseoperator.ContainsChargingStation(ChargingStationId));
        }

        #endregion

        #region GetChargingStationbyId(ChargingStationId)

        public ChargingStation GetChargingStationbyId(ChargingStation_Id ChargingStationId)
        {

            ChargingStation _ChargingStation = null;

            foreach (var ChargingStationOperator in _EVSEOperators.Values)
                if (ChargingStationOperator.TryGetChargingStationbyId(ChargingStationId, out _ChargingStation))
                    return _ChargingStation;

            return null;

        }

        #endregion

        #region TryGetChargingStationbyId(ChargingStationId, out ChargingStation)

        public Boolean TryGetChargingStationbyId(ChargingStation_Id ChargingStationId, out ChargingStation ChargingStation)
        {

            foreach (var ChargingStationOperator in _EVSEOperators.Values)
                if (ChargingStationOperator.TryGetChargingStationbyId(ChargingStationId, out ChargingStation))
                    return true;

            ChargingStation = null;
            return false;

        }

        #endregion


        #region ReserveChargingStation(...)

        public async Task<ReservationResult> ReserveChargingStation(DateTime                Timestamp,
                                                                    CancellationToken       CancellationToken,
                                                                    EVSP_Id                 ProviderId,
                                                                    ChargingReservation_Id  ReservationId,
                                                                    DateTime?               StartTime,
                                                                    TimeSpan?               Duration,
                                                                    ChargingStation_Id      ChargingStationId,
                                                                    ChargingProduct_Id      ChargingProductId  = null,
                                                                    IEnumerable<Auth_Token> RFIDIds            = null,
                                                                    IEnumerable<eMA_Id>     eMAIds             = null,
                                                                    IEnumerable<UInt32>     PINs               = null)
        {

            #region Try to remove an existing reservation if this is an update!

            if (ReservationId != null)
            {

                ChargingReservation _Reservation = null;

                if (!_ChargingReservations.TryRemove(ReservationId, out _Reservation))
                    return ReservationResult.UnknownChargingReservationId;

            }

            #endregion

            ChargingStation _ChargingStation;

            if (!TryGetChargingStationbyId(ChargingStationId, out _ChargingStation))
                return ReservationResult.UnknownChargingStation;

            // Find a matching EVSE within the given charging station
            var _EVSE = _ChargingStation.EVSEs.
                            Where  (evse => evse.Status.Value == EVSEStatusType.Available).
                            OrderBy(evse => evse.Id).
                            FirstOrDefault();

            if (_EVSE != null)
            {

                var _Reservation = new ChargingReservation(Timestamp,
                                                           StartTime.HasValue ? StartTime.Value : DateTime.Now,
                                                           Duration. HasValue ? Duration. Value : MaxReservationDuration,
                                                           ProviderId,
                                                           ChargingReservationType.AtChargingStation,
                                                           _EVSE.ChargingStation.ChargingPool.EVSEOperator.RoamingNetwork,
                                                           _EVSE.ChargingStation.ChargingPool.Id,
                                                           _EVSE.ChargingStation.Id,
                                                           _EVSE.Id,
                                                           ChargingProductId,
                                                           RFIDIds,
                                                           eMAIds,
                                                           PINs);

                _EVSE.Reservation = _ChargingReservations.AddAndReturnValue(_Reservation.Id, _Reservation);

                return ReservationResult.Success(_Reservation);

            }

            return ReservationResult.NoEVSEsAvailable;

        }

        #endregion

        #endregion

        #region ChargingPool methods

        #region ContainsChargingPool(ChargingPool)

        /// <summary>
        /// Check if the given ChargingPool is already present within the roaming network.
        /// </summary>
        /// <param name="ChargingPool">An ChargingPool.</param>
        public Boolean ContainsChargingPool(ChargingPool ChargingPool)
        {
            return _EVSEOperators.Values.Any(evseoperator => evseoperator.ContainsChargingPool(ChargingPool.Id));
        }

        #endregion

        #region ContainsChargingPool(ChargingPoolId)

        /// <summary>
        /// Check if the given ChargingPool identification is already present within the roaming network.
        /// </summary>
        /// <param name="ChargingPoolId">An ChargingPool identification.</param>
        public Boolean ContainsChargingPool(ChargingPool_Id ChargingPoolId)
        {
            return _EVSEOperators.Values.Any(evseoperator => evseoperator.ContainsChargingPool(ChargingPoolId));
        }

        #endregion

        #region GetChargingPoolbyId(ChargingPoolId)

        public ChargingPool GetChargingPoolbyId(ChargingPool_Id ChargingPoolId)
        {

            ChargingPool _ChargingPool = null;

            foreach (var ChargingPoolOperator in _EVSEOperators.Values)
                if (ChargingPoolOperator.TryGetChargingPoolbyId(ChargingPoolId, out _ChargingPool))
                    return _ChargingPool;

            return null;

        }

        #endregion

        #region TryGetChargingPoolbyId(ChargingPoolId, out ChargingPool)

        public Boolean TryGetChargingPoolbyId(ChargingPool_Id ChargingPoolId, out ChargingPool ChargingPool)
        {

            foreach (var ChargingPoolOperator in _EVSEOperators.Values)
                if (ChargingPoolOperator.TryGetChargingPoolbyId(ChargingPoolId, out ChargingPool))
                    return true;

            ChargingPool = null;
            return false;

        }

        #endregion


        #region ReserveChargingPool(...)

        public async Task<ReservationResult> ReserveChargingPool(DateTime                Timestamp,
                                                                 CancellationToken       CancellationToken,
                                                                 EVSP_Id                 ProviderId,
                                                                 ChargingReservation_Id  ReservationId,
                                                                 DateTime?               StartTime,
                                                                 TimeSpan?               Duration,
                                                                 ChargingPool_Id         ChargingPoolId,
                                                                 ChargingProduct_Id      ChargingProductId  = null,
                                                                 IEnumerable<Auth_Token> RFIDIds            = null,
                                                                 IEnumerable<eMA_Id>     eMAIds             = null,
                                                                 IEnumerable<UInt32>     PINs               = null)
        {

            #region Try to remove an existing reservation if this is an update!

            if (ReservationId != null)
            {

                ChargingReservation _Reservation = null;

                if (!_ChargingReservations.TryRemove(ReservationId, out _Reservation))
                    return ReservationResult.UnknownChargingReservationId;

            }

            #endregion

            ChargingPool _ChargingPool;

            if (!TryGetChargingPoolbyId(ChargingPoolId, out _ChargingPool))
                return ReservationResult.UnknownChargingPool;

            // Find a matching EVSE within the given charging pool
            var _EVSE = _ChargingPool.EVSEs.
                            Where  (evse => evse.Status.Value == EVSEStatusType.Available).
                            OrderBy(evse => evse.Id).
                            FirstOrDefault();

            if (_EVSE != null)
            {

                var _Reservation = new ChargingReservation(Timestamp,
                                                           StartTime.HasValue ? StartTime.Value : DateTime.Now,
                                                           Duration. HasValue ? Duration. Value : MaxReservationDuration,
                                                           ProviderId,
                                                           ChargingReservationType.AtChargingPool,
                                                           _EVSE.ChargingStation.ChargingPool.EVSEOperator.RoamingNetwork,
                                                           _EVSE.ChargingStation.ChargingPool.Id,
                                                           _EVSE.ChargingStation.Id,
                                                           _EVSE.Id,
                                                           ChargingProductId,
                                                           RFIDIds,
                                                           eMAIds,
                                                           PINs);

                _EVSE.Reservation = _ChargingReservations.AddAndReturnValue(_Reservation.Id, _Reservation);

                return ReservationResult.Success(_Reservation);

            }

            return ReservationResult.NoEVSEsAvailable;

        }

        #endregion

        #endregion

        #region Reservation methods

        #region TryGetChargingReservationbyId(ChargingReservationId, out ChargingReservation)

        public Boolean TryGetChargingReservationbyId(ChargingReservation_Id ChargingReservationId, out ChargingReservation ChargingReservation)
        {
            return _ChargingReservations.TryGetValue(ChargingReservationId, out ChargingReservation);
        }

        #endregion

        #region Remove(ChargingReservation)

        public Boolean Remove(ChargingReservation_Id ChargingReservationId)
        {

            ChargingReservation _ChargingReservation;

            if (_ChargingReservations.TryRemove(ChargingReservationId, out _ChargingReservation))
            {

                EVSE _EVSE = null;

                if (TryGetEVSEbyId(_ChargingReservation.EVSEId, out _EVSE))
                    _EVSE.Reservation = null;

                return true;

            }

            return false;

        }

        #endregion

        #endregion



        #region RegisterAuthService(Priority, AuthenticationService)

        public Boolean RegisterAuthService(UInt32         Priority,
                                           IAuthServices  AuthenticationService)
        {

            lock (AuthenticationServices)
            {

                if (!AuthenticationServices.ContainsKey(Priority))
                {
                    AuthenticationServices.Add(Priority, AuthenticationService);
                    return true;
                }

                return false;

            }

        }

        #endregion


        #region AuthorizeStart(OperatorId, AuthToken, EVSEId = null, SessionId = null, PartnerProductId = null, PartnerSessionId = null)

        /// <summary>
        /// Create an authorize start request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">An optional EVSE identification.</param>
        /// <param name="SessionId">An optional session identification.</param>
        /// <param name="PartnerProductId">An optional partner product identification.</param>
        /// <param name="PartnerSessionId">An optional partner session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<HTTPResponse<AuthStartResult>>

            AuthorizeStart(EVSEOperator_Id     OperatorId,
                           Auth_Token          AuthToken,
                           EVSE_Id             EVSEId            = null,
                           ChargingSession_Id  SessionId         = null,
                           ChargingProduct_Id  PartnerProductId  = null,
                           ChargingSession_Id  PartnerSessionId  = null,
                           TimeSpan?           QueryTimeout      = null)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException("OperatorId", "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException("AuthToken",  "The given parameter must not be null!");

            #endregion

            // Will store the SessionId in order to contact the right authenticator at later requests!

            lock (AuthenticationServices)
            {

                AuthStartResult AuthStartResult;

                foreach (var AuthenticationService in AuthenticationServices.
                                                          OrderBy(AuthServiceWithPriority => AuthServiceWithPriority.Key).
                                                          Select (AuthServiceWithPriority => AuthServiceWithPriority.Value))
                {

                    var _Task = AuthenticationService.AuthorizeStart(OperatorId,
                                                                     AuthToken,
                                                                     EVSEId,
                                                                     SessionId,
                                                                     PartnerProductId,
                                                                     PartnerSessionId,
                                                                     QueryTimeout);

                    _Task.Wait(TimeSpan.FromSeconds(30));

                    AuthStartResult = _Task.Result.Content;

                    #region Authorized

                    if (AuthStartResult.AuthorizationResult == AuthorizeStartResultType.Success)
                    {

                        // Store the upstream SessionId and its AuthenticationService!
                        // Will be deleted when the CDRecord was sent!
                        SessionIdAuthenticatorCache.Add(AuthStartResult.SessionId, AuthenticationService);

                        return new HTTPResponse<AuthStartResult>(_Task.Result.HttpResponse,
                                                                 AuthStartResult);

                    }

                    #endregion

                    #region Blocked

                    //else if (AuthStartResult.AuthorizationResult == AuthorizationResultType.Blocked)
                    //    return new HTTPResponse<AuthStartResult>(_Task.Result.HttpResponse,
                    //                                             AuthStartResult);

                    #endregion

                }

                #region ...else fail!

                return new HTTPResponse<AuthStartResult>(new HTTPResponse(),
                                                         new AuthStartResult(AuthorizatorId) {
                                                             AuthorizationResult  = AuthorizeStartResultType.Error,
                                                             Description          = "No authorization service returned a positiv result!"
                                                         });

                #endregion

            }

        }

        #endregion

        #region AuthorizeStop(OperatorId, SessionId, AuthToken, EVSEId = null, PartnerSessionId = null)

        /// <summary>
        /// Create an authorize stop request.
        /// </summary>
        /// <param name="OperatorId">An EVSE operator identification.</param>
        /// <param name="SessionId">The session identification from the AuthorizeStart request.</param>
        /// <param name="AuthToken">A (RFID) user identification.</param>
        /// <param name="EVSEId">An optional EVSE identification.</param>
        /// <param name="PartnerSessionId">An optional partner session identification.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<HTTPResponse<AuthStopResult>>

            AuthorizeStop(EVSEOperator_Id      OperatorId,
                          ChargingSession_Id   SessionId,
                          Auth_Token           AuthToken,
                          EVSE_Id              EVSEId            = null,   // OICP v2.0: Optional
                          ChargingSession_Id   PartnerSessionId  = null,   // OICP v2.0: Optional [50]
                          TimeSpan?            QueryTimeout      = null)

        {

            #region Initial checks

            if (OperatorId == null)
                throw new ArgumentNullException("OperatorId", "The given parameter must not be null!");

            if (SessionId  == null)
                throw new ArgumentNullException("SessionId",  "The given parameter must not be null!");

            if (AuthToken  == null)
                throw new ArgumentNullException("AuthToken",  "The given parameter must not be null!");

            #endregion

            lock (AuthenticationServices)
            {

                #region An authenticator was found for the upstream SessionId!

                IAuthServices AuthenticationService;

                if (SessionIdAuthenticatorCache.TryGetValue(SessionId, out AuthenticationService))
                {

                    var _Task = AuthenticationService.AuthorizeStop(OperatorId, SessionId, AuthToken, EVSEId, PartnerSessionId);
                    _Task.Wait();

                    if (_Task.Result.Content.AuthorizationResult == AuthorizeStopResultType.Success)// ||
                        //_Task.Result.Content.AuthorizationResult == AuthorizationResultType.Blocked)
                        return _Task.Result;

                }

                #endregion

                #region Try to find anyone who might kown anything about the given SessionId!

                foreach (var OtherAuthenticationService in AuthenticationServices.
                                                               OrderBy(AuthServiceWithPriority => AuthServiceWithPriority.Key).
                                                               Select (AuthServiceWithPriority => AuthServiceWithPriority.Value).
                                                               ToArray())
                {

                    var _Task = OtherAuthenticationService.AuthorizeStop(OperatorId,
                                                                         SessionId,
                                                                         AuthToken,
                                                                         EVSEId,
                                                                         PartnerSessionId,
                                                                         QueryTimeout);
                    _Task.Wait();

                    if (_Task.Result.Content.AuthorizationResult == AuthorizeStopResultType.Success) // ||
                        //_Task.Result.Content.AuthorizationResult == AuthorizationResultType.Blocked)
                        return _Task.Result;

                }

                #endregion

                #region ...else fail!

                return new HTTPResponse<AuthStopResult>(new HTTPResponse(),
                                                        new AuthStopResult(AuthorizatorId) {
                                                            AuthorizationResult  = AuthorizeStopResultType.Error,
                                                            Description          = "No authorization service returned a positiv result!"
                                                        });

                #endregion

            }

        }

        #endregion

        #region SendChargeDetailRecord(EVSEId, SessionId, PartnerProductId, SessionStart, SessionEnd, AuthToken = null, eMAId = null, PartnerSessionId = null, ..., QueryTimeout = null)

        /// <summary>
        /// Create a SendChargeDetailRecord request.
        /// </summary>
        /// <param name="EVSEId">An EVSE identification.</param>
        /// <param name="SessionId">The session identification from the Authorize Start request.</param>
        /// <param name="PartnerProductId"></param>
        /// <param name="SessionStart">The timestamp of the session start.</param>
        /// <param name="SessionEnd">The timestamp of the session end.</param>
        /// <param name="AuthToken">An optional (RFID) user identification.</param>
        /// <param name="eMAId">An optional e-Mobility account identification.</param>
        /// <param name="PartnerSessionId">An optional partner session identification.</param>
        /// <param name="ChargingStart">An optional charging start timestamp.</param>
        /// <param name="ChargingEnd">An optional charging end timestamp.</param>
        /// <param name="MeterValueStart">An optional initial value of the energy meter.</param>
        /// <param name="MeterValueEnd">An optional final value of the energy meter.</param>
        /// <param name="MeterValuesInBetween">An optional enumeration of meter values during the charging session.</param>
        /// <param name="ConsumedEnergy">The optional amount of consumed energy.</param>
        /// <param name="MeteringSignature">An optional signature for the metering values.</param>
        /// <param name="HubOperatorId">An optional identification of the hub operator.</param>
        /// <param name="HubProviderId">An optional identification of the hub provider.</param>
        /// <param name="QueryTimeout">An optional timeout for this query.</param>
        public async Task<HTTPResponse<SendCDRResult>>

            SendChargeDetailRecord(EVSE_Id              EVSEId,
                                   ChargingSession_Id   SessionId,
                                   ChargingProduct_Id   PartnerProductId,
                                   DateTime             SessionStart,
                                   DateTime             SessionEnd,
                                   AuthInfo             AuthInfo,
                                   ChargingSession_Id   PartnerSessionId      = null,
                                   DateTime?            ChargingStart         = null,
                                   DateTime?            ChargingEnd           = null,
                                   Double?              MeterValueStart       = null,
                                   Double?              MeterValueEnd         = null,
                                   IEnumerable<Double>  MeterValuesInBetween  = null,
                                   Double?              ConsumedEnergy        = null,
                                   String               MeteringSignature     = null,
                                   HubOperator_Id       HubOperatorId         = null,
                                   EVSP_Id              HubProviderId         = null,
                                   TimeSpan?            QueryTimeout          = null)

        {

            #region Initial checks

            if (EVSEId           == null)
                throw new ArgumentNullException("EVSEId",            "The given parameter must not be null!");

            if (SessionId        == null)
                throw new ArgumentNullException("SessionId",         "The given parameter must not be null!");

            if (PartnerProductId == null)
                throw new ArgumentNullException("PartnerProductId",  "The given parameter must not be null!");

            if (SessionStart     == null)
                throw new ArgumentNullException("SessionStart",      "The given parameter must not be null!");

            if (SessionEnd       == null)
                throw new ArgumentNullException("SessionEnd",        "The given parameter must not be null!");

            if (AuthInfo         == null)
                throw new ArgumentNullException("AuthInfo",          "The given parameter must not be null!");

            #endregion

            lock (AuthenticationServices)
            {

                #region Some CDR should perhaps be filtered...

                var OnFilterCDRRecordsLocal = OnFilterCDRRecords;
                if (OnFilterCDRRecordsLocal != null)
                {

                    var _SENDCDRResult = OnFilterCDRRecordsLocal(AuthorizatorId, AuthInfo, PartnerSessionId);

                    if (_SENDCDRResult != null)
                        return new HTTPResponse<SendCDRResult>(new HTTPResponse(),
                                                               _SENDCDRResult);

                }

                #endregion

                IAuthServices  AuthenticationService;

                #region An authenticator was found for the upstream SessionId!

                if (SessionIdAuthenticatorCache.TryGetValue(SessionId, out AuthenticationService))
                {

                    var _Task = AuthenticationService.SendChargeDetailRecord(EVSEId,
                                                                             SessionId,
                                                                             PartnerProductId,
                                                                             SessionStart,
                                                                             SessionEnd,
                                                                             AuthInfo,
                                                                             PartnerSessionId,
                                                                             ChargingStart,
                                                                             ChargingEnd,
                                                                             MeterValueStart,
                                                                             MeterValueEnd,
                                                                             MeterValuesInBetween,
                                                                             ConsumedEnergy,
                                                                             MeteringSignature,
                                                                             HubOperatorId,
                                                                             HubProviderId,
                                                                             QueryTimeout);

                    _Task.Wait();

                    if (_Task.Result.Content.Result == SendCDRResultType.Forwarded)
                    {
                        SessionIdAuthenticatorCache.Remove(SessionId);
                        return _Task.Result;
                    }

                }

                #endregion

                #region Try to find anyone who might kown anything about the given SessionId!

                foreach (var OtherAuthenticationService in AuthenticationServices.
                                                               OrderBy(v => v.Key).
                                                               Select(v => v.Value).
                                                               ToArray())
                {

                    var _Task = OtherAuthenticationService.SendChargeDetailRecord(EVSEId,
                                                                                  SessionId,
                                                                                  PartnerProductId,
                                                                                  SessionStart,
                                                                                  SessionEnd,
                                                                                  AuthInfo,
                                                                                  PartnerSessionId,
                                                                                  ChargingStart,
                                                                                  ChargingEnd,
                                                                                  MeterValueStart,
                                                                                  MeterValueEnd,
                                                                                  MeterValuesInBetween,
                                                                                  ConsumedEnergy,
                                                                                  MeteringSignature,
                                                                                  HubOperatorId,
                                                                                  HubProviderId,
                                                                                  QueryTimeout);

                    _Task.Wait();

                    if (_Task.Result.Content.Result == SendCDRResultType.Forwarded)
                    {
                        SessionIdAuthenticatorCache.Remove(SessionId);
                        return _Task.Result;
                    }

                }

                #endregion

                #region ...else fail!

                return new HTTPResponse<SendCDRResult>(new HTTPResponse(),
                                                       SendCDRResult.False(AuthorizatorId,
                                                                           "No authorization service returned a positiv result!"));

                #endregion

            }

        }

        #endregion


        #region RemoteStart(Timestamp, RoamingNetworkId, SessionId, PartnerSessionId, ProviderId, eMAId, EVSEId, ChargingProductId)

        /// <summary>
        /// Initiate a remote start of the given charging session at the given EVSE
        /// and for the given Provider/eMAId.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="PartnerSessionId">The unique identification for this charging session on the partner side.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider.</param>
        /// <param name="eMAId">The unique identification of the e-mobility account.</param>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <param name="ChargingProductId">The unique identification of the choosen charging product at the given EVSE.</param>
        /// <returns>A remote start result object.</returns>
        public async Task<HTTPResponse<RemoteStartResult>>  RemoteStart(DateTime            Timestamp,
                                                                        RoamingNetwork_Id   RoamingNetworkId,
                                                                        ChargingSession_Id  SessionId,
                                                                        ChargingSession_Id  PartnerSessionId,
                                                                        EVSP_Id             ProviderId,
                                                                        eMA_Id              eMAId,
                                                                        EVSE_Id             EVSEId,
                                                                        ChargingProduct_Id  ChargingProductId)
        {

            return new HTTPResponse<RemoteStartResult>();

        //    lock (AuthenticationServices)
        //    {

        //        var OnRemoteStartLocal = OnRemoteStart;
        //        if (OnRemoteStartLocal != null)
        //        {

        //            return OnRemoteStartLocal(Timestamp,
        //                                      RoamingNetworkId,
        //                                      SessionId,
        //                                      PartnerSessionId,
        //                                      ProviderId,
        //                                      eMAId,
        //                                      EVSEId,
        //                                      ChargingProductId);

        //        }

        //        return RemoteStartResult.Error;

        //    }

        }

        #endregion

        #region RemoteStop(Timestamp, RoamingNetworkId, SessionId, PartnerSessionId, ProviderId, EVSEId)

        /// <summary>
        /// Initiate a remote stop of the given charging session at the given EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp of the request.</param>
        /// <param name="RoamingNetworkId">The unique identification for the roaming network.</param>
        /// <param name="SessionId">The unique identification for this charging session.</param>
        /// <param name="PartnerSessionId">The unique identification for this charging session on the partner side.</param>
        /// <param name="ProviderId">The unique identification of the e-mobility service provider.</param>
        /// <param name="EVSEId">The unique identification of an EVSE.</param>
        /// <returns>A remote stop result object.</returns>
        public async Task<HTTPResponse<RemoteStopResult>> RemoteStop(DateTime            Timestamp,
                                                                     RoamingNetwork_Id   RoamingNetworkId,
                                                                     ChargingSession_Id  SessionId,
                                                                     ChargingSession_Id  PartnerSessionId,
                                                                     EVSP_Id             ProviderId,
                                                                     EVSE_Id             EVSEId)
        {

            return new HTTPResponse<RemoteStopResult>();

            //lock (AuthenticationServices)
            //{
            //
            //    var OnRemoteStopLocal = OnRemoteStop;
            //    if (OnRemoteStopLocal != null)
            //        return OnRemoteStopLocal(Timestamp,
            //                                 RoamingNetworkId,
            //                                 SessionId,
            //                                 PartnerSessionId,
            //                                 ProviderId,
            //                                 EVSEId);
            //
            //    return RemoteStopResult.Error;
            //
            //}

        }

        #endregion


        #region SendChargingPoolAdminStatusDiff(StatusDiff)

        public void SendChargingPoolAdminStatusDiff(ChargingPoolAdminStatusDiff StatusDiff)
        {

            lock (AuthenticationServices)
            {

                var OnChargingPoolAdminDiffLocal = OnChargingPoolAdminDiff;
                if (OnChargingPoolAdminDiffLocal != null)
                    OnChargingPoolAdminDiffLocal(StatusDiff);

                //return RemoteStartResult.Error;

            }

            //return StatusDiff;

        }

        #endregion

        #region SendChargingStationAdminStatusDiff(StatusDiff)

        public void SendChargingStationAdminStatusDiff(ChargingStationAdminStatusDiff StatusDiff)
        {

            lock (AuthenticationServices)
            {

                var OnChargingStationAdminDiffLocal = OnChargingStationAdminDiff;
                if (OnChargingStationAdminDiffLocal != null)
                    OnChargingStationAdminDiffLocal(StatusDiff);

                //return RemoteStartResult.Error;

            }

            //return StatusDiff;

        }

        #endregion

        #region SendEVSEStatusDiff(StatusDiff)

        public void SendEVSEStatusDiff(EVSEStatusDiff StatusDiff)
        {

            lock (AuthenticationServices)
            {

                var OnEVSEStatusDiffLocal = OnEVSEStatusDiff;
                if (OnEVSEStatusDiffLocal != null)
                    OnEVSEStatusDiffLocal(StatusDiff);

                //return RemoteStartResult.Error;

            }

            //return StatusDiff;

        }

        #endregion











        #region (internal) UpdateEVSEData(Timestamp, EVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update the data of an EVSE.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSE">The changed EVSE.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        internal void UpdateEVSEData(DateTime  Timestamp,
                                     EVSE      EVSE,
                                     String    PropertyName,
                                     Object    OldValue,
                                     Object    NewValue)
        {

            var OnEVSEDataChangedLocal = OnEVSEDataChanged;
            if (OnEVSEDataChangedLocal != null)
                OnEVSEDataChangedLocal(Timestamp, EVSE, PropertyName, OldValue, NewValue);

        }

        #endregion

        #region (internal) UpdateEVSEStatus(Timestamp, EVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update an EVSE status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSE">The updated EVSE.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal void UpdateEVSEStatus(DateTime                     Timestamp,
                                       EVSE                         EVSE,
                                       Timestamped<EVSEStatusType>  OldStatus,
                                       Timestamped<EVSEStatusType>  NewStatus)
        {

            var OnEVSEStatusChangedLocal = OnEVSEStatusChanged;
            if (OnEVSEStatusChangedLocal != null)
                OnEVSEStatusChangedLocal(Timestamp, EVSE, OldStatus, NewStatus);

        }

        #endregion

        #region (internal) UpdateEVSEAdminStatus(Timestamp, EVSE, OldStatus, NewStatus)

        /// <summary>
        /// Update an EVSE admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSE">The updated EVSE.</param>
        /// <param name="OldStatus">The old EVSE status.</param>
        /// <param name="NewStatus">The new EVSE status.</param>
        internal void UpdateEVSEAdminStatus(DateTime                          Timestamp,
                                            EVSE                              EVSE,
                                            Timestamped<EVSEAdminStatusType>  OldStatus,
                                            Timestamped<EVSEAdminStatusType>  NewStatus)
        {

            var OnEVSEAdminStatusChangedLocal = OnEVSEAdminStatusChanged;
            if (OnEVSEAdminStatusChangedLocal != null)
                OnEVSEAdminStatusChangedLocal(Timestamp, EVSE, OldStatus, NewStatus);

        }

        #endregion


        #region (internal) UpdateChargingStationData(Timestamp, ChargingStation, OldStatus, NewStatus)

        /// <summary>
        /// Update the data of a charging station.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingStation">The changed charging station.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        internal void UpdateChargingStationData(DateTime         Timestamp,
                                                ChargingStation  ChargingStation,
                                                String           PropertyName,
                                                Object           OldValue,
                                                Object           NewValue)
        {

            var OnChargingStationDataChangedLocal = OnChargingStationDataChanged;
            if (OnChargingStationDataChangedLocal != null)
                OnChargingStationDataChangedLocal(Timestamp, ChargingStation, PropertyName, OldValue, NewValue);

        }

        #endregion

        #region (internal) UpdateChargingStationAdminStatus(Timestamp, ChargingStation, OldStatus, NewStatus)

        /// <summary>
        /// Update the current charging station admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingStation">The updated charging station.</param>
        /// <param name="OldStatus">The old charging station admin status.</param>
        /// <param name="NewStatus">The new charging station admin status.</param>
        internal void UpdateChargingStationAdminStatus(DateTime                                     Timestamp,
                                                       ChargingStation                              ChargingStation,
                                                       Timestamped<ChargingStationAdminStatusType>  OldStatus,
                                                       Timestamped<ChargingStationAdminStatusType>  NewStatus)
        {

            var OnChargingStationAdminStatusChangedLocal = OnChargingStationAdminStatusChanged;
            if (OnChargingStationAdminStatusChangedLocal != null)
                OnChargingStationAdminStatusChangedLocal(Timestamp, ChargingStation, OldStatus, NewStatus);

        }

        #endregion

        #region (internal) UpdateAggregatedChargingStationStatus(Timestamp, ChargingStation, OldStatus, NewStatus)

        /// <summary>
        /// Update the current aggregated charging station status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingStation">The updated charging station.</param>
        /// <param name="OldStatus">The old aggreagted charging station status.</param>
        /// <param name="NewStatus">The new aggreagted charging station status.</param>
        internal void UpdateAggregatedChargingStationStatus(DateTime                                Timestamp,
                                                            ChargingStation                         ChargingStation,
                                                            Timestamped<ChargingStationStatusType>  OldStatus,
                                                            Timestamped<ChargingStationStatusType>  NewStatus)
        {

            var OnAggregatedChargingStationStatusChangedLocal = OnAggregatedChargingStationStatusChanged;
            if (OnAggregatedChargingStationStatusChangedLocal != null)
                OnAggregatedChargingStationStatusChangedLocal(Timestamp, ChargingStation, OldStatus, NewStatus);

        }

        #endregion

        #region (internal) UpdateAggregatedChargingStationAdminStatus(Timestamp, ChargingStation, OldStatus, NewStatus)

        /// <summary>
        /// Update the current aggregated charging station admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingStation">The updated charging station.</param>
        /// <param name="OldStatus">The old aggreagted charging station admin status.</param>
        /// <param name="NewStatus">The new aggreagted charging station admin status.</param>
        internal void UpdateAggregatedChargingStationAdminStatus(DateTime                                     Timestamp,
                                                                 ChargingStation                              ChargingStation,
                                                                 Timestamped<ChargingStationAdminStatusType>  OldStatus,
                                                                 Timestamped<ChargingStationAdminStatusType>  NewStatus)
        {

            var OnAggregatedChargingStationAdminStatusChangedLocal = OnAggregatedChargingStationAdminStatusChanged;
            if (OnAggregatedChargingStationAdminStatusChangedLocal != null)
                OnAggregatedChargingStationAdminStatusChangedLocal(Timestamp, ChargingStation, OldStatus, NewStatus);

        }

        #endregion


        #region (internal) UpdateChargingPoolData(Timestamp, ChargingPool, OldStatus, NewStatus)

        /// <summary>
        /// Update the data of an charging pool.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingPool">The changed charging pool.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        internal void UpdateChargingPoolData(DateTime      Timestamp,
                                             ChargingPool  ChargingPool,
                                             String        PropertyName,
                                             Object        OldValue,
                                             Object        NewValue)
        {

            var OnChargingPoolDataChangedLocal = OnChargingPoolDataChanged;
            if (OnChargingPoolDataChangedLocal != null)
                OnChargingPoolDataChangedLocal(Timestamp, ChargingPool, PropertyName, OldValue, NewValue);

        }

        #endregion

        #region (internal) UpdateChargingPoolStatus(Timestamp, ChargingPool, OldStatus, NewStatus)

        /// <summary>
        /// Update a charging pool status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingPool">The updated charging pool.</param>
        /// <param name="OldStatus">The old aggregated charging pool status.</param>
        /// <param name="NewStatus">The new aggregated charging pool status.</param>
        internal void UpdateChargingPoolStatus(DateTime                             Timestamp,
                                               ChargingPool                         ChargingPool,
                                               Timestamped<ChargingPoolStatusType>  OldStatus,
                                               Timestamped<ChargingPoolStatusType>  NewStatus)
        {

            var OnAggregatedChargingPoolStatusChangedLocal = OnAggregatedChargingPoolStatusChanged;
            if (OnAggregatedChargingPoolStatusChangedLocal != null)
                OnAggregatedChargingPoolStatusChangedLocal(Timestamp, ChargingPool, OldStatus, NewStatus);

        }

        #endregion

        #region (internal) UpdateChargingPoolAdminStatus(Timestamp, ChargingPool, OldStatus, NewStatus)

        /// <summary>
        /// Update a charging pool admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="ChargingPool">The updated charging pool.</param>
        /// <param name="OldStatus">The old aggregated charging pool status.</param>
        /// <param name="NewStatus">The new aggregated charging pool status.</param>
        internal void UpdateChargingPoolAdminStatus(DateTime                                  Timestamp,
                                                    ChargingPool                              ChargingPool,
                                                    Timestamped<ChargingPoolAdminStatusType>  OldStatus,
                                                    Timestamped<ChargingPoolAdminStatusType>  NewStatus)
        {

            var OnAggregatedChargingPoolAdminStatusChangedLocal = OnAggregatedChargingPoolAdminStatusChanged;
            if (OnAggregatedChargingPoolAdminStatusChangedLocal != null)
                OnAggregatedChargingPoolAdminStatusChangedLocal(Timestamp, ChargingPool, OldStatus, NewStatus);

        }

        #endregion


        #region (internal) UpdateEVSEOperatorData(Timestamp, EVSEOperator, OldStatus, NewStatus)

        /// <summary>
        /// Update the data of an evse operator.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSEOperator">The changed evse operator.</param>
        /// <param name="PropertyName">The name of the changed property.</param>
        /// <param name="OldValue">The old value of the changed property.</param>
        /// <param name="NewValue">The new value of the changed property.</param>
        internal void UpdateEVSEOperatorData(DateTime      Timestamp,
                                             EVSEOperator  EVSEOperator,
                                             String        PropertyName,
                                             Object        OldValue,
                                             Object        NewValue)
        {

            var OnEVSEOperatorDataChangedLocal = OnEVSEOperatorDataChanged;
            if (OnEVSEOperatorDataChangedLocal != null)
                OnEVSEOperatorDataChangedLocal(Timestamp, EVSEOperator, PropertyName, OldValue, NewValue);

        }

        #endregion

        #region (internal) UpdateStatus(Timestamp, EVSEOperator, OldStatus, NewStatus)

        /// <summary>
        /// Update the current EVSE operator status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSEOperator">The updated EVSE operator.</param>
        /// <param name="OldStatus">The old aggreagted EVSE operator status.</param>
        /// <param name="NewStatus">The new aggreagted EVSE operator status.</param>
        internal void UpdateStatus(DateTime                             Timestamp,
                                   EVSEOperator                         EVSEOperator,
                                   Timestamped<EVSEOperatorStatusType>  OldStatus,
                                   Timestamped<EVSEOperatorStatusType>  NewStatus)
        {

            // Send EVSE operator status change upstream
            var OnAggregatedEVSEOperatorStatusChangedLocal = OnAggregatedEVSEOperatorStatusChanged;
            if (OnAggregatedEVSEOperatorStatusChangedLocal != null)
                OnAggregatedEVSEOperatorStatusChangedLocal(Timestamp, EVSEOperator, OldStatus, NewStatus);


            // Calculate new aggregated roaming network status and send upstream
            if (StatusAggregationDelegate != null)
            {

                var NewAggregatedStatus = new Timestamped<RoamingNetworkStatusType>(StatusAggregationDelegate(new EVSEOperatorStatusReport(_EVSEOperators.Values)));

                if (NewAggregatedStatus.Value != _StatusHistory.Peek().Value)
                {

                    var OldAggregatedStatus = _StatusHistory.Peek();

                    _StatusHistory.Push(NewAggregatedStatus);

                    var OnAggregatedStatusChangedLocal = OnAggregatedStatusChanged;
                    if (OnAggregatedStatusChangedLocal != null)
                        OnAggregatedStatusChangedLocal(Timestamp, this, OldAggregatedStatus, NewAggregatedStatus);

                }

            }

        }

        #endregion

        #region (internal) UpdateAdminStatus(Timestamp, EVSEOperator, OldStatus, NewStatus)

        /// <summary>
        /// Update the current EVSE operator admin status.
        /// </summary>
        /// <param name="Timestamp">The timestamp when this change was detected.</param>
        /// <param name="EVSEOperator">The updated EVSE operator.</param>
        /// <param name="OldStatus">The old aggreagted EVSE operator status.</param>
        /// <param name="NewStatus">The new aggreagted EVSE operator status.</param>
        internal void UpdateAdminStatus(DateTime                                  Timestamp,
                                        EVSEOperator                              EVSEOperator,
                                        Timestamped<EVSEOperatorAdminStatusType>  OldStatus,
                                        Timestamped<EVSEOperatorAdminStatusType>  NewStatus)
        {

            // Send EVSE operator admin status change upstream
            var OnAggregatedEVSEOperatorAdminStatusChangedLocal = OnAggregatedEVSEOperatorAdminStatusChanged;
            if (OnAggregatedEVSEOperatorAdminStatusChangedLocal != null)
                OnAggregatedEVSEOperatorAdminStatusChangedLocal(Timestamp, EVSEOperator, OldStatus, NewStatus);


            // Calculate new aggregated roaming network status and send upstream
            if (AdminStatusAggregationDelegate != null)
            {

                var NewAggregatedStatus = new Timestamped<RoamingNetworkAdminStatusType>(AdminStatusAggregationDelegate(new EVSEOperatorAdminStatusReport(_EVSEOperators.Values)));

                if (NewAggregatedStatus.Value != _AdminStatusHistory.Peek().Value)
                {

                    var OldAggregatedStatus = _AdminStatusHistory.Peek();

                    _AdminStatusHistory.Push(NewAggregatedStatus);

                    var OnAggregatedAdminStatusChangedLocal = OnAggregatedAdminStatusChanged;
                    if (OnAggregatedAdminStatusChangedLocal != null)
                        OnAggregatedAdminStatusChangedLocal(Timestamp, this, OldAggregatedStatus, NewAggregatedStatus);

                }

            }

        }

        #endregion


        #region IEnumerable<IEntity> Members

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _EVSEOperators.Values.GetEnumerator();
        }

        public IEnumerator<IEntity> GetEnumerator()
        {
            return _EVSEOperators.Values.GetEnumerator();
        }

        #endregion

        #region IComparable<RoamingNetwork> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object Object)
        {

            if (Object == null)
                throw new ArgumentNullException("The given object must not be null!");

            // Check if the given object is an RoamingNetwork.
            var RoamingNetwork = Object as RoamingNetwork;
            if ((Object) RoamingNetwork == null)
                throw new ArgumentException("The given object is not an RoamingNetwork!");

            return CompareTo(RoamingNetwork);

        }

        #endregion

        #region CompareTo(RoamingNetwork)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="RoamingNetwork">An RoamingNetwork object to compare with.</param>
        public Int32 CompareTo(RoamingNetwork RoamingNetwork)
        {

            if ((Object) RoamingNetwork == null)
                throw new ArgumentNullException("The given RoamingNetwork must not be null!");

            return Id.CompareTo(RoamingNetwork.Id);

        }

        #endregion

        #endregion

        #region IEquatable<RoamingNetwork> Members

        #region Equals(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        /// <returns>true|false</returns>
        public override Boolean Equals(Object Object)
        {

            if (Object == null)
                return false;

            // Check if the given object is an RoamingNetwork.
            var RoamingNetwork = Object as RoamingNetwork;
            if ((Object) RoamingNetwork == null)
                return false;

            return this.Equals(RoamingNetwork);

        }

        #endregion

        #region Equals(RoamingNetwork)

        /// <summary>
        /// Compares two RoamingNetwork for equality.
        /// </summary>
        /// <param name="RoamingNetwork">An RoamingNetwork to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(RoamingNetwork RoamingNetwork)
        {

            if ((Object) RoamingNetwork == null)
                return false;

            return Id.Equals(RoamingNetwork.Id);

        }

        #endregion

        #endregion

        #region GetHashCode()

        /// <summary>
        /// Get the hashcode of this object.
        /// </summary>
        public override Int32 GetHashCode()
        {
            return Id.GetHashCode();
        }

        #endregion

        #region (override) ToString()

        /// <summary>
        /// Get a string representation of this object.
        /// </summary>
        public override String ToString()
        {
            return Id.ToString();
        }

        #endregion

    }

}
