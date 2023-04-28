﻿/*
 * Copyright (c) 2014-2023 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using Org.BouncyCastle.Crypto.Parameters;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod.DNS;
using org.GraphDefined.Vanaheimr.Hermod.Logging;

#endregion

namespace cloud.charging.open.protocols.WWCP
{

    public abstract class AWWCPEMPAdapter<TChargeDetailRecords> : ACryptoEMobilityEntity<EMPRoamingProvider_Id,
                                                                                         EMPRoamingProviderAdminStatusTypes,
                                                                                         EMPRoamingProviderStatusTypes>,
                                                                  //IEMPRoamingProvider,
                                                                  ISendPOIData,
                                                                  ISendAdminStatus,
                                                                  ISendEnergyStatus,
                                                                  ISendStatus
    {

        #region Data

        /// <summary>
        /// The default EVSE data & status intervall.
        /// </summary>
        public  readonly static TimeSpan                                                         DefaultFlushEVSEDataAndStatusEvery      = TimeSpan.FromSeconds(31);

        /// <summary>
        /// The default EVSE fast status intervall.
        /// </summary>
        public  readonly static TimeSpan                                                         DefaultFlushEVSEFastStatusEvery         = TimeSpan.FromSeconds(3);

        /// <summary>
        /// The default EVSe status refresh intervall.
        /// </summary>
        public  readonly static TimeSpan                                                         DefaultEVSEStatusRefreshEvery           = TimeSpan.FromHours(6);

        /// <summary>
        /// The default CDR check intervall.
        /// </summary>
        public  readonly static TimeSpan                                                         DefaultFlushChargeDetailRecordsEvery    = TimeSpan.FromSeconds(15);


        protected               UInt64                                                           _FlushEVSEDataRunId                     = 1;
        protected               UInt64                                                           _StatusRunId                            = 1;
        protected               UInt64                                                           _CDRRunId                               = 1;

        protected readonly      SemaphoreSlim                                                    DataAndStatusLock                       = new (1, 1);
        protected readonly      Object                                                           DataAndStatusLockOld                    = new ();

        protected readonly      SemaphoreSlim                                                    FlushEVSEDataAndStatusLock              = new (1, 1);
        protected readonly      Timer                                                            FlushEVSEDataAndStatusTimer;

        protected readonly      SemaphoreSlim                                                    FlushEVSEFastStatusLock                 = new (1, 1);
        protected readonly      Timer                                                            FlushEVSEFastStatusTimer;

        protected readonly      SemaphoreSlim                                                    FlushChargeDetailRecordsLock            = new (1, 1);
        protected readonly      Timer                                                            FlushChargeDetailRecordsTimer;

        protected readonly      Dictionary<IRoamingNetwork,         List<PropertyUpdateInfo>>    roamingNetworksUpdateLog;
        protected readonly      Dictionary<ChargingStationOperator, List<PropertyUpdateInfo>>    chargingStationOperatorsUpdateLog;

        protected readonly      Dictionary<IChargingPool,           List<PropertyUpdateInfo>>    chargingPoolsUpdateLog;
        protected readonly      HashSet<IChargingPool>                                           chargingPoolsToAddQueue;
        protected readonly      HashSet<IChargingPool>                                           chargingPoolsToUpdateQueue;
        protected readonly      HashSet<IChargingPool>                                           chargingPoolsToRemoveQueue;
        protected readonly      List<ChargingPoolAdminStatusUpdate>                              chargingPoolAdminStatusChangesFastQueue;
        protected readonly      List<ChargingPoolAdminStatusUpdate>                              chargingPoolAdminStatusChangesDelayedQueue;
        protected readonly      List<ChargingPoolStatusUpdate>                                   chargingPoolStatusChangesFastQueue;
        protected readonly      List<ChargingPoolStatusUpdate>                                   chargingPoolStatusChangesDelayedQueue;

        protected readonly      HashSet<IChargingStation>                                        chargingStationsToAddQueue;
        protected readonly      HashSet<IChargingStation>                                        chargingStationsToUpdateQueue;
        protected readonly      HashSet<IChargingStation>                                        chargingStationsToRemoveQueue;
        protected readonly      List<ChargingStationAdminStatusUpdate>                           chargingStationAdminStatusChangesFastQueue;
        protected readonly      List<ChargingStationAdminStatusUpdate>                           chargingStationAdminStatusChangesDelayedQueue;
        protected readonly      List<ChargingStationStatusUpdate>                                chargingStationStatusChangesFastQueue;
        protected readonly      List<ChargingStationStatusUpdate>                                chargingStationStatusChangesDelayedQueue;
        protected readonly      Dictionary<IChargingStation,        List<PropertyUpdateInfo>>    chargingStationsUpdateLog;

        protected readonly      HashSet<IEVSE>                                                   evsesToAddQueue;
        protected readonly      HashSet<IEVSE>                                                   evsesToUpdateQueue;
        protected readonly      HashSet<IEVSE>                                                   evsesToRemoveQueue;
        protected readonly      List<EVSEAdminStatusUpdate>                                      evseAdminStatusChangesFastQueue;
        protected readonly      List<EVSEAdminStatusUpdate>                                      evseAdminStatusChangesDelayedQueue;
        protected readonly      List<EVSEStatusUpdate>                                           evseStatusChangesFastQueue;
        protected readonly      List<EVSEStatusUpdate>                                           evseStatusChangesDelayedQueue;
        protected readonly      Dictionary<IEVSE,                   List<PropertyUpdateInfo>>    evsesUpdateLog;

        protected readonly      List<TChargeDetailRecords>                                       chargeDetailRecordsQueue;

        protected readonly      TimeSpan                                                         MaxLockWaitingTime                      = TimeSpan.FromSeconds(120);

        public static readonly  TimeSpan                                                         DefaultRequestTimeout                   = TimeSpan.FromSeconds(30);

        #endregion

        #region Properties

        #region Include... ChargingStationOperator/ChargingPool/ChargingStation/EVSE

        /// <summary>
        /// Only include charging station identifications matching the given delegate.
        /// </summary>
        public IncludeChargingStationOperatorIdDelegate  IncludeChargingStationOperatorIds    { get; }

        /// <summary>
        /// Only include charging stations matching the given delegate.
        /// </summary>
        public IncludeChargingStationOperatorDelegate    IncludeChargingStationOperators      { get; }

        /// <summary>
        /// Only include charging pool identifications matching the given delegate.
        /// </summary>
        public IncludeChargingPoolIdDelegate             IncludeChargingPoolIds               { get; }

        /// <summary>
        /// Only include charging pools matching the given delegate.
        /// </summary>
        public IncludeChargingPoolDelegate               IncludeChargingPools                 { get; }

        /// <summary>
        /// Only include charging station identifications matching the given delegate.
        /// </summary>
        public IncludeChargingStationIdDelegate          IncludeChargingStationIds            { get; }

        /// <summary>
        /// Only include charging stations matching the given delegate.
        /// </summary>
        public IncludeChargingStationDelegate            IncludeChargingStations              { get; }

        /// <summary>
        /// Only include EVSE identifications matching the given delegate.
        /// </summary>
        public IncludeEVSEIdDelegate                     IncludeEVSEIds                       { get; }

        /// <summary>
        /// Only include EVSEs matching the given delegate.
        /// </summary>
        public IncludeEVSEDelegate                       IncludeEVSEs                         { get; }

        #endregion

        #region Disable... PushXXX/Authentication/SendChargeDetailRecords

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean                                   DisablePushData                      { get; set; }

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean                                   DisablePushAdminStatus               { get; set; }

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean                                   DisablePushStatus                    { get; set; }

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean                                   DisableEVSEStatusRefresh             { get; set; }

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean                                   DisablePushEnergyStatus              { get; set; }

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean                                   DisableAuthentication                { get; set; }

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean                                   DisableSendChargeDetailRecords       { get; set; }

        #endregion

        #region Filters

        /// <summary>
        /// A delegate for filtering charge detail records.
        /// </summary>
        public ChargeDetailRecordFilterDelegate          ChargeDetailRecordFilter             { get; }

        #endregion

        #region Flush...Every

        /// <summary>
        /// The EVSE data updates transmission intervall.
        /// </summary>
        public TimeSpan                                  FlushEVSEDataAndStatusEvery          { get; set; }

        /// <summary>
        /// The EVSE status updates transmission intervall.
        /// </summary>
        public TimeSpan                                  FlushEVSEFastStatusEvery             { get; set; }

        /// <summary>
        /// The EVSE status refresh intervall.
        /// </summary>
        public TimeSpan                                  EVSEStatusRefreshEvery               { get; set; }

        /// <summary>
        /// The charge detail record transmission intervall.
        /// </summary>
        public TimeSpan                                  FlushChargeDetailRecordsEvery        { get; set; }

        #endregion

        #region Logging

        public Boolean?                                   IsDevelopment            { get; }
        public IEnumerable<String>?                       DevelopmentServers       { get; }
        public Boolean?                                   DisableLogging           { get; set; }
        public String?                                    LoggingPath              { get; }
        public String?                                    LoggingContext           { get; }
        public String?                                    LogfileName              { get; }
        public LogfileCreatorDelegate?                    LogfileCreator           { get; }

        public String?                                    ClientsLoggingPath       { get; }
        public String?                                    ClientsLoggingContext    { get; }
        public LogfileCreatorDelegate?                    ClientsLogfileCreator    { get; }

        #endregion

        public DNSClient?  DNSClient    { get; }

        #endregion

        #region Events

        #region OnWWCPCPOAdapterException

        public delegate Task OnWWCPCPOAdapterExceptionDelegate(DateTime             Timestamp,
                                                               AWWCPEMPAdapter<TChargeDetailRecords>      Sender,
                                                               Exception            Exception);

        public event OnWWCPCPOAdapterExceptionDelegate OnWWCPCPOAdapterException;

        #endregion


        #region FlushEVSEDataAndStatusQueues

        public delegate void FlushEVSEDataAndStatusQueuesStartedDelegate(AWWCPEMPAdapter<TChargeDetailRecords> Sender, DateTime StartTime, TimeSpan Every, UInt64 RunId);

        public event FlushEVSEDataAndStatusQueuesStartedDelegate? FlushEVSEDataAndStatusQueuesStartedEvent;

        public delegate void FlushEVSEDataAndStatusQueuesFinishedDelegate(AWWCPEMPAdapter<TChargeDetailRecords> Sender, DateTime StartTime, DateTime EndTime, TimeSpan Runtime, TimeSpan Every, UInt64 RunId);

        public event FlushEVSEDataAndStatusQueuesFinishedDelegate? FlushEVSEDataAndStatusQueuesFinishedEvent;

        #endregion

        #region FlushEVSEFastStatusQueues

        public delegate void FlushEVSEFastStatusQueuesStartedDelegate(AWWCPEMPAdapter<TChargeDetailRecords> Sender, DateTime StartTime, TimeSpan Every, UInt64 RunId);

        public event FlushEVSEFastStatusQueuesStartedDelegate? FlushEVSEFastStatusQueuesStartedEvent;

        public delegate void FlushEVSEFastStatusQueuesFinishedDelegate(AWWCPEMPAdapter<TChargeDetailRecords> Sender, DateTime StartTime, DateTime EndTime, TimeSpan Runtime, TimeSpan Every, UInt64 RunId);

        public event FlushEVSEFastStatusQueuesFinishedDelegate? FlushEVSEFastStatusQueuesFinishedEvent;

        #endregion

        #region FlushChargeDetailRecordsQueues

        public delegate void FlushChargeDetailRecordsQueuesStartedDelegate(AWWCPEMPAdapter<TChargeDetailRecords> Sender, DateTime StartTime, TimeSpan Every, UInt64 RunId);

        public event FlushChargeDetailRecordsQueuesStartedDelegate? FlushChargeDetailRecordsQueuesStartedEvent;

        public delegate void FlushChargeDetailRecordsQueuesFinishedDelegate(AWWCPEMPAdapter<TChargeDetailRecords> Sender, DateTime StartTime, DateTime EndTime, TimeSpan Runtime, TimeSpan Every, UInt64 RunId);

        public event FlushChargeDetailRecordsQueuesFinishedDelegate? FlushChargeDetailRecordsQueuesFinishedEvent;

        #endregion

        public delegate Task OnWarningsDelegate(DateTime Timestamp, String Class, String Method, IEnumerable<Warning> Warnings);

        public event OnWarningsDelegate? OnWarnings;

        #endregion

        #region Constructor(s)

        /// <summary>
        /// Create a new WWCP wrapper for the OICP roaming client for Charging Station Operators/CPOs.
        /// </summary>
        /// <param name="Id">The unique identification of the roaming provider.</param>
        /// <param name="Name">The offical (multi-language) name of the roaming provider.</param>
        /// <param name="Description">An optional (multi-language) description of the charging station operator roaming provider.</param>
        /// <param name="RoamingNetwork">A WWCP roaming network.</param>
        /// 
        /// <param name="IncludeEVSEIds">Only include EVSE identifications matching the given delegate.</param>
        /// <param name="IncludeEVSEs">Only include EVSEs matching the given delegate.</param>
        /// <param name="IncludeChargingStationIds">Only include charging station identifications matching the given delegate.</param>
        /// <param name="IncludeChargingStations">Only include charging stations matching the given delegate.</param>
        /// <param name="IncludeChargingPoolIds">Only include charging pool identifications matching the given delegate.</param>
        /// <param name="IncludeChargingPools">Only include charging pools matching the given delegate.</param>
        /// <param name="ChargeDetailRecordFilter">A delegate for filtering charge detail records.</param>
        /// 
        /// <param name="FlushEVSEDataAndStatusEvery">The service check intervall.</param>
        /// <param name="FlushEVSEFastStatusEvery">The status check intervall.</param>
        /// <param name="FlushChargeDetailRecordsEvery">The charge detail record intervall.</param>
        /// 
        /// <param name="DisablePushData">This service can be disabled, e.g. for debugging reasons.</param>
        /// <param name="DisablePushStatus">This service can be disabled, e.g. for debugging reasons.</param>
        /// <param name="DisableAuthentication">This service can be disabled, e.g. for debugging reasons.</param>
        /// <param name="DisableSendChargeDetailRecords">This service can be disabled, e.g. for debugging reasons.</param>
        protected AWWCPEMPAdapter(EMPRoamingProvider_Id                      Id,
                                  IRoamingNetwork                            RoamingNetwork,

                                  I18NString?                                Name                                = null,
                                  I18NString?                                Description                         = null,

                                  IncludeEVSEIdDelegate?                     IncludeEVSEIds                      = null,
                                  IncludeEVSEDelegate?                       IncludeEVSEs                        = null,
                                  IncludeChargingStationIdDelegate?          IncludeChargingStationIds           = null,
                                  IncludeChargingStationDelegate?            IncludeChargingStations             = null,
                                  IncludeChargingPoolIdDelegate?             IncludeChargingPoolIds              = null,
                                  IncludeChargingPoolDelegate?               IncludeChargingPools                = null,
                                  IncludeChargingStationOperatorIdDelegate?  IncludeChargingStationOperatorIds   = null,
                                  IncludeChargingStationOperatorDelegate?    IncludeChargingStationOperators     = null,
                                  ChargeDetailRecordFilterDelegate?          ChargeDetailRecordFilter            = null,

                                  TimeSpan?                                  FlushEVSEDataAndStatusEvery         = null,
                                  TimeSpan?                                  FlushEVSEFastStatusEvery            = null,
                                  TimeSpan?                                  EVSEStatusRefreshEvery              = null,
                                  TimeSpan?                                  FlushChargeDetailRecordsEvery       = null,

                                  Boolean                                    DisablePushData                     = false,
                                  Boolean                                    DisablePushAdminStatus              = false,
                                  Boolean                                    DisablePushStatus                   = false,
                                  Boolean                                    DisableEVSEStatusRefresh            = false,
                                  Boolean                                    DisablePushEnergyStatus             = false,
                                  Boolean                                    DisableAuthentication               = false,
                                  Boolean                                    DisableSendChargeDetailRecords      = false,

                                  String                                     EllipticCurve                       = "P-256",
                                  ECPrivateKeyParameters?                    PrivateKey                          = null,
                                  PublicKeyCertificates?                     PublicKeyCertificates               = null,

                                  Boolean?                                   IsDevelopment                       = null,
                                  IEnumerable<String>?                       DevelopmentServers                  = null,
                                  Boolean?                                   DisableLogging                      = false,
                                  String?                                    LoggingPath                         = null,
                                  String?                                    LoggingContext                      = null,
                                  String?                                    LogfileName                         = null,
                                  LogfileCreatorDelegate?                    LogfileCreator                      = null,

                                  String?                                    ClientsLoggingPath                  = null,
                                  String?                                    ClientsLoggingContext               = null,
                                  LogfileCreatorDelegate?                    ClientsLogfileCreator               = null,
                                  DNSClient?                                 DNSClient                           = null)

            : base(Id,
                   RoamingNetwork,
                   Name,
                   Description,
                   EllipticCurve,
                   PrivateKey,
                   PublicKeyCertificates)

        {

            this.IncludeEVSEIds                                  = IncludeEVSEIds                    ?? (evseid             => true);
            this.IncludeEVSEs                                    = IncludeEVSEs                      ?? (evse               => true);
            this.IncludeChargingStationIds                       = IncludeChargingStationIds         ?? (chargingStationId  => true);
            this.IncludeChargingStations                         = IncludeChargingStations           ?? (chargingStation    => true);
            this.IncludeChargingPoolIds                          = IncludeChargingPoolIds            ?? (chargingPoolId     => true);
            this.IncludeChargingPools                            = IncludeChargingPools              ?? (chargingPool       => true);
            this.IncludeChargingStationOperatorIds               = IncludeChargingStationOperatorIds ?? (chargingStationId  => true);
            this.IncludeChargingStationOperators                 = IncludeChargingStationOperators   ?? (chargingStation    => true);
            this.ChargeDetailRecordFilter                        = ChargeDetailRecordFilter          ?? (chargeDetailRecord => ChargeDetailRecordFilters.forward);

            this.DisablePushData                                 = DisablePushData;
            this.DisablePushAdminStatus                          = DisablePushAdminStatus;
            this.DisablePushStatus                               = DisablePushStatus;
            this.DisableEVSEStatusRefresh                        = DisableEVSEStatusRefresh;
            this.DisablePushEnergyStatus                         = DisablePushEnergyStatus;
            this.DisableAuthentication                           = DisableAuthentication;
            this.DisableSendChargeDetailRecords                  = DisableSendChargeDetailRecords;

            this.FlushEVSEDataAndStatusEvery                     = FlushEVSEDataAndStatusEvery       ?? DefaultFlushEVSEDataAndStatusEvery;
            this.FlushEVSEFastStatusEvery                        = FlushEVSEFastStatusEvery          ?? DefaultFlushEVSEFastStatusEvery;
            this.EVSEStatusRefreshEvery                          = EVSEStatusRefreshEvery            ?? DefaultEVSEStatusRefreshEvery;
            this.FlushChargeDetailRecordsEvery                   = FlushChargeDetailRecordsEvery     ?? DefaultFlushChargeDetailRecordsEvery;

            this.FlushEVSEDataAndStatusTimer                     = new Timer(FlushEVSEDataAndStatus);
            this.FlushEVSEFastStatusTimer                        = new Timer(FlushEVSEFastStatus);
            this.FlushChargeDetailRecordsTimer                   = new Timer(FlushChargeDetailRecords);

            this.chargingPoolsToAddQueue                         = new HashSet<IChargingPool>();
            this.chargingPoolsToUpdateQueue                      = new HashSet<IChargingPool>();
            this.chargingPoolsToRemoveQueue                      = new HashSet<IChargingPool>();
            this.chargingPoolAdminStatusChangesFastQueue         = new List<ChargingPoolAdminStatusUpdate>();
            this.chargingPoolAdminStatusChangesDelayedQueue      = new List<ChargingPoolAdminStatusUpdate>();
            this.chargingPoolStatusChangesFastQueue              = new List<ChargingPoolStatusUpdate>();
            this.chargingPoolStatusChangesDelayedQueue           = new List<ChargingPoolStatusUpdate>();
            this.chargingPoolsUpdateLog                          = new Dictionary<IChargingPool,    List<PropertyUpdateInfo>>();

            this.chargingStationsToAddQueue                      = new HashSet<IChargingStation>();
            this.chargingStationsToUpdateQueue                   = new HashSet<IChargingStation>();
            this.chargingStationsToRemoveQueue                   = new HashSet<IChargingStation>();
            this.chargingStationAdminStatusChangesFastQueue      = new List<ChargingStationAdminStatusUpdate>();
            this.chargingStationAdminStatusChangesDelayedQueue   = new List<ChargingStationAdminStatusUpdate>();
            this.chargingStationStatusChangesFastQueue           = new List<ChargingStationStatusUpdate>();
            this.chargingStationStatusChangesDelayedQueue        = new List<ChargingStationStatusUpdate>();
            this.chargingStationsUpdateLog                       = new Dictionary<IChargingStation, List<PropertyUpdateInfo>>();

            this.evsesToAddQueue                                 = new HashSet<IEVSE>();
            this.evsesToUpdateQueue                              = new HashSet<IEVSE>();
            this.evsesToRemoveQueue                              = new HashSet<IEVSE>();
            this.evseAdminStatusChangesFastQueue                 = new List<EVSEAdminStatusUpdate>();
            this.evseAdminStatusChangesDelayedQueue              = new List<EVSEAdminStatusUpdate>();
            this.evseStatusChangesFastQueue                      = new List<EVSEStatusUpdate>();
            this.evseStatusChangesDelayedQueue                   = new List<EVSEStatusUpdate>();
            this.evsesUpdateLog                                  = new Dictionary<IEVSE,            List<PropertyUpdateInfo>>();

            this.chargeDetailRecordsQueue                        = new List<TChargeDetailRecords>();

            this.IsDevelopment                                   = IsDevelopment;
            this.DevelopmentServers                              = DevelopmentServers;
            this.DisableLogging                                  = DisableLogging;
            this.LoggingPath                                     = LoggingPath;
            this.LoggingContext                                  = LoggingContext;
            this.LogfileName                                     = LogfileName;
            this.LogfileCreator                                  = LogfileCreator;

            this.ClientsLoggingPath                              = ClientsLoggingPath;
            this.ClientsLoggingContext                           = ClientsLoggingContext;
            this.ClientsLogfileCreator                           = ClientsLogfileCreator;
            this.DNSClient                                       = DNSClient;

        }

        #endregion


        #region (Set/Add/Update/Delete) Roaming network...

        #region SetStaticData   (RoamingNetwork, ...)

        /// <summary>
        /// Set the EVSE data of the given roaming network as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// <param name="TransmissionType">Whether to send the roaming network update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            SetStaticData(IRoamingNetwork     RoamingNetwork,
                          TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?           Timestamp           = null,
                          CancellationToken?  CancellationToken   = null,
                          EventTracking_Id?   EventTrackingId     = null,
                          TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region AddStaticData   (RoamingNetwork, ...)

        /// <summary>
        /// Add the EVSE data of the given roaming network to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// <param name="TransmissionType">Whether to send the roaming network update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            AddStaticData(IRoamingNetwork     RoamingNetwork,
                          TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?           Timestamp           = null,
                          CancellationToken?  CancellationToken   = null,
                          EventTracking_Id?   EventTrackingId     = null,
                          TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region UpdateStaticData(RoamingNetwork, PropertyName, NewValue, OldValue = null, DataSource = null, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the EVSE data of the given roaming network within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network.</param>
        /// <param name="PropertyName">The name of the roaming network property to update.</param>
        /// <param name="NewValue">The new value of the roaming network property to update.</param>
        /// <param name="OldValue">The optional old value of the roaming network property to update.</param>
        /// <param name="DataSource">An optional data source or context for this roaming network property update.</param>
        /// <param name="TransmissionType">Whether to send the roaming network update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            UpdateStaticData(IRoamingNetwork     RoamingNetwork,
                             String?             PropertyName,
                             Object?             NewValue,
                             Object?             OldValue,
                             String?             DataSource          = null,
                             TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?           Timestamp           = null,
                             CancellationToken?  CancellationToken   = null,
                             EventTracking_Id?   EventTrackingId     = null,
                             TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region DeleteStaticData(RoamingNetwork, ...)

        /// <summary>
        /// Delete the EVSE data of the given roaming network from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="RoamingNetwork">A roaming network to upload.</param>
        /// <param name="TransmissionType">Whether to send the roaming network update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            DeleteStaticData(IRoamingNetwork     RoamingNetwork,
                             TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?           Timestamp           = null,
                             CancellationToken?  CancellationToken   = null,
                             EventTracking_Id?   EventTrackingId     = null,
                             TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion


        #region UpdateRoamingNetworkAdminStatus(AdminStatusUpdates, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of roaming network admin status updates.
        /// </summary>
        /// <param name="AdminStatusUpdates">An enumeration of roaming network admin status updates.</param>
        /// <param name="TransmissionType">Whether to send the roaming network admin status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushRoamingNetworkAdminStatusResult>

            UpdateAdminStatus(IEnumerable<RoamingNetworkAdminStatusUpdate>  AdminStatusUpdates,
                              TransmissionTypes                             TransmissionType    = TransmissionTypes.Enqueue,

                              DateTime?                                     Timestamp           = null,
                              CancellationToken?                            CancellationToken   = null,
                              EventTracking_Id?                             EventTrackingId     = null,
                              TimeSpan?                                     RequestTimeout      = null)


                => Task.FromResult(
                       PushRoamingNetworkAdminStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #region UpdateRoamingNetworkStatus     (StatusUpdates,      TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of roaming network status updates.
        /// </summary>
        /// <param name="StatusUpdates">An enumeration of roaming network status updates.</param>
        /// <param name="TransmissionType">Whether to send the roaming network status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushRoamingNetworkStatusResult>

            UpdateStatus(IEnumerable<RoamingNetworkStatusUpdate>  StatusUpdates,
                          TransmissionTypes                       TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?                               Timestamp           = null,
                          CancellationToken?                      CancellationToken   = null,
                          EventTracking_Id?                       EventTrackingId     = null,
                          TimeSpan?                               RequestTimeout      = null)


                => Task.FromResult(
                       PushRoamingNetworkStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #endregion

        #region (Set/Add/Update/Delete) Charging station operator(s)...

        #region SetStaticData   (ChargingStationOperator, ...)

        /// <summary>
        /// Set the EVSE data of the given charging station operator as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperator">A charging station operator.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            SetStaticData(IChargingStationOperator  ChargingStationOperator,
                          TransmissionTypes         TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?                 Timestamp           = null,
                          CancellationToken?        CancellationToken   = null,
                          EventTracking_Id?         EventTrackingId     = null,
                          TimeSpan?                 RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region AddStaticData   (ChargingStationOperator, ...)

        /// <summary>
        /// Add the given charging station.
        /// </summary>
        /// <param name="ChargingStationOperator">A charging station operator.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            AddStaticData(IChargingStationOperator  ChargingStationOperator,
                          TransmissionTypes         TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?                 Timestamp           = null,
                          CancellationToken?        CancellationToken   = null,
                          EventTracking_Id?         EventTrackingId     = null,
                          TimeSpan?                 RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region UpdateStaticData(ChargingStationOperator, PropertyName, NewValue, OldValue = null, DataSource = null, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the EVSE data of the given charging station operator within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperator">A charging station operator.</param>
        /// <param name="PropertyName">The name of the charging station operator property to update.</param>
        /// <param name="NewValue">The new value of the charging station operator property to update.</param>
        /// <param name="OldValue">The optional old value of the charging station operator property to update.</param>
        /// <param name="DataSource">An optional data source or context for this charging station operator property update.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            UpdateStaticData(IChargingStationOperator  ChargingStationOperator,
                             String                    PropertyName,
                             Object?                   NewValue,
                             Object?                   OldValue            = null,
                             String?                   DataSource          = null,
                             TransmissionTypes         TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?                 Timestamp           = null,
                             CancellationToken?        CancellationToken   = null,
                             EventTracking_Id?         EventTrackingId     = null,
                             TimeSpan?                 RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region DeleteStaticData(ChargingStationOperator, ...)

        /// <summary>
        /// Delete the EVSE data of the given charging station operator from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperator">A charging station operator.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            DeleteStaticData(IChargingStationOperator  ChargingStationOperator,
                             TransmissionTypes         TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?                 Timestamp           = null,
                             CancellationToken?        CancellationToken   = null,
                             EventTracking_Id?         EventTrackingId     = null,
                             TimeSpan?                 RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion


        #region SetStaticData   (ChargingStationOperators, ...)

        /// <summary>
        /// Set the EVSE data of the given enumeration of charging station operators as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperators">An enumeration of charging station operators.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            SetStaticData(IEnumerable<IChargingStationOperator>  ChargingStationOperators,
                          TransmissionTypes                      TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?                              Timestamp           = null,
                          CancellationToken?                     CancellationToken   = null,
                          EventTracking_Id?                      EventTrackingId     = null,
                          TimeSpan?                              RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region AddStaticData   (ChargingStationOperators, ...)

        /// <summary>
        /// Add the EVSE data of the given enumeration of charging station operators to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperators">An enumeration of charging station operators.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            AddStaticData(IEnumerable<IChargingStationOperator>  ChargingStationOperators,
                          TransmissionTypes                      TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?                              Timestamp           = null,
                          CancellationToken?                     CancellationToken   = null,
                          EventTracking_Id?                      EventTrackingId     = null,
                          TimeSpan?                              RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region UpdateStaticData(ChargingStationOperators, ...)

        /// <summary>
        /// Update the EVSE data of the given enumeration of charging station operators within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperators">An enumeration of charging station operators.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            UpdateStaticData(IEnumerable<IChargingStationOperator>  ChargingStationOperators,
                             TransmissionTypes                      TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?                              Timestamp           = null,
                             CancellationToken?                     CancellationToken   = null,
                             EventTracking_Id?                      EventTrackingId     = null,
                             TimeSpan?                              RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region DeleteStaticData(ChargingStationOperators, ...)

        /// <summary>
        /// Delete the EVSE data of the given enumeration of charging station operators from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStationOperators">An enumeration of charging station operators.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            DeleteStaticData(IEnumerable<IChargingStationOperator>  ChargingStationOperators,
                             TransmissionTypes                      TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?                              Timestamp           = null,
                             CancellationToken?                     CancellationToken   = null,
                             EventTracking_Id?                      EventTrackingId     = null,
                             TimeSpan?                              RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion


        #region UpdateChargingStationOperatorAdminStatus(AdminStatusUpdates, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of charging station operator admin status updates.
        /// </summary>
        /// <param name="AdminStatusUpdates">An enumeration of charging station operator admin status updates.</param>
        /// <param name="TransmissionType">Whether to send the charging station operator admin status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationOperatorAdminStatusResult>

            UpdateAdminStatus(IEnumerable<ChargingStationOperatorAdminStatusUpdate>  AdminStatusUpdates,
                              TransmissionTypes                                      TransmissionType    = TransmissionTypes.Enqueue,

                              DateTime?                                              Timestamp           = null,
                              CancellationToken?                                     CancellationToken   = null,
                              EventTracking_Id?                                      EventTrackingId     = null,
                              TimeSpan?                                              RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationOperatorAdminStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #region UpdateChargingStationOperatorStatus     (StatusUpdates,      TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of charging station operator status updates.
        /// </summary>
        /// <param name="StatusUpdates">An enumeration of charging station operator status updates.</param>
        /// <param name="TransmissionType">Whether to send the charging station operator status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationOperatorStatusResult>

            UpdateStatus(IEnumerable<ChargingStationOperatorStatusUpdate>  StatusUpdates,
                         TransmissionTypes                                 TransmissionType    = TransmissionTypes.Enqueue,

                         DateTime?                                         Timestamp           = null,
                         CancellationToken?                                CancellationToken   = null,
                         EventTracking_Id?                                 EventTrackingId     = null,
                         TimeSpan?                                         RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationOperatorStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #endregion

        #region (Set/Add/Update/Delete) Charging pool(s)...

        #region SetStaticData   (ChargingPool,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Set the EVSE data of the given charging pool as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolDataResult>

            SetStaticData(IChargingPool       ChargingPool,
                          TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?           Timestamp           = null,
                          CancellationToken?  CancellationToken   = null,
                          EventTracking_Id?   EventTrackingId     = null,
                          TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolDataResult.NoOperation(
                           AuthId:                 Id,
                           SendPOIData:            this,
                           RejectedChargingPools:  Array.Empty<IChargingPool>(),
                           Description:            null,
                           Warnings:               null,
                           Runtime:                null
                       )
                   );

        #endregion

        #region AddStaticData   (ChargingPool,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Add the given charging pool.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolDataResult>

            AddStaticData(IChargingPool       ChargingPool,
                          TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?           Timestamp           = null,
                          CancellationToken?  CancellationToken   = null,
                          EventTracking_Id?   EventTrackingId     = null,
                          TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolDataResult.NoOperation(
                           AuthId:                 Id,
                           SendPOIData:            this,
                           RejectedChargingPools:  Array.Empty<IChargingPool>(),
                           Description:            null,
                           Warnings:               null,
                           Runtime:                null
                       )
                   );

        #endregion

        #region UpdateStaticData(ChargingPool,  PropertyName, NewValue, OldValue = null, DataSource = null, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the EVSE data of the given charging pool within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="PropertyName">The name of the charging pool property to update.</param>
        /// <param name="NewValue">The new value of the charging pool property to update.</param>
        /// <param name="OldValue">The optional old value of the charging pool property to update.</param>
        /// <param name="DataSource">An optional data source or context for this charging pool property update.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolDataResult>

            UpdateStaticData(IChargingPool       ChargingPool,
                             String              PropertyName,
                             Object?             NewValue,
                             Object?             OldValue            = null,
                             String?             DataSource          = null,
                             TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?           Timestamp           = null,
                             CancellationToken?  CancellationToken   = null,
                             EventTracking_Id?   EventTrackingId     = null,
                             TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolDataResult.NoOperation(
                           AuthId:                 Id,
                           SendPOIData:            this,
                           RejectedChargingPools:  Array.Empty<IChargingPool>(),
                           Description:            null,
                           Warnings:               null,
                           Runtime:                null
                       )
                   );

        #endregion

        #region DeleteStaticData(ChargingPool,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Delete the EVSE data of the given charging pool from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPool">A charging pool.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolDataResult>

            DeleteStaticData(IChargingPool       ChargingPool,
                             TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?           Timestamp           = null,
                             CancellationToken?  CancellationToken   = null,
                             EventTracking_Id?   EventTrackingId     = null,
                             TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolDataResult.NoOperation(
                           AuthId:                 Id,
                           SendPOIData:            this,
                           RejectedChargingPools:  Array.Empty<IChargingPool>(),
                           Description:            null,
                           Warnings:               null,
                           Runtime:                null
                       )
                   );

        #endregion


        #region SetStaticData   (ChargingPools, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Set the EVSE data of the given enumeration of charging pools as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolDataResult>

            SetStaticData(IEnumerable<IChargingPool>  ChargingPools,
                          TransmissionTypes           TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?                   Timestamp           = null,
                          CancellationToken?          CancellationToken   = null,
                          EventTracking_Id?           EventTrackingId     = null,
                          TimeSpan?                   RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolDataResult.NoOperation(
                           AuthId:                 Id,
                           SendPOIData:            this,
                           RejectedChargingPools:  Array.Empty<IChargingPool>(),
                           Description:            null,
                           Warnings:               null,
                           Runtime:                null
                       )
                   );

        #endregion

        #region AddStaticData   (ChargingPools, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Add the EVSE data of the given enumeration of charging pools to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolDataResult>

            AddStaticData(IEnumerable<IChargingPool>  ChargingPools,
                          TransmissionTypes           TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?                   Timestamp           = null,
                          CancellationToken?          CancellationToken   = null,
                          EventTracking_Id?           EventTrackingId     = null,
                          TimeSpan?                   RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolDataResult.NoOperation(
                           AuthId:                 Id,
                           SendPOIData:            this,
                           RejectedChargingPools:  Array.Empty<IChargingPool>(),
                           Description:            null,
                           Warnings:               null,
                           Runtime:                null
                       )
                   );

        #endregion

        #region UpdateStaticData(ChargingPools, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the EVSE data of the given enumeration of charging pools within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolDataResult>

            UpdateStaticData(IEnumerable<IChargingPool>  ChargingPools,
                             TransmissionTypes           TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?                   Timestamp           = null,
                             CancellationToken?          CancellationToken   = null,
                             EventTracking_Id?           EventTrackingId     = null,
                             TimeSpan?                   RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolDataResult.NoOperation(
                           AuthId:                 Id,
                           SendPOIData:            this,
                           RejectedChargingPools:  Array.Empty<IChargingPool>(),
                           Description:            null,
                           Warnings:               null,
                           Runtime:                null
                       )
                   );

        #endregion

        #region DeleteStaticData(ChargingPools, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Delete the EVSE data of the given enumeration of charging pools from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingPools">An enumeration of charging pools.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolDataResult>

            DeleteStaticData(IEnumerable<IChargingPool>  ChargingPools,
                             TransmissionTypes           TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?                   Timestamp           = null,
                             CancellationToken?          CancellationToken   = null,
                             EventTracking_Id?           EventTrackingId     = null,
                             TimeSpan?                   RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolDataResult.NoOperation(
                           AuthId:                 Id,
                           SendPOIData:            this,
                           RejectedChargingPools:  Array.Empty<IChargingPool>(),
                           Description:            null,
                           Warnings:               null,
                           Runtime:                null
                       )
                   );

        #endregion


        #region UpdateAdminStatus (AdminStatusUpdates,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of charging pool admin status updates.
        /// </summary>
        /// <param name="AdminStatusUpdates">An enumeration of charging pool admin status updates.</param>
        /// <param name="TransmissionType">Whether to send the charging pool admin status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolAdminStatusResult>

            UpdateAdminStatus(IEnumerable<ChargingPoolAdminStatusUpdate>  AdminStatusUpdates,
                              TransmissionTypes                           TransmissionType    = TransmissionTypes.Enqueue,

                              DateTime?                                   Timestamp           = null,
                              CancellationToken?                          CancellationToken   = null,
                              EventTracking_Id?                           EventTrackingId     = null,
                              TimeSpan?                                   RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolAdminStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #region UpdateStatus      (StatusUpdates,       TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of charging pool status updates.
        /// </summary>
        /// <param name="StatusUpdates">An enumeration of charging pool status updates.</param>
        /// <param name="TransmissionType">Whether to send the charging pool status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolStatusResult>

            UpdateStatus(IEnumerable<ChargingPoolStatusUpdate>  StatusUpdates,
                         TransmissionTypes                      TransmissionType    = TransmissionTypes.Enqueue,

                         DateTime?                              Timestamp           = null,
                         CancellationToken?                     CancellationToken   = null,
                         EventTracking_Id?                      EventTrackingId     = null,
                         TimeSpan?                              RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #region UpdateEnergyStatus(EnergyStatusUpdates, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of charging pool energy status updates.
        /// </summary>
        /// <param name="EnergyStatusUpdates">An enumeration of charging pool energy status updates.</param>
        /// <param name="TransmissionType">Whether to send the charging pool energy status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingPoolEnergyStatusResult>

            UpdateEnergyStatus(IEnumerable<ChargingPoolEnergyStatusUpdate>  EnergyStatusUpdates,
                               TransmissionTypes                            TransmissionType    = TransmissionTypes.Enqueue,

                               DateTime?                                    Timestamp           = null,
                               CancellationToken?                           CancellationToken   = null,
                               EventTracking_Id?                            EventTrackingId     = null,
                               TimeSpan?                                    RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingPoolEnergyStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #endregion

        #region (Set/Add/Update/Delete) Charging station(s)...

        #region SetStaticData   (ChargingStation,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Set the EVSE data of the given charging station as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationDataResult>

            SetStaticData(IChargingStation    ChargingStation,
                          TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?           Timestamp           = null,
                          CancellationToken?  CancellationToken   = null,
                          EventTracking_Id?   EventTrackingId     = null,
                          TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationDataResult.NoOperation(
                           AuthId:                    Id,
                           SendPOIData:               this,
                           RejectedChargingStations:  Array.Empty<IChargingStation>(),
                           Description:               null,
                           Warnings:                  null,
                           Runtime:                   null
                       )
                   );

        #endregion

        #region AddStaticData   (ChargingStation,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Add the EVSE data of the given charging station to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="TransmissionType">Whether to send the charging pool update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationDataResult>

            AddStaticData(IChargingStation    ChargingStation,
                          TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?           Timestamp           = null,
                          CancellationToken?  CancellationToken   = null,
                          EventTracking_Id?   EventTrackingId     = null,
                          TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationDataResult.NoOperation(
                           AuthId:                    Id,
                           SendPOIData:               this,
                           RejectedChargingStations:  Array.Empty<IChargingStation>(),
                           Description:               null,
                           Warnings:                  null,
                           Runtime:                   null
                       )
                   );

        #endregion

        #region UpdateStaticData(ChargingStation,  PropertyName, NewValue, OldValue = null, DataSource = null, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the EVSE data of the given charging station within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="PropertyName">The name of the charging station property to update.</param>
        /// <param name="NewValue">The new value of the charging station property to update.</param>
        /// <param name="OldValue">The optional old value of the charging station property to update.</param>
        /// <param name="DataSource">An optional data source or context for this charging station property update.</param>
        /// <param name="TransmissionType">Whether to send the charging station update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationDataResult>

            UpdateStaticData(IChargingStation    ChargingStation,
                             String              PropertyName,
                             Object?             NewValue,
                             Object?             OldValue            = null,
                             String?             DataSource          = null,
                             TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?           Timestamp           = null,
                             CancellationToken?  CancellationToken   = null,
                             EventTracking_Id?   EventTrackingId     = null,
                             TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationDataResult.NoOperation(
                           AuthId:                    Id,
                           SendPOIData:               this,
                           RejectedChargingStations:  Array.Empty<IChargingStation>(),
                           Description:               null,
                           Warnings:                  null,
                           Runtime:                   null
                       )
                   );

        #endregion

        #region DeleteStaticData(ChargingStation,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Delete the EVSE data of the given charging station from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStation">A charging station.</param>
        /// <param name="TransmissionType">Whether to send the charging station update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationDataResult>

            DeleteStaticData(IChargingStation    ChargingStation,
                             TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?           Timestamp           = null,
                             CancellationToken?  CancellationToken   = null,
                             EventTracking_Id?   EventTrackingId     = null,
                             TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationDataResult.NoOperation(
                           AuthId:                    Id,
                           SendPOIData:               this,
                           RejectedChargingStations:  Array.Empty<IChargingStation>(),
                           Description:               null,
                           Warnings:                  null,
                           Runtime:                   null
                       )
                   );

        #endregion


        #region SetStaticData   (ChargingStations, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Set the EVSE data of the given enumeration of charging stations as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="TransmissionType">Whether to send the charging station update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationDataResult>

            SetStaticData(IEnumerable<IChargingStation>  ChargingStations,
                          TransmissionTypes              TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?                      Timestamp           = null,
                          CancellationToken?             CancellationToken   = null,
                          EventTracking_Id?              EventTrackingId     = null,
                          TimeSpan?                      RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationDataResult.NoOperation(
                           AuthId:                    Id,
                           SendPOIData:               this,
                           RejectedChargingStations:  Array.Empty<IChargingStation>(),
                           Description:               null,
                           Warnings:                  null,
                           Runtime:                   null
                       )
                   );

        #endregion

        #region AddStaticData   (ChargingStations, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Add the EVSE data of the given enumeration of charging stations to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="TransmissionType">Whether to send the charging station update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationDataResult>

            AddStaticData(IEnumerable<IChargingStation>  ChargingStations,
                          TransmissionTypes              TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?                      Timestamp           = null,
                          CancellationToken?             CancellationToken   = null,
                          EventTracking_Id?              EventTrackingId     = null,
                          TimeSpan?                      RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationDataResult.NoOperation(
                           AuthId:                    Id,
                           SendPOIData:               this,
                           RejectedChargingStations:  Array.Empty<IChargingStation>(),
                           Description:               null,
                           Warnings:                  null,
                           Runtime:                   null
                       )
                   );

        #endregion

        #region UpdateStaticData(ChargingStations, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the EVSE data of the given enumeration of charging stations within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="TransmissionType">Whether to send the charging station update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationDataResult>

            UpdateStaticData(IEnumerable<IChargingStation>  ChargingStations,
                             TransmissionTypes              TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?                      Timestamp           = null,
                             CancellationToken?             CancellationToken   = null,
                             EventTracking_Id?              EventTrackingId     = null,
                             TimeSpan?                      RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationDataResult.NoOperation(
                           AuthId:                    Id,
                           SendPOIData:               this,
                           RejectedChargingStations:  Array.Empty<IChargingStation>(),
                           Description:               null,
                           Warnings:                  null,
                           Runtime:                   null
                       )
                   );

        #endregion

        #region DeleteStaticData(ChargingStations, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Delete the EVSE data of the given enumeration of charging stations from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="ChargingStations">An enumeration of charging stations.</param>
        /// <param name="TransmissionType">Whether to send the charging station update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationDataResult>

            DeleteStaticData(IEnumerable<IChargingStation>  ChargingStations,
                             TransmissionTypes              TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?                      Timestamp           = null,
                             CancellationToken?             CancellationToken   = null,
                             EventTracking_Id?              EventTrackingId     = null,
                             TimeSpan?                      RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationDataResult.NoOperation(
                           AuthId:                    Id,
                           SendPOIData:               this,
                           RejectedChargingStations:  Array.Empty<IChargingStation>(),
                           Description:               null,
                           Warnings:                  null,
                           Runtime:                   null
                       )
                   );

        #endregion


        #region UpdateAdminStatus (AdminStatusUpdates,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of charging station admin status updates.
        /// </summary>
        /// <param name="AdminStatusUpdates">An enumeration of charging station admin status updates.</param>
        /// <param name="TransmissionType">Whether to send the charging station admin status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationAdminStatusResult>

            UpdateAdminStatus(IEnumerable<ChargingStationAdminStatusUpdate>  AdminStatusUpdates,
                              TransmissionTypes                              TransmissionType    = TransmissionTypes.Enqueue,

                              DateTime?                                      Timestamp           = null,
                              CancellationToken?                             CancellationToken   = null,
                              EventTracking_Id?                              EventTrackingId     = null,
                              TimeSpan?                                      RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationAdminStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #region UpdateStatus      (StatusUpdates,       TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of charging station status updates.
        /// </summary>
        /// <param name="StatusUpdates">An enumeration of charging station status updates.</param>
        /// <param name="TransmissionType">Whether to send the charging station status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationStatusResult>

            UpdateStatus(IEnumerable<ChargingStationStatusUpdate>  StatusUpdates,
                         TransmissionTypes                         TransmissionType    = TransmissionTypes.Enqueue,

                         DateTime?                                 Timestamp           = null,
                         CancellationToken?                        CancellationToken   = null,
                         EventTracking_Id?                         EventTrackingId     = null,
                         TimeSpan?                                 RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #region UpdateEnergyStatus(EnergyStatusUpdates, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of charging station energy status updates.
        /// </summary>
        /// <param name="EnergyStatusUpdates">An enumeration of charging station energy status updates.</param>
        /// <param name="TransmissionType">Whether to send the charging station energy status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushChargingStationEnergyStatusResult>

            UpdateEnergyStatus(IEnumerable<ChargingStationEnergyStatusUpdate>  EnergyStatusUpdates,
                               TransmissionTypes                               TransmissionType    = TransmissionTypes.Enqueue,

                               DateTime?                                       Timestamp           = null,
                               CancellationToken?                              CancellationToken   = null,
                               EventTracking_Id?                               EventTrackingId     = null,
                               TimeSpan?                                       RequestTimeout      = null)


                => Task.FromResult(
                       PushChargingStationEnergyStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #endregion

        #region (Set/Add/Update/Delete) EVSE(s)...

        #region SetStaticData   (EVSE,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Set the given EVSE as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSE">An EVSE to upload.</param>
        /// <param name="TransmissionType">Whether to send the EVSE directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            SetStaticData(IEVSE               EVSE,
                          TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?           Timestamp           = null,
                          CancellationToken?  CancellationToken   = null,
                          EventTracking_Id?   EventTrackingId     = null,
                          TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region AddStaticData   (EVSE,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Add the given EVSE to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSE">An EVSE to upload.</param>
        /// <param name="TransmissionType">Whether to send the EVSE directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            AddStaticData(IEVSE               EVSE,
                          TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?           Timestamp           = null,
                          CancellationToken?  CancellationToken   = null,
                          EventTracking_Id?   EventTrackingId     = null,
                          TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region UpdateStaticData(EVSE,  PropertyName, NewValue, OldValue = null, DataSource = null, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the static data of the given EVSE.
        /// The EVSE can be uploaded as a whole, or just a single property of the EVSE.
        /// </summary>
        /// <param name="EVSE">An EVSE to update.</param>
        /// <param name="PropertyName">The name of the EVSE property to update.</param>
        /// <param name="NewValue">The new value of the EVSE property to update.</param>
        /// <param name="OldValue">The optional old value of the EVSE property to update.</param>
        /// <param name="DataSource">An optional data source or context for this EVSE property update.</param>
        /// <param name="TransmissionType">Whether to send the EVSE update directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            UpdateStaticData(IEVSE               EVSE,
                             String              PropertyName,
                             Object?             NewValue,
                             Object?             OldValue            = null,
                             String?             DataSource          = null,
                             TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?           Timestamp           = null,
                             CancellationToken?  CancellationToken   = null,
                             EventTracking_Id?   EventTrackingId     = null,
                             TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region DeleteStaticData(EVSE,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Delete the static data of the given EVSE.
        /// </summary>
        /// <param name="EVSE">An EVSE to delete.</param>
        /// <param name="TransmissionType">Whether to send the EVSE deletion directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            DeleteStaticData(IEVSE               EVSE,
                             TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?           Timestamp           = null,
                             CancellationToken?  CancellationToken   = null,
                             EventTracking_Id?   EventTrackingId     = null,
                             TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion


        #region SetStaticData   (EVSEs, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Set the given enumeration of EVSEs as new static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="TransmissionType">Whether to send the EVSE directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            SetStaticData(IEnumerable<IEVSE>  EVSEs,
                          TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?           Timestamp           = null,
                          CancellationToken?  CancellationToken   = null,
                          EventTracking_Id?   EventTrackingId     = null,
                          TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region AddStaticData   (EVSEs, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Add the given enumeration of EVSEs to the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="TransmissionType">Whether to send the EVSE directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            AddStaticData(IEnumerable<IEVSE>  EVSEs,
                          TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                          DateTime?           Timestamp           = null,
                          CancellationToken?  CancellationToken   = null,
                          EventTracking_Id?   EventTrackingId     = null,
                          TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region UpdateStaticData(EVSEs, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of EVSEs within the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="TransmissionType">Whether to send the EVSE directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            UpdateStaticData(IEnumerable<IEVSE>  EVSEs,
                             TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?           Timestamp           = null,
                             CancellationToken?  CancellationToken   = null,
                             EventTracking_Id?   EventTrackingId     = null,
                             TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion

        #region DeleteStaticData(EVSEs, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Delete the given enumeration of EVSEs from the static EVSE data at the OICP server.
        /// </summary>
        /// <param name="EVSEs">An enumeration of EVSEs.</param>
        /// <param name="TransmissionType">Whether to send the EVSE directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEDataResult>

            DeleteStaticData(IEnumerable<IEVSE>  EVSEs,
                             TransmissionTypes   TransmissionType    = TransmissionTypes.Enqueue,

                             DateTime?           Timestamp           = null,
                             CancellationToken?  CancellationToken   = null,
                             EventTracking_Id?   EventTrackingId     = null,
                             TimeSpan?           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEDataResult.NoOperation(
                           AuthId:         Id,
                           SendPOIData:    this,
                           RejectedEVSEs:  Array.Empty<IEVSE>(),
                           Description:    null,
                           Warnings:       null,
                           Runtime:        null
                       )
                   );

        #endregion


        #region UpdateAdminStatus (AdminStatusUpdates,  TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of EVSE admin status updates.
        /// </summary>
        /// <param name="AdminStatusUpdates">An enumeration of EVSE admin status updates.</param>
        /// <param name="TransmissionType">Whether to send the EVSE admin status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEAdminStatusResult>

            UpdateAdminStatus(IEnumerable<EVSEAdminStatusUpdate>  AdminStatusUpdates,
                              TransmissionTypes                   TransmissionType    = TransmissionTypes.Enqueue,

                              DateTime?                           Timestamp           = null,
                              CancellationToken?                  CancellationToken   = null,
                              EventTracking_Id?                   EventTrackingId     = null,
                              TimeSpan?                           RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEAdminStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #region UpdateStatus      (StatusUpdates,       TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of EVSE status updates.
        /// </summary>
        /// <param name="StatusUpdates">An enumeration of EVSE status updates.</param>
        /// <param name="TransmissionType">Whether to send the EVSE status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEStatusResult>

            UpdateStatus(IEnumerable<EVSEStatusUpdate>  StatusUpdates,
                         TransmissionTypes              TransmissionType    = TransmissionTypes.Enqueue,

                         DateTime?                      Timestamp           = null,
                         CancellationToken?             CancellationToken   = null,
                         EventTracking_Id?              EventTrackingId     = null,
                         TimeSpan?                      RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #region UpdateEnergyStatus(EnergyStatusUpdates, TransmissionType = Enqueue, ...)

        /// <summary>
        /// Update the given enumeration of EVSE energy status updates.
        /// </summary>
        /// <param name="EnergyStatusUpdates">An enumeration of EVSE energy status updates.</param>
        /// <param name="TransmissionType">Whether to send the EVSE energy status updates directly or enqueue it for a while.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        public virtual Task<PushEVSEEnergyStatusResult>

            UpdateEnergyStatus(IEnumerable<EVSEEnergyStatusUpdate>  EnergyStatusUpdates,
                               TransmissionTypes                    TransmissionType    = TransmissionTypes.Enqueue,

                               DateTime?                            Timestamp           = null,
                               CancellationToken?                   CancellationToken   = null,
                               EventTracking_Id?                    EventTrackingId     = null,
                               TimeSpan?                            RequestTimeout      = null)


                => Task.FromResult(
                       PushEVSEEnergyStatusResult.NoOperation(
                           Id,
                           this
                       )
                   );

        #endregion

        #endregion



        #region [obsolete] UpdateAdminStatus(Sender, AdminStatusUpdates, ...)

        /// <summary>
        /// Update the given enumeration of EVSE admin status updates.
        /// </summary>
        /// <param name="Sender">The sender of the admin status update.</param>
        /// <param name="AdminStatusUpdates">An enumeration of EVSE admin status updates.</param>
        /// 
        /// <param name="Timestamp">The optional timestamp of the request.</param>
        /// <param name="CancellationToken">An optional token to cancel this request.</param>
        /// <param name="EventTrackingId">An optional event tracking identification for correlating this request with other events.</param>
        /// <param name="RequestTimeout">An optional timeout for this request.</param>
        protected async Task<PushEVSEAdminStatusResult>

            UpdateStatus(ISendAdminStatus                    Sender,
                         IEnumerable<EVSEAdminStatusUpdate>  AdminStatusUpdates,

                         DateTime?                           Timestamp,
                         CancellationToken?                  CancellationToken,
                         EventTracking_Id                    EventTrackingId,
                         TimeSpan?                           RequestTimeout)

        {

            #region Initial checks

            if (AdminStatusUpdates == null || !AdminStatusUpdates.Any())
                return PushEVSEAdminStatusResult.NoOperation(Id,
                                                             Sender);

            if (DisablePushStatus)
                return PushEVSEAdminStatusResult.AdminDown(Id,
                                                           Sender,
                                                           AdminStatusUpdates);

            #endregion


            #region Send OnEnqueueSendCDRRequest event

            //try
            //{

            //    OnEnqueueSendCDRRequest?.Invoke(StartTime,
            //                                    Timestamp.Value,
            //                                    this,
            //                                    EventTrackingId,
            //                                    RoamingNetwork.Id,
            //                                    ChargeDetailRecord,
            //                                    RequestTimeout);

            //}
            //catch (Exception e)
            //{
            //    DebugX.LogException(e, nameof(WWCPCPOAdapter) + "." + nameof(OnSendCDRRequest));
            //}

            #endregion

            await DataAndStatusLock.WaitAsync().ConfigureAwait(false);

            try
            {

                var FilteredUpdates = AdminStatusUpdates.Where(adminstatusupdate => IncludeEVSEIds(adminstatusupdate.Id)).
                                                         ToArray();

                if (FilteredUpdates.Length > 0)
                {

                    foreach (var Update in FilteredUpdates)
                    {

                        // Delay the status update until the EVSE data had been uploaded!
                        if (!DisablePushData && evsesToAddQueue.Any(evse => evse.Id == Update.Id))
                            evseAdminStatusChangesDelayedQueue.Add(Update);

                        else
                            evseAdminStatusChangesFastQueue.Add(Update);

                    }

                    FlushEVSEFastStatusTimer.Change(FlushEVSEFastStatusEvery, TimeSpan.FromMilliseconds(-1));

                    return PushEVSEAdminStatusResult.Enqueued(Id, Sender);

                }

                return PushEVSEAdminStatusResult.NoOperation(Id, Sender);

            }
            finally
            {
                DataAndStatusLock.Release();
            }

        }

        #endregion



        #region (timer) FlushEVSEDataAndStatus(State)

        protected abstract Boolean  SkipFlushEVSEDataAndStatusQueues();

        protected abstract Task     FlushEVSEDataAndStatusQueues();

        private void FlushEVSEDataAndStatus(Object State)
        {
            if (!DisablePushData)
                FlushEVSEDataAndStatus2(State).Wait();
        }

        private async Task FlushEVSEDataAndStatus2(Object State)
        {

            var LockTaken = await FlushEVSEDataAndStatusLock.WaitAsync(0);

            try
            {

                if (LockTaken)
                {

                    //DebugX.LogT("FlushEVSEDataAndStatusLock entered!");

                    if (SkipFlushEVSEDataAndStatusQueues())
                        return;

                    #region Send StartEvent...

                    var StartTime = Timestamp.Now;

                    FlushEVSEDataAndStatusQueuesStartedEvent?.Invoke(this,
                                                                     StartTime,
                                                                     FlushEVSEDataAndStatusEvery,
                                                                     _FlushEVSEDataRunId);

                    #endregion

                    await FlushEVSEDataAndStatusQueues();

                    #region Send Finished Event...

                    var EndTime = Timestamp.Now;

                    FlushEVSEDataAndStatusQueuesFinishedEvent?.Invoke(this,
                                                                      StartTime,
                                                                      EndTime,
                                                                      EndTime - StartTime,
                                                                      FlushEVSEDataAndStatusEvery,
                                                                      _FlushEVSEDataRunId);

                    #endregion

                    _FlushEVSEDataRunId++;

                }

            }
            catch (Exception e)
            {

                while (e.InnerException != null)
                    e = e.InnerException;

                DebugX.LogT(GetType().Name + ".FlushEVSEDataAndStatus '" + Id + "' led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

                OnWWCPCPOAdapterException?.Invoke(Timestamp.Now,
                                                  this,
                                                  e);

            }

            finally
            {

                if (LockTaken)
                {
                    FlushEVSEDataAndStatusLock.Release();
               //     DebugX.LogT("FlushEVSEDataAndStatusLock released!");
                }

                else
                    DebugX.LogT("FlushEVSEDataAndStatusLock exited!");

            }

        }

        #endregion

        #region (timer) FlushEVSEFastStatus(State)

        protected abstract Boolean  SkipFlushEVSEFastStatusQueues();

        protected abstract Task     FlushEVSEFastStatusQueues();

        private void FlushEVSEFastStatus(Object State)
        {
            if (!DisablePushStatus)
                FlushEVSEFastStatus2(State).Wait();
        }

        private async Task FlushEVSEFastStatus2(Object State)
        {

            var LockTaken = await FlushEVSEFastStatusLock.WaitAsync(0);

            try
            {

                if (LockTaken)
                {

                    //DebugX.LogT("FlushEVSEFastStatusLock entered!");

                    if (SkipFlushEVSEFastStatusQueues())
                        return;

                    #region Send StartEvent...

                    var StartTime = Timestamp.Now;

                    FlushEVSEFastStatusQueuesStartedEvent?.Invoke(this,
                                                                  StartTime,
                                                                  FlushEVSEFastStatusEvery,
                                                                  _StatusRunId);

                    #endregion

                    await FlushEVSEFastStatusQueues();

                    #region Send Finished Event...

                    var EndTime = Timestamp.Now;

                    FlushEVSEFastStatusQueuesFinishedEvent?.Invoke(this,
                                                                   StartTime,
                                                                   EndTime,
                                                                   EndTime - StartTime,
                                                                   FlushEVSEFastStatusEvery,
                                                                   _StatusRunId);

                    #endregion

                    _StatusRunId++;

                }

            }
            catch (Exception e)
            {

                while (e.InnerException != null)
                    e = e.InnerException;

                DebugX.LogT(GetType().Name + ".FlushEVSEFastStatus '" + Id + "' led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

                OnWWCPCPOAdapterException?.Invoke(Timestamp.Now,
                                                  this,
                                                  e);

            }

            finally
            {

                if (LockTaken)
                {
                    FlushEVSEFastStatusLock.Release();
                 //   DebugX.LogT("FlushEVSEFastStatusLock released!");
                }

                else
                    DebugX.LogT("FlushEVSEFastStatusLock exited!");

            }

        }

        #endregion

        #region (timer) FlushChargeDetailRecords(State)

        protected abstract Boolean  SkipFlushChargeDetailRecordsQueues();

        protected abstract Task     FlushChargeDetailRecordsQueues(IEnumerable<TChargeDetailRecords> ChargeDetailsRecords);

        private void FlushChargeDetailRecords(Object State)
        {
            if (!DisableSendChargeDetailRecords)
                FlushChargeDetailRecords2(State).Wait();
        }

        private async Task FlushChargeDetailRecords2(Object State)
        {

            var LockTaken = await FlushChargeDetailRecordsLock.WaitAsync(0);

            try
            {

                if (LockTaken)
                {

                    //DebugX.LogT("FlushChargeDetailRecordsLock entered!");

                    if (SkipFlushChargeDetailRecordsQueues())
                        return;

                    #region Send StartEvent...

                    var StartTime = Timestamp.Now;

                    FlushChargeDetailRecordsQueuesStartedEvent?.Invoke(this,
                                                                       StartTime,
                                                                       FlushEVSEFastStatusEvery,
                                                                       _CDRRunId);

                    #endregion

                    var chargeDetailRecordsQueueCopy = chargeDetailRecordsQueue.ToArray();
                    chargeDetailRecordsQueue.Clear();

                    await FlushChargeDetailRecordsQueues(chargeDetailRecordsQueueCopy);

                    #region Send Finished Event...

                    var EndTime = Timestamp.Now;

                    FlushChargeDetailRecordsQueuesFinishedEvent?.Invoke(this,
                                                                        StartTime,
                                                                        EndTime,
                                                                        EndTime - StartTime,
                                                                        FlushEVSEFastStatusEvery,
                                                                        _CDRRunId);

                    #endregion

                    _CDRRunId++;

                }

            }
            catch (Exception e)
            {

                while (e.InnerException != null)
                    e = e.InnerException;

                DebugX.LogT(GetType().Name + ".FlushChargeDetailRecords '" + Id + "' led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

                OnWWCPCPOAdapterException?.Invoke(Timestamp.Now,
                                                  this,
                                                  e);

            }

            finally
            {

                if (LockTaken)
                {
                    FlushChargeDetailRecordsLock.Release();
                    DebugX.LogT("FlushChargeDetailRecordsLock released!");
                }

                else
                    DebugX.LogT("FlushChargeDetailRecordsLock exited!");

            }

        }

        #endregion



        protected void SendOnWarnings(DateTime              Timestamp,
                                      String                Class,
                                      String                Method,
                                      IEnumerable<Warning>  Warnings)
        {

            if (Warnings.Any())
                OnWarnings?.Invoke(Timestamp,
                                   Class,
                                   Method,
                                   Warnings);

        }


    }

}
