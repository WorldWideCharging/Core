﻿/*
 * Copyright (c) 2014-2017 GraphDefined GmbH <achim.friedland@graphdefined.com>
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using org.GraphDefined.Vanaheimr.Illias;

#endregion

namespace org.GraphDefined.WWCP
{


    public abstract class AWWCPCPOAdapter : ABaseEMobilityEntity<CSORoamingProvider_Id>
                                         //   ICSORoamingProvider,
                                         //   IComparable
    {

        public class PropertyUpdateInfos
        {

            public String PropertyName  { get; }
            public Object OldValue      { get; }
            public Object NewValue      { get; }

            public PropertyUpdateInfos(String PropertyName,
                                       Object OldValue,
                                       Object NewValue)
            {

                this.PropertyName  = PropertyName;
                this.OldValue      = OldValue;
                this.NewValue      = NewValue;

            }


            public override string ToString()

                => String.Concat("Update of '", PropertyName, "' '",
                                 OldValue != null ? OldValue.ToString() : "",
                                 "' -> '",
                                 NewValue != null ? NewValue.ToString() : "",
                                 "'!");

        }

        #region Data

        /// <summary>
        /// The default service check intervall.
        /// </summary>
        public readonly static TimeSpan                                                         DefaultServiceCheckEvery    = TimeSpan.FromSeconds(31);

        /// <summary>
        /// The default status check intervall.
        /// </summary>
        public readonly static TimeSpan                                                         DefaultStatusCheckEvery     = TimeSpan.FromSeconds(3);

        /// <summary>
        /// The default CDR check intervall.
        /// </summary>
        public readonly static TimeSpan                                                         DefaultCDRCheckEvery        = TimeSpan.FromSeconds(15);


        protected              UInt64                                                           _FlushEVSEDataRunId;
        protected              UInt64                                                           _StatusRunId;


        protected readonly     Object                                                           FlushEVSEDataAndStatusLock;
        protected readonly     Timer                                                            FlushEVSEDataTimer;
        protected readonly     Object                                                           DataAndStatusLock;
        protected readonly     Object                                                           FlushEVSEFastStatusLock;
        protected readonly     Timer                                                            StatusCheckTimer;
        protected readonly     Object                                                           FlushChargeDetailRecordsLock;

        protected readonly     Dictionary<EVSE,                    List<PropertyUpdateInfos>>   EVSEsUpdateLog;
        protected readonly     Dictionary<ChargingStation,         List<PropertyUpdateInfos>>   ChargingStationsUpdateLog;
        protected readonly     Dictionary<ChargingPool,            List<PropertyUpdateInfos>>   ChargingPoolsUpdateLog;
        protected readonly     Dictionary<ChargingStationOperator, List<PropertyUpdateInfos>>   ChargingStationOperatorsUpdateLog;
        protected readonly     Dictionary<RoamingNetwork,          List<PropertyUpdateInfos>>   RoamingNetworksUpdateLog;

        #endregion

        #region Properties

        /// <summary>
        /// The offical (multi-language) name of the charging station operator roaming provider.
        /// </summary>
        [Mandatory]
        public I18NString  Name                              { get; }

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean     DisablePushData                   { get; set; }

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean     DisablePushStatus                 { get; set; }

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean     DisableAuthentication             { get; set; }

        /// <summary>
        /// This service can be disabled, e.g. for debugging reasons.
        /// </summary>
        public Boolean     DisableSendChargeDetailRecords    { get; set; }



        #region FlushEVSEDataEvery

        protected UInt32 _FlushEVSEDataAndStatusEvery;

        /// <summary>
        /// The EVSE data updates transmission intervall.
        /// </summary>
        public TimeSpan FlushEVSEDataEvery
        {

            get
            {
                return TimeSpan.FromSeconds(_FlushEVSEDataAndStatusEvery);
            }

            set
            {
                _FlushEVSEDataAndStatusEvery = (UInt32)value.TotalSeconds;
            }

        }

        #endregion

        #region FlushEVSEFastStatusEvery

        protected UInt32 _FlushEVSEFastStatusEvery;

        /// <summary>
        /// The EVSE status updates transmission intervall.
        /// </summary>
        public TimeSpan FlushEVSEFastStatusEvery
        {

            get
            {
                return TimeSpan.FromSeconds(_FlushEVSEFastStatusEvery);
            }

            set
            {
                _FlushEVSEFastStatusEvery = (UInt32)value.TotalSeconds;
            }

        }

        #endregion

        #region FlushChargeDetailRecordsEvery

        protected UInt32 _FlushChargeDetailRecordsEvery;

        /// <summary>
        /// The charge detail record transmission intervall.
        /// </summary>
        public TimeSpan FlushChargeDetailRecordsEvery
        {

            get
            {
                return TimeSpan.FromSeconds(_FlushChargeDetailRecordsEvery);
            }

            set
            {
                _FlushChargeDetailRecordsEvery = (UInt32)value.TotalSeconds;
            }

        }

        #endregion

        #endregion

        #region Events

        #region OnWWCPCPOAdapterException

        public delegate Task OnWWCPCPOAdapterExceptionDelegate(DateTime         Timestamp,
                                                               AWWCPCPOAdapter  Sender,
                                                               Exception        Exception);

        public event OnWWCPCPOAdapterExceptionDelegate OnWWCPCPOAdapterException;

        #endregion


        public delegate void FlushEVSEDataAndStatusQueuesStartedDelegate(AWWCPCPOAdapter Sender, DateTime StartTime, TimeSpan Every, UInt64 RunId);

        public event FlushEVSEDataAndStatusQueuesStartedDelegate FlushEVSEDataAndStatusQueuesStartedEvent;

        public delegate void FlushEVSEDataAndStatusQueuesFinishedDelegate(AWWCPCPOAdapter Sender, DateTime StartTime, DateTime EndTime, TimeSpan Runtime, TimeSpan Every, UInt64 RunId);

        public event FlushEVSEDataAndStatusQueuesFinishedDelegate FlushEVSEDataAndStatusQueuesFinishedEvent;


        public delegate void FlushEVSEFastStatusQueuesStartedDelegate(AWWCPCPOAdapter Sender, DateTime StartTime, TimeSpan Every, UInt64 RunId);

        public event FlushEVSEFastStatusQueuesStartedDelegate FlushEVSEFastStatusQueuesStartedEvent;

        public delegate void FlushEVSEFastStatusQueuesFinishedDelegate(AWWCPCPOAdapter Sender, DateTime StartTime, DateTime EndTime, TimeSpan Runtime, TimeSpan Every, UInt64 RunId);

        public event FlushEVSEFastStatusQueuesFinishedDelegate FlushEVSEFastStatusQueuesFinishedEvent;


        public delegate void FlushChargeDetailRecordsQueuesStartedDelegate(AWWCPCPOAdapter Sender, DateTime StartTime, TimeSpan Every, UInt64 RunId);

        public event FlushChargeDetailRecordsQueuesStartedDelegate FlushChargeDetailRecordsQueuesStartedEvent;

        public delegate void FlushChargeDetailRecordsQueuesFinishedDelegate(AWWCPCPOAdapter Sender, DateTime StartTime, DateTime EndTime, TimeSpan Runtime, TimeSpan Every, UInt64 RunId);

        public event FlushChargeDetailRecordsQueuesFinishedDelegate FlushChargeDetailRecordsQueuesFinishedEvent;


        #endregion


        public AWWCPCPOAdapter(CSORoamingProvider_Id                              Id,
                               I18NString                                         Name,
                               RoamingNetwork                                     RoamingNetwork,

                               //IncludeEVSEIdDelegate                              IncludeEVSEIds                                  = null,
                               //IncludeEVSEDelegate                                IncludeEVSEs                                    = null,
                               TimeSpan?                                          ServiceCheckEvery                               = null,
                               TimeSpan?                                          StatusCheckEvery                                = null,
                               TimeSpan?                                          CDRCheckEvery                                   = null,

                               Boolean                                            DisablePushData                                 = false,
                               Boolean                                            DisablePushStatus                               = false,
                               Boolean                                            DisableAuthentication                           = false,
                               Boolean                                            DisableSendChargeDetailRecords                  = false
                              )

            : base(Id,
                   RoamingNetwork)

        {

            this.Name                                            = Name;

            this.DisablePushData                                 = DisablePushData;
            this.DisablePushStatus                               = DisablePushStatus;
            this.DisableAuthentication                           = DisableAuthentication;
            this.DisableSendChargeDetailRecords                  = DisableSendChargeDetailRecords;


            this._FlushEVSEDataAndStatusEvery                    = (UInt32) (ServiceCheckEvery.HasValue
                                                                      ? ServiceCheckEvery.Value. TotalMilliseconds
                                                                      : DefaultServiceCheckEvery.TotalMilliseconds);

            this.FlushEVSEDataAndStatusLock                      = new Object();
            this.FlushEVSEDataTimer                              = new Timer(FlushEVSEDataAndStatus, null, 0, _FlushEVSEDataAndStatusEvery);
            this.DataAndStatusLock                               = new Object();

            this._FlushEVSEFastStatusEvery                       = (UInt32) (StatusCheckEvery.HasValue
                                                                        ? StatusCheckEvery.Value.  TotalMilliseconds
                                                                        : DefaultStatusCheckEvery. TotalMilliseconds);

            this._FlushChargeDetailRecordsEvery                  = (UInt32) (CDRCheckEvery.HasValue
                                                                        ? CDRCheckEvery.Value.  TotalMilliseconds
                                                                        : DefaultCDRCheckEvery. TotalMilliseconds);

            this.FlushEVSEFastStatusLock                         = new Object();
            this.StatusCheckTimer                                = new Timer(FlushEVSEFastStatus, null, 0, _FlushEVSEFastStatusEvery);

            this.EVSEsUpdateLog                                  = new Dictionary<EVSE,            List<PropertyUpdateInfos>>();
            this.ChargingStationsUpdateLog                       = new Dictionary<ChargingStation, List<PropertyUpdateInfos>>();
            this.ChargingPoolsUpdateLog                          = new Dictionary<ChargingPool,    List<PropertyUpdateInfos>>();



        }



        #region (timer) FlushEVSEDataAndStatus(State)

        protected abstract Task FlushEVSEDataAndStatusQueues();

        private void FlushEVSEDataAndStatus(Object State)
        {

            if (!DisablePushData)
            {

                if (Monitor.TryEnter(FlushEVSEDataAndStatusLock))
                {

                    try
                    {

                        //if (EVSEsToAddQueue.              Count == 0 &&
                        //    EVSEsToUpdateQueue.           Count == 0 &&
                        //    EVSEStatusChangesDelayedQueue.Count == 0 &&
                        //    EVSEsToRemoveQueue.           Count == 0)
                        //{
                        //    return;
                        //}

                        #region Send StartEvent...

                        var StartTime = DateTime.UtcNow;

                        FlushEVSEDataAndStatusQueuesStartedEvent?.Invoke(this,
                                                                         StartTime,
                                                                         TimeSpan.FromMilliseconds(_FlushEVSEDataAndStatusEvery),
                                                                         _FlushEVSEDataRunId++);

                        #endregion

                        FlushEVSEDataAndStatusQueues().Wait();

                        #region Send Finished Event...

                        var EndTime = DateTime.UtcNow;

                        FlushEVSEDataAndStatusQueuesFinishedEvent?.Invoke(this,
                                                                          StartTime,
                                                                          EndTime,
                                                                          EndTime - StartTime,
                                                                          TimeSpan.FromMilliseconds(_FlushEVSEDataAndStatusEvery),
                                                                          _FlushEVSEDataRunId);

                        #endregion

                    }
                    catch (Exception e)
                    {

                        while (e.InnerException != null)
                            e = e.InnerException;

                        DebugX.LogT(GetType().Name + ".FlushEVSEDataAndStatus '" + Id + "' led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

                        OnWWCPCPOAdapterException?.Invoke(DateTime.UtcNow,
                                                          this,
                                                          e);

                    }

                    finally
                    {
                        Monitor.Exit(FlushEVSEDataAndStatusLock);
                    }

                }

                else
                {

                    DebugX.LogT("FlushEVSEDataAndStatusLock missed!");
                    FlushEVSEDataTimer.Change(_FlushEVSEDataAndStatusEvery, Timeout.Infinite);

                }

            }

        }

        #endregion

        #region (timer) FlushEVSEFastStatus(State)

        protected abstract Task FlushEVSEFastStatusQueues();

        private void FlushEVSEFastStatus(Object State)
        {

            if (!DisablePushStatus)
            {

                if (Monitor.TryEnter(FlushEVSEFastStatusLock))
                {

                    try
                    {

                        //if (EVSEStatusChangesFastQueue.Count == 0)
                        //    return;

                        #region Send StartEvent...

                        var StartTime = DateTime.UtcNow;

                        FlushEVSEFastStatusQueuesStartedEvent?.Invoke(this,
                                                                      StartTime,
                                                                      TimeSpan.FromMilliseconds(_FlushEVSEFastStatusEvery),
                                                                      _StatusRunId++);

                        #endregion

                        FlushEVSEFastStatusQueues().Wait();

                        #region Send Finished Event...

                        var EndTime = DateTime.UtcNow;

                        FlushEVSEFastStatusQueuesFinishedEvent?.Invoke(this,
                                                                       StartTime,
                                                                       EndTime,
                                                                       EndTime - StartTime,
                                                                       TimeSpan.FromMilliseconds(_FlushEVSEFastStatusEvery),
                                                                       _StatusRunId);

                        #endregion

                    }
                    catch (Exception e)
                    {

                        while (e.InnerException != null)
                            e = e.InnerException;

                        DebugX.LogT(GetType().Name + ".FlushEVSEFastStatus '" + Id + "' led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

                        OnWWCPCPOAdapterException?.Invoke(DateTime.UtcNow,
                                                          this,
                                                          e);

                    }

                    finally
                    {
                        Monitor.Exit(FlushEVSEFastStatusLock);
                    }

                }

                else
                {

                    DebugX.LogT("FlushEVSEFastStatusLock missed!");
                    StatusCheckTimer.Change(_FlushEVSEFastStatusEvery, Timeout.Infinite);

                }

            }

        }

        #endregion

        #region (timer) FlushChargeDetailRecords(State)

        protected abstract Task FlushChargeDetailRecordsQueues();

        private void FlushChargeDetailRecords(Object State)
        {

            if (!DisableSendChargeDetailRecords)
            {

                if (Monitor.TryEnter(FlushChargeDetailRecordsLock))
                {

                    try
                    {

                        //if (ChargeDetailRecordQueue.Count == 0)
                        //    return;

                        #region Send StartEvent...

                        var StartTime = DateTime.UtcNow;

                        FlushChargeDetailRecordsQueuesStartedEvent?.Invoke(this,
                                                                           StartTime,
                                                                           TimeSpan.FromMilliseconds(_FlushEVSEFastStatusEvery),
                                                                           _StatusRunId++);

                        #endregion

                        FlushChargeDetailRecordsQueues().Wait();

                        #region Send Finished Event...

                        var EndTime = DateTime.UtcNow;

                        FlushChargeDetailRecordsQueuesFinishedEvent?.Invoke(this,
                                                                            StartTime,
                                                                            EndTime,
                                                                            EndTime - StartTime,
                                                                            TimeSpan.FromMilliseconds(_FlushEVSEFastStatusEvery),
                                                                            _StatusRunId);

                        #endregion

                    }
                    catch (Exception e)
                    {

                        while (e.InnerException != null)
                            e = e.InnerException;

                        DebugX.LogT(GetType().Name + ".FlushChargeDetailRecords '" + Id + "' led to an exception: " + e.Message + Environment.NewLine + e.StackTrace);

                        OnWWCPCPOAdapterException?.Invoke(DateTime.UtcNow,
                                                          this,
                                                          e);

                    }

                    finally
                    {
                        Monitor.Exit(FlushChargeDetailRecordsLock);
                    }

                }

                else
                {

                    DebugX.LogT("FlushChargeDetailRecordsLock missed!");
                    StatusCheckTimer.Change(_FlushChargeDetailRecordsEvery, Timeout.Infinite);

                }

            }

        }

        #endregion



    }

}
