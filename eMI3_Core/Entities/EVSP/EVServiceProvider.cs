﻿/*
 * Copyright (c) 2014-2015 Achim Friedland <achim.friedland@graphdefined.com>
 * This file is part of eMI3 Core <http://www.github.com/GraphDefined/eMI3_Core>
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

using org.GraphDefined.eMI3.LocalService;

#endregion

namespace org.GraphDefined.eMI3
{

    /// <summary>
    /// The Electric Vehicle Service Provider is not only the main contract party
    /// of the EV driver, the EVSP also takes care of the EV driver master data,
    /// the authentication and autorisation process before charging and for the
    /// billing process after charging.
    /// The EVSP provides the EV drivere one or multiple methods for authentication
    /// (e.g. based on RFID cards, login/passwords, client certificates). The EVSP
    /// takes care that none of the provided authentication methods can be misused
    /// by any entity in the ev charging process to track the ev driver or its
    /// behaviour.
    /// </summary>
    public class EVServiceProvider : AEntity<EVSP_Id>,
                                     IEquatable<EVServiceProvider>, IComparable<EVServiceProvider>, IComparable
    {

        #region Data

        #endregion

        #region Properties

        #region RoamingNetwork

        private readonly RoamingNetwork _RoamingNetwork;

        /// <summary>
        /// The associated EV Roaming Network of the Electric Vehicle Supply Equipment Operator.
        /// </summary>
        public RoamingNetwork RoamingNetwork
        {
            get
            {
                return _RoamingNetwork;
            }
        }

        #endregion

        #region Name

        private I18NString _Name;

        /// <summary>
        /// The offical (multi-language) name of the Electric Vehicle Service Provider.
        /// </summary>
        [Mandatory]
        public I18NString Name
        {

            get
            {
                return _Name;
            }

            set
            {
                SetProperty<I18NString>(ref _Name, value);
            }

        }

        #endregion

        #region Description

        private I18NString _Description;

        /// <summary>
        /// An optional additional (multi-language) description of the Electric Vehicle Service Provider.
        /// </summary>
        [Optional]
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


        #region EMobilityService

        private readonly IRoamingProviderProvided_EVSEOperatorServices _EMobilityService;

        public IRoamingProviderProvided_EVSEOperatorServices EMobilityService
        {
            get
            {
                return _EMobilityService;
            }
        }

        #endregion

        #endregion

        #region Constructor(s)

        #region (internal) EVSProvider(Id, RoamingNetwork)

        /// <summary>
        /// Create a new Electric Vehicle Service Provider (EVSP)
        /// having the given EVSProvider_Id.
        /// </summary>
        /// <param name="Id">The EVSPool Id.</param>
        /// <param name="RoamingNetwork">The corresponding roaming network.</param>
        internal EVServiceProvider(EVSP_Id  Id,
                                   RoamingNetwork        RoamingNetwork)

            : this(Id, RoamingNetwork, new LocalEMobilityService(Id, Authorizator_Id.Parse(Id.ToString() + " Local Authorizator")))

        { }

        #endregion

        #region (internal) EVSProvider(Id, RoamingNetwork, EMobilityService)

        /// <summary>
        /// Create a new Electric Vehicle Service Provider (EVSP)
        /// having the given EVSProvider_Id.
        /// </summary>
        /// <param name="Id">The EVSPool Id.</param>
        /// <param name="RoamingNetwork">The associated roaming network.</param>
        /// <param name="EMobilityService">The attached local or remote e-mobility service.</param>
        internal EVServiceProvider(EVSP_Id          Id,
                                   RoamingNetwork                RoamingNetwork,
                                   IRoamingProviderProvided_EVSEOperatorServices  EMobilityService)

            : base(Id)

        {

            #region Initial Checks

            if (RoamingNetwork == null)
                throw new ArgumentNullException("RoamingNetwork", "The given roaming network must not be null!");

            if (EMobilityService == null)
                throw new ArgumentNullException("EMobilityService", "The given e-mobility service must not be null!");

            #endregion

            this.Name                   = new I18NString();
            this.Description            = new I18NString();

            this._RoamingNetwork        = RoamingNetwork;
            this._EMobilityService      = EMobilityService;

        }

        #endregion

        #endregion



        #region IComparable<EVSE_Operator> Members

        #region CompareTo(Object)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="Object">An object to compare with.</param>
        public Int32 CompareTo(Object Object)
        {

            if (Object == null)
                throw new ArgumentNullException("The given object must not be null!");

            // Check if the given object is an EVSE_Operator.
            var EVSE_Operator = Object as EVServiceProvider;
            if ((Object) EVSE_Operator == null)
                throw new ArgumentException("The given object is not an EVSE_Operator!");

            return CompareTo(EVSE_Operator);

        }

        #endregion

        #region CompareTo(EVSE_Operator)

        /// <summary>
        /// Compares two instances of this object.
        /// </summary>
        /// <param name="EVSE_Operator">An EVSE_Operator object to compare with.</param>
        public Int32 CompareTo(EVServiceProvider EVSE_Operator)
        {

            if ((Object) EVSE_Operator == null)
                throw new ArgumentNullException("The given EVSE_Operator must not be null!");

            return Id.CompareTo(EVSE_Operator.Id);

        }

        #endregion

        #endregion

        #region IEquatable<EVSE_Operator> Members

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

            // Check if the given object is an EVSE_Operator.
            var EVSE_Operator = Object as EVServiceProvider;
            if ((Object) EVSE_Operator == null)
                return false;

            return this.Equals(EVSE_Operator);

        }

        #endregion

        #region Equals(EVSE_Operator)

        /// <summary>
        /// Compares two EVSE_Operator for equality.
        /// </summary>
        /// <param name="EVSE_Operator">An EVSE_Operator to compare with.</param>
        /// <returns>True if both match; False otherwise.</returns>
        public Boolean Equals(EVServiceProvider EVSE_Operator)
        {

            if ((Object) EVSE_Operator == null)
                return false;

            return Id.Equals(EVSE_Operator.Id);

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

        #region ToString()

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
