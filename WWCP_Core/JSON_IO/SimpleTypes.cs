﻿/*
 * Copyright (c) 2014-2017 GraphDefined GmbH <achim.friedland@graphdefined.com>
 * This file is part of WWCP Net <https://github.com/OpenChargingCloud/WWCP_Net>
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

using Newtonsoft.Json.Linq;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Aegir;
using org.GraphDefined.Vanaheimr.Hermod;
using System.Globalization;

#endregion

namespace org.GraphDefined.WWCP.Net.IO.JSON
{

    /// <summary>
    /// WWCP JSON I/O.
    /// </summary>
    public static partial class JSON_IO
    {

        #region ToJSON(this Id, JPropertyKey)

        /// <summary>
        /// Create a JSON representation of the given identificator.
        /// </summary>
        /// <param name="Id">An identificator.</param>
        /// <param name="JPropertyKey">The name of the JSON property key to use.</param>
        public static JProperty ToJSON(this IId Id, String JPropertyKey)

            => Id != null
                   ? new JProperty(JPropertyKey, Id.ToString())
                   : null;

        #endregion

        #region ToJSON(this Location, JPropertyKey)

        /// <summary>
        /// Create a JSON representation of the given GeoLocation.
        /// </summary>
        /// <param name="Location">A GeoLocation.</param>
        /// <param name="JPropertyKey">The name of the JSON property key to use.</param>
        public static JProperty ToJSON(this GeoCoordinate Location, String JPropertyKey)
        {

            if (Location == default(GeoCoordinate))
                return null;

            return new JProperty(JPropertyKey,
                                 JSONObject.Create(
                                     Location.Projection != GravitationalModel.WGS84 ? new JProperty("projection", Location.Projection.ToString()) : null,
                                     new JProperty("lat", Location.Latitude. Value),
                                     new JProperty("lng", Location.Longitude.Value),
                                     Location.Altitude.HasValue                      ? new JProperty("altitude",   Location.Altitude.Value.Value)  : null)
                                );

        }

        /// <summary>
        /// Create a JSON representation of the given GeoLocation.
        /// </summary>
        /// <param name="Location">A GeoLocation.</param>
        /// <param name="JPropertyKey">The name of the JSON property key to use.</param>
        public static JProperty ToJSON(this GeoCoordinate? Location, String JPropertyKey)
        {

            if (!Location.HasValue)
                return null;

            return new JProperty(JPropertyKey,
                                 JSONObject.Create(
                                     Location.Value.Projection != GravitationalModel.WGS84 ? new JProperty("projection", Location.Value.Projection.ToString()) : null,
                                     new JProperty("lat", Location.Value.Latitude. Value),
                                     new JProperty("lng", Location.Value.Longitude.Value),
                                     Location.Value.Altitude.HasValue                      ? new JProperty("altitude",   Location.Value.Altitude.Value.Value)  : null)
                                );

        }

        #endregion

        #region ToJSON(this OpeningTimes)

        public static JObject ToJSON(this OpeningTimes OpeningTimes)
        {

            var JO = new JObject();
            //OpeningTimes.RegularHours.ForEach(rh => JO.Add(rh.Weekday.ToString(), new JObject(new JProperty("from", rh.Begin.ToString()), new JProperty("to", rh.End.ToString()))));
            OpeningTimes.RegularOpenings.ForEach(rh => JO.Add(rh.DayOfWeek.ToString(), new JArray(rh.PeriodBegin.ToString(), rh.PeriodEnd.ToString())));
            if (OpeningTimes.FreeText.IsNotNullOrEmpty())
                JO.Add("Text", OpeningTimes.FreeText);

            return (OpeningTimes != null)
                       ? OpeningTimes.IsOpen24Hours
                             ? new JObject()
                             : JO
                       : null;

        }

        #endregion

        #region ToJSON(this OpeningTimes, JPropertyKey)

        public static JProperty ToJSON(this OpeningTimes OpeningTimes, String JPropertyKey)

            => OpeningTimes != null
                   ? OpeningTimes.IsOpen24Hours
                         ? new JProperty(JPropertyKey, "24/7")
                         : new JProperty(JPropertyKey, OpeningTimes.ToJSON())
                   : null;

        #endregion

        #region ToJSON(this GridConnection, JPropertyKey)

        public static JProperty ToJSON(this GridConnectionTypes GridConnection, String JPropertyKey)

            => GridConnection != GridConnectionTypes.Unknown
                   ? new JProperty(JPropertyKey,
                                   GridConnection.ToString())
                   : null;

        #endregion

        #region ToJSON(this ChargingStationUIFeatures, JPropertyKey)

        public static JProperty ToJSON(this UIFeatures ChargingStationUIFeatures, String JPropertyKey)

            => new JProperty(JPropertyKey,
                             ChargingStationUIFeatures.ToString());

        #endregion

        #region ToJSON(this AuthenticationModes, JPropertyKey)

        public static JProperty ToJSON(this ReactiveSet<AuthenticationModes> AuthenticationModes, String JPropertyKey)

            => AuthenticationModes != null
                   ? new JProperty(JPropertyKey,
                                   new JArray(AuthenticationModes.SafeSelect(mode => mode.ToJSON())))
                   : null;

        #endregion

        #region ToJSON(this Text, JPropertyKey)

        public static JProperty ToJSON(this String Text, String JPropertyKey)

            => Text.IsNotNullOrEmpty()
                   ? new JProperty(JPropertyKey, Text)
                   : null;

        #endregion

        #region ToJSON(this Brand)

        public static JObject ToJSON(this Brand Brand)

            => Brand != null
                   ? JSONObject.Create(

                         new JProperty("Id",    Brand.Id.ToString()),
                         new JProperty("Name",  Brand.Name.ToJSON()),

                         Brand.LogoURI.IsNotNullOrEmpty()
                             ? new JProperty("LogoURI",   Brand.LogoURI)
                             : null,

                         Brand.Homepage.IsNotNullOrEmpty()
                             ? new JProperty("Homepage",  Brand.Homepage)
                             : null

                     )
                   : null;

        #endregion

        #region ToJSON(this Brand, JPropertyKey)

        public static JProperty ToJSON(this Brand Brand, String JPropertyKey)

            => Brand != null
                   ? new JProperty(JPropertyKey, Brand.ToJSON())
                   : null;

        #endregion

        #region ToJSON(this DataLicense)

        public static JObject ToJSON(this DataLicense DataLicense)

            => DataLicense != null
                   ? JSONObject.Create(
                         new JProperty("id",           DataLicense.Id),
                         new JProperty("description",  DataLicense.Description),
                         new JProperty("uris",         new JArray(DataLicense.URIs))
                     )
                   : null;

        #endregion

    }

}
