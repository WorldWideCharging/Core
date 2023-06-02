﻿/*
 * Copyright (c) 2014-2023 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using NUnit.Framework;

using org.GraphDefined.Vanaheimr.Illias;
using org.GraphDefined.Vanaheimr.Hermod;
using org.GraphDefined.Vanaheimr.Hermod.HTTP;

#endregion

namespace cloud.charging.open.protocols.WWCP.tests.RoamingNetwork
{

    /// <summary>
    /// Unit tests for EVSEs.
    /// </summary>
    [TestFixture]
    public class EVSETests : AEVSETests
    {

        #region EVSE_Init_Test()

        /// <summary>
        /// A test for creating an EVSE within a charging station.
        /// </summary>
        [Test]
        public void EVSE_Init_Test()
        {

            Assert.IsNotNull(roamingNetwork);
            Assert.IsNotNull(DE_GEF);
            Assert.IsNotNull(DE_GEF_P0001);
            Assert.IsNotNull(DE_GEF_S0001_AAAA);
            Assert.IsNotNull(DE_GEF_E0001_AAAA_1);

            if (roamingNetwork      is not null &&
                DE_GEF              is not null &&
                DE_GEF_P0001        is not null &&
                DE_GEF_S0001_AAAA   is not null &&
                DE_GEF_E0001_AAAA_1 is not null)
            {

                Assert.AreEqual ("DE*GEF*E0001*AAAA*1",                 DE_GEF_E0001_AAAA_1.Id.         ToString());
                Assert.AreEqual ("GraphDefined EVSE #1",                DE_GEF_E0001_AAAA_1.Name.       FirstText());
                Assert.AreEqual ("powered by GraphDefined EVSEs GmbH",  DE_GEF_E0001_AAAA_1.Description.FirstText());

                Assert.AreEqual (EVSEAdminStatusTypes.OutOfService,     DE_GEF_E0001_AAAA_1.AdminStatus);
                Assert.AreEqual (1,                                     DE_GEF_E0001_AAAA_1.AdminStatusSchedule().Count());

                Assert.AreEqual (EVSEStatusTypes.Offline,               DE_GEF_E0001_AAAA_1.Status);
                Assert.AreEqual (1,                                     DE_GEF_E0001_AAAA_1.StatusSchedule().     Count());


                Assert.AreEqual (1,                                     roamingNetwork.   EVSEs.    Count());
                Assert.AreEqual (1,                                     roamingNetwork.   EVSEIds().Count());

                Assert.AreEqual (1,                                     DE_GEF.           EVSEs.    Count());
                Assert.AreEqual (1,                                     DE_GEF.           EVSEIds().Count());

                Assert.AreEqual (1,                                     DE_GEF_P0001.     EVSEs.    Count());
                Assert.AreEqual (1,                                     DE_GEF_P0001.     EVSEIds().Count());

                Assert.AreEqual (1,                                     DE_GEF_S0001_AAAA.EVSEs.    Count());
                Assert.AreEqual (1,                                     DE_GEF_S0001_AAAA.EVSEIds().Count());


                Assert.IsTrue   (roamingNetwork.   ContainsEVSE(EVSE_Id.Parse("DE*GEF*E0001*AAAA*1")));
                Assert.IsNotNull(roamingNetwork.   GetEVSEById (EVSE_Id.Parse("DE*GEF*E0001*AAAA*1")));

                Assert.IsTrue   (DE_GEF.           ContainsEVSE(EVSE_Id.Parse("DE*GEF*E0001*AAAA*1")));
                Assert.IsNotNull(DE_GEF.           GetEVSEById (EVSE_Id.Parse("DE*GEF*E0001*AAAA*1")));

                Assert.IsTrue   (DE_GEF_P0001.     ContainsEVSE(EVSE_Id.Parse("DE*GEF*E0001*AAAA*1")));
                Assert.IsNotNull(DE_GEF_P0001.     GetEVSEById (EVSE_Id.Parse("DE*GEF*E0001*AAAA*1")));

                Assert.IsTrue   (DE_GEF_S0001_AAAA.ContainsEVSE(EVSE_Id.Parse("DE*GEF*E0001*AAAA*1")));
                Assert.IsNotNull(DE_GEF_S0001_AAAA.GetEVSEById (EVSE_Id.Parse("DE*GEF*E0001*AAAA*1")));

            }

        }

        #endregion

        #region EVSE_Init_DefaultStatus_Test()

        /// <summary>
        /// A test for creating an EVSE within a charging station.
        /// </summary>
        [Test]
        public void EVSE_Init_DefaultStatus_Test()
        {

            Assert.IsNotNull(roamingNetwork);
            Assert.IsNotNull(DE_GEF);
            Assert.IsNotNull(DE_GEF_P0001);
            Assert.IsNotNull(DE_GEF_S0001_AAAA);

            if (roamingNetwork    is not null &&
                DE_GEF            is not null &&
                DE_GEF_P0001      is not null &&
                DE_GEF_S0001_AAAA is not null)
            {

                var DE_GEF_E0001_AAAA_X = DE_GEF_S0001_AAAA.AddEVSE(
                                                                Id:           EVSE_Id.Parse("DE*GEF*E1234"),
                                                                Name:         I18NString.Create(Languages.de, "DE*GEF EVSE 1234"),
                                                                Description:  I18NString.Create(Languages.de, "powered by GraphDefined EVSEs GmbH")
                                                            ).Result.EVSE;

                Assert.IsNotNull(DE_GEF_E0001_AAAA_X);

                if (DE_GEF_E0001_AAAA_X is not null)
                {

                    Assert.AreEqual ("DE*GEF*E1234",                        DE_GEF_E0001_AAAA_X.Id.         ToString());
                    Assert.AreEqual ("DE*GEF EVSE 1234",                    DE_GEF_E0001_AAAA_X.Name.       FirstText());
                    Assert.AreEqual ("powered by GraphDefined EVSEs GmbH",  DE_GEF_E0001_AAAA_X.Description.FirstText());

                    Assert.AreEqual (EVSEAdminStatusTypes.Operational,      DE_GEF_E0001_AAAA_X.AdminStatus);
                    Assert.AreEqual (EVSEStatusTypes.Available,             DE_GEF_E0001_AAAA_X.Status);

                    Assert.IsTrue   (roamingNetwork.   ContainsEVSE(EVSE_Id.Parse("DE*GEF*E1234")));
                    Assert.IsNotNull(roamingNetwork.   GetEVSEById (EVSE_Id.Parse("DE*GEF*E1234")));

                    Assert.IsTrue   (DE_GEF.           ContainsEVSE(EVSE_Id.Parse("DE*GEF*E1234")));
                    Assert.IsNotNull(DE_GEF.           GetEVSEById (EVSE_Id.Parse("DE*GEF*E1234")));

                    Assert.IsTrue   (DE_GEF_P0001.     ContainsEVSE(EVSE_Id.Parse("DE*GEF*E1234")));
                    Assert.IsNotNull(DE_GEF_P0001.     GetEVSEById (EVSE_Id.Parse("DE*GEF*E1234")));

                    Assert.IsTrue   (DE_GEF_S0001_AAAA.ContainsEVSE(EVSE_Id.Parse("DE*GEF*E1234")));
                    Assert.IsNotNull(DE_GEF_S0001_AAAA.GetEVSEById (EVSE_Id.Parse("DE*GEF*E1234")));

                }

            }

        }

        #endregion

        #region EVSE_Init_AllProperties_Test()

        /// <summary>
        /// A test for creating an EVSE within a charging station having all properties.
        /// </summary>
        [Test]
        public void EVSE_Init_AllProperties_Test()
        {

            Assert.IsNotNull(roamingNetwork);
            Assert.IsNotNull(DE_GEF);
            Assert.IsNotNull(DE_GEF_P0001);
            Assert.IsNotNull(DE_GEF_S0001_AAAA);

            if (roamingNetwork    is not null &&
                DE_GEF            is not null &&
                DE_GEF_P0001      is not null &&
                DE_GEF_S0001_AAAA is not null)
            {

                var success = false;

                var DE_GEF_E1234_5678_1 = DE_GEF_S0001_AAAA.AddEVSE(
                                                                Id:                  EVSE_Id.Parse("DE*GEF*E1234*5678*1"),
                                                                Name:                I18NString.Create(Languages.de, "DE*GEF EVSE 1234*5678*1"),
                                                                Description:         I18NString.Create(Languages.de, "powered by GraphDefined EVSEs GmbH"),
                                                                InitialAdminStatus:  EVSEAdminStatusTypes.OutOfService,
                                                                InitialStatus:       EVSEStatusTypes.Offline,
                                                                OnSuccess:           evse => success = true,
                                                                Configurator:        evse => {

                                                                                         evse.Brands.Add(new Brand(
                                                                                                             Id:            Brand_Id.Parse("openChargingCloudEVSE"),
                                                                                                             Name:          I18NString.Create(Languages.de, "Open Charging Cloud EVSE"),
                                                                                                             Logo:          URL.Parse("https://open.charging.cloud/logos.json"),
                                                                                                             Homepage:      URL.Parse("https://open.charging.cloud"),
                                                                                                             DataLicenses:  new OpenDataLicense[] {
                                                                                                                                OpenDataLicense.CreativeCommons_BY_SA_4
                                                                                                                            }
                                                                                                         ));

                                                                                     }
                                                            ).Result.EVSE;

                Assert.IsNotNull(DE_GEF_E1234_5678_1);
                Assert.IsTrue   (success);

                if (DE_GEF_E1234_5678_1 is not null)
                {

                    Assert.AreEqual ("DE*GEF*E1234*5678*1",                 DE_GEF_E1234_5678_1.Id.         ToString());
                    Assert.AreEqual ("DE*GEF EVSE 1234*5678*1",             DE_GEF_E1234_5678_1.Name.       FirstText());
                    Assert.AreEqual ("powered by GraphDefined EVSEs GmbH",  DE_GEF_E1234_5678_1.Description.FirstText());

                    Assert.AreEqual (EVSEAdminStatusTypes.OutOfService,     DE_GEF_E1234_5678_1.AdminStatus);
                    Assert.AreEqual (EVSEStatusTypes.Offline,               DE_GEF_E1234_5678_1.Status);

                    Assert.IsTrue   (roamingNetwork.   ContainsEVSE(EVSE_Id.Parse("DE*GEF*E1234*5678*1")));
                    Assert.IsNotNull(roamingNetwork.   GetEVSEById (EVSE_Id.Parse("DE*GEF*E1234*5678*1")));

                    Assert.IsTrue   (DE_GEF.           ContainsEVSE(EVSE_Id.Parse("DE*GEF*E1234*5678*1")));
                    Assert.IsNotNull(DE_GEF.           GetEVSEById (EVSE_Id.Parse("DE*GEF*E1234*5678*1")));

                    Assert.IsTrue   (DE_GEF_P0001.     ContainsEVSE(EVSE_Id.Parse("DE*GEF*E1234*5678*1")));
                    Assert.IsNotNull(DE_GEF_P0001.     GetEVSEById (EVSE_Id.Parse("DE*GEF*E1234*5678*1")));

                    Assert.IsTrue   (DE_GEF_S0001_AAAA.ContainsEVSE(EVSE_Id.Parse("DE*GEF*E1234*5678*1")));
                    Assert.IsNotNull(DE_GEF_S0001_AAAA.GetEVSEById (EVSE_Id.Parse("DE*GEF*E1234*5678*1")));


                    Assert.AreEqual(1, DE_GEF_E1234_5678_1.Brands.Count);



                    DE_GEF_E1234_5678_1.Brands.Add(new Brand(
                                                       Id:            Brand_Id.Parse("openChargingCloud23"),
                                                       Name:          I18NString.Create(Languages.de, "Open Charging Cloud 23"),
                                                       Logo:          URL.Parse("https://open.charging.cloud/logos.json"),
                                                       Homepage:      URL.Parse("https://open.charging.cloud"),
                                                       DataLicenses:  new OpenDataLicense[] {
                                                                          OpenDataLicense.CreativeCommons_BY_SA_4
                                                                      }
                                                   ));


                    Assert.AreEqual(2, DE_GEF_E1234_5678_1.Brands.Count);


                    #region Setup OnEVSEDataChange listeners

                    var evseDataChanges = new List<String>();

                    DE_GEF_E1234_5678_1.OnDataChanged += async (Timestamp,
                                                                EventTrackingId,
                                                                EVSE,
                                                                PropertyName,
                                                                NewValue,
                                                                OldValue,
                                                                dataSource) => {

                        evseDataChanges.Add(String.Concat(EVSE.ToString(), ".", PropertyName, ": ", OldValue?.ToString() ?? "", " => ", NewValue?.ToString() ?? ""));

                    };


                    var chargingStationEVSEDataChanges = new List<String>();

                    DE_GEF_S0001_AAAA.OnEVSEDataChanged += async (Timestamp,
                                                                  EventTrackingId,
                                                                  EVSE,
                                                                  PropertyName,
                                                                  NewValue,
                                                                  OldValue,
                                                                  dataSource) => {

                        chargingStationEVSEDataChanges.Add(String.Concat(EVSE.ToString(), ".", PropertyName, ": ", OldValue?.ToString() ?? "", " => ", NewValue?.ToString() ?? ""));

                    };


                    var chargingPoolEVSEDataChanges = new List<String>();

                    DE_GEF_P0001.OnEVSEDataChanged += async (Timestamp,
                                                             EventTrackingId,
                                                             EVSE,
                                                             PropertyName,
                                                             NewValue,
                                                             OldValue,
                                                             dataSource) => {

                        chargingPoolEVSEDataChanges.Add(String.Concat(EVSE.ToString(), ".", PropertyName, ": ", OldValue?.ToString() ?? "", " => ", NewValue?.ToString() ?? ""));

                    };


                    var chargingStationOperatorEVSEDataChanges = new List<String>();

                    DE_GEF.OnEVSEDataChanged += async (Timestamp,
                                                       EventTrackingId,
                                                       EVSE,
                                                       PropertyName,
                                                       NewValue,
                                                       OldValue,
                                                       dataSource) => {

                        chargingStationOperatorEVSEDataChanges.Add(String.Concat(EVSE.ToString(), ".", PropertyName, ": ", OldValue?.ToString() ?? "", " => ", NewValue?.ToString() ?? ""));

                    };


                    var roamingNetworkEVSEDataChanges = new List<String>();

                    roamingNetwork.OnEVSEDataChanged += async (Timestamp,
                                                               EventTrackingId,
                                                               EVSE,
                                                               PropertyName,
                                                               NewValue,
                                                               OldValue,
                                                               dataSource) => {

                        roamingNetworkEVSEDataChanges.Add(String.Concat(EVSE.ToString(), ".", PropertyName, ": ", OldValue?.ToString() ?? "", " => ", NewValue?.ToString() ?? ""));

                    };

                    #endregion

                    DE_GEF_E1234_5678_1.Name.       Set(Languages.it, "namelalala");
                    DE_GEF_E1234_5678_1.Description.Set(Languages.it, "desclalala");

                    Assert.AreEqual(2, evseDataChanges.                       Count);
                    Assert.AreEqual(2, chargingStationEVSEDataChanges.        Count);
                    Assert.AreEqual(2, chargingPoolEVSEDataChanges.           Count);
                    Assert.AreEqual(2, chargingStationOperatorEVSEDataChanges.Count);
                    Assert.AreEqual(2, roamingNetworkEVSEDataChanges.         Count);

                    var now = Timestamp.Now;

                    DE_GEF_E1234_5678_1.MaxPower           = 123.45m;
                    DE_GEF_E1234_5678_1.MaxPower           = 234.56m;

                    DE_GEF_E1234_5678_1.MaxPowerRealTime   = 345.67m;
                    DE_GEF_E1234_5678_1.MaxPowerRealTime   = 456.78m;

                    DE_GEF_E1234_5678_1.MaxPowerPrognoses.Replace(new[] {
                                                                      new Timestamped<Decimal>(now + TimeSpan.FromMinutes(1), 567.89m),
                                                                      new Timestamped<Decimal>(now + TimeSpan.FromMinutes(2), 678.91m),
                                                                      new Timestamped<Decimal>(now + TimeSpan.FromMinutes(3), 789.12m)
                                                                  });

                    Assert.AreEqual(7, evseDataChanges.                       Count);
                    Assert.AreEqual(7, chargingStationEVSEDataChanges.        Count);
                    Assert.AreEqual(7, chargingPoolEVSEDataChanges.           Count);
                    Assert.AreEqual(7, chargingStationOperatorEVSEDataChanges.Count);
                    Assert.AreEqual(7, roamingNetworkEVSEDataChanges.         Count);

                    // The same again... must not call the data change listeners!
                    DE_GEF_E1234_5678_1.Name.       Set(Languages.it, "namelalala");
                    DE_GEF_E1234_5678_1.Description.Set(Languages.it, "desclalala");

                    DE_GEF_E1234_5678_1.MaxPower           = 234.56m;
                    DE_GEF_E1234_5678_1.MaxPowerRealTime   = 456.78m;
                    DE_GEF_E1234_5678_1.MaxPowerPrognoses.Replace(new[] {
                                                                      new Timestamped<Decimal>(now + TimeSpan.FromMinutes(1), 567.89m),
                                                                      new Timestamped<Decimal>(now + TimeSpan.FromMinutes(2), 678.91m),
                                                                      new Timestamped<Decimal>(now + TimeSpan.FromMinutes(3), 789.12m)
                                                                  });

                    Assert.AreEqual(7, evseDataChanges.                       Count);
                    Assert.AreEqual(7, chargingStationEVSEDataChanges.        Count);
                    Assert.AreEqual(7, chargingPoolEVSEDataChanges.           Count);
                    Assert.AreEqual(7, chargingStationOperatorEVSEDataChanges.Count);
                    Assert.AreEqual(7, roamingNetworkEVSEDataChanges.         Count);

                }

            }

        }

        #endregion



        #region EVSE_AdminStatus_Test()

        /// <summary>
        /// A test for the admin status.
        /// </summary>
        [Test]
        public void EVSE_AdminStatus_Test()
        {

            Assert.IsNotNull(roamingNetwork);
            Assert.IsNotNull(DE_GEF);
            Assert.IsNotNull(DE_GEF_P0001);
            Assert.IsNotNull(DE_GEF_S0001_AAAA);
            Assert.IsNotNull(DE_GEF_E0001_AAAA_1);

            if (roamingNetwork      is not null &&
                DE_GEF              is not null &&
                DE_GEF_P0001        is not null &&
                DE_GEF_S0001_AAAA   is not null &&
                DE_GEF_E0001_AAAA_1 is not null)
            {

                // Status entries are compared by their ISO 8601 timestamps!
                Thread.Sleep(1000);

                DE_GEF_E0001_AAAA_1.AdminStatus = EVSEAdminStatusTypes.InternalUse;
                Assert.AreEqual(EVSEAdminStatusTypes.InternalUse,          DE_GEF_E0001_AAAA_1.AdminStatus);
                Assert.AreEqual("internalUse, outOfService",               DE_GEF_E0001_AAAA_1.AdminStatusSchedule().Select(status => status.Value.ToString()).AggregateWith(", "));
                Assert.AreEqual(2,                                         DE_GEF_E0001_AAAA_1.AdminStatusSchedule().Count());

                Thread.Sleep(1000);

                DE_GEF_E0001_AAAA_1.AdminStatus = EVSEAdminStatusTypes.Operational;
                Assert.AreEqual(EVSEAdminStatusTypes.Operational,          DE_GEF_E0001_AAAA_1.AdminStatus);
                Assert.AreEqual("operational, internalUse, outOfService",  DE_GEF_E0001_AAAA_1.AdminStatusSchedule().Select(status => status.Value.ToString()).AggregateWith(", "));
                Assert.AreEqual(3,                                         DE_GEF_E0001_AAAA_1.AdminStatusSchedule().Count());


                Assert.AreEqual("1 entities; operational: 1 (100,00)", DE_GEF_E0001_AAAA_1.                                   GenerateAdminStatusReport().    ToString());
                Assert.AreEqual("1 entities; operational: 1 (100,00)", new IEVSE[]                    { DE_GEF_E0001_AAAA_1 }.GenerateAdminStatusReport().    ToString());
                Assert.AreEqual("1 entities; operational: 1 (100,00)", DE_GEF_S0001_AAAA.                                     GenerateEVSEAdminStatusReport().ToString());
                Assert.AreEqual("1 entities; operational: 1 (100,00)", new IChargingStation[]         { DE_GEF_S0001_AAAA }.  GenerateEVSEAdminStatusReport().ToString());
                Assert.AreEqual("1 entities; operational: 1 (100,00)", DE_GEF_P0001.                                          GenerateEVSEAdminStatusReport().ToString());
                Assert.AreEqual("1 entities; operational: 1 (100,00)", new IChargingPool[]            { DE_GEF_P0001 }.       GenerateEVSEAdminStatusReport().ToString());
                Assert.AreEqual("1 entities; operational: 1 (100,00)", DE_GEF.                                                GenerateEVSEAdminStatusReport().ToString());
                Assert.AreEqual("1 entities; operational: 1 (100,00)", new IChargingStationOperator[] { DE_GEF }.             GenerateEVSEAdminStatusReport().ToString());
                Assert.AreEqual("1 entities; operational: 1 (100,00)", roamingNetwork.                                        GenerateEVSEAdminStatusReport().ToString());


                var jsonStatusReport = DE_GEF_E0001_AAAA_1.GenerateAdminStatusReport().ToJSON();
                jsonStatusReport.Remove("timestamp");

                Assert.AreEqual("{\"@context\":\"https://open.charging.cloud/contexts/wwcp+json/evseAdminStatusReport\",\"count\":1,\"report\":{\"operational\":{\"count\":1,\"percentage\":100.0}}}",
                                jsonStatusReport.ToString(Newtonsoft.Json.Formatting.None));

            }

        }

        #endregion

        #region EVSE_Status_Test()

        /// <summary>
        /// A test for the admin status.
        /// </summary>
        [Test]
        public void EVSE_Status_Test()
        {

            Assert.IsNotNull(roamingNetwork);
            Assert.IsNotNull(DE_GEF);
            Assert.IsNotNull(DE_GEF_P0001);
            Assert.IsNotNull(DE_GEF_S0001_AAAA);
            Assert.IsNotNull(DE_GEF_E0001_AAAA_1);

            if (roamingNetwork      is not null &&
                DE_GEF              is not null &&
                DE_GEF_P0001        is not null &&
                DE_GEF_S0001_AAAA   is not null &&
                DE_GEF_E0001_AAAA_1 is not null)
            {

                // Status entries are compared by their ISO 8601 timestamps!
                Thread.Sleep(1000);

                DE_GEF_E0001_AAAA_1.Status = EVSEStatusTypes.Reserved;
                Assert.AreEqual(EVSEStatusTypes.Reserved,    DE_GEF_E0001_AAAA_1.Status);
                Assert.AreEqual("reserved, offline",         DE_GEF_E0001_AAAA_1.StatusSchedule().Select(status => status.Value.ToString()).AggregateWith(", "));
                Assert.AreEqual(2,                           DE_GEF_E0001_AAAA_1.StatusSchedule().Count());

                Thread.Sleep(1000);

                DE_GEF_E0001_AAAA_1.Status = EVSEStatusTypes.Error;
                Assert.AreEqual(EVSEStatusTypes.Error,       DE_GEF_E0001_AAAA_1.Status);
                Assert.AreEqual("error, reserved, offline",  DE_GEF_E0001_AAAA_1.StatusSchedule().Select(status => status.Value.ToString()).AggregateWith(", "));
                Assert.AreEqual(3,                           DE_GEF_E0001_AAAA_1.StatusSchedule().Count());


                Assert.AreEqual("1 entities; error: 1 (100,00)", DE_GEF_E0001_AAAA_1.                                   GenerateStatusReport().    ToString());
                Assert.AreEqual("1 entities; error: 1 (100,00)", new IEVSE[]                    { DE_GEF_E0001_AAAA_1 }.GenerateStatusReport().    ToString());
                Assert.AreEqual("1 entities; error: 1 (100,00)", DE_GEF_S0001_AAAA.                                     GenerateEVSEStatusReport().ToString());
                Assert.AreEqual("1 entities; error: 1 (100,00)", new IChargingStation[]         { DE_GEF_S0001_AAAA }.  GenerateEVSEStatusReport().ToString());
                Assert.AreEqual("1 entities; error: 1 (100,00)", DE_GEF_P0001.                                          GenerateEVSEStatusReport().ToString());
                Assert.AreEqual("1 entities; error: 1 (100,00)", new IChargingPool[]            { DE_GEF_P0001 }.       GenerateEVSEStatusReport().ToString());
                Assert.AreEqual("1 entities; error: 1 (100,00)", DE_GEF.                                                GenerateEVSEStatusReport().ToString());
                Assert.AreEqual("1 entities; error: 1 (100,00)", new IChargingStationOperator[] { DE_GEF }.             GenerateEVSEStatusReport().ToString());
                Assert.AreEqual("1 entities; error: 1 (100,00)", roamingNetwork.                                        GenerateEVSEStatusReport().ToString());


                var jsonStatusReport = DE_GEF_E0001_AAAA_1.GenerateStatusReport().ToJSON();
                jsonStatusReport.Remove("timestamp");

                Assert.AreEqual("{\"@context\":\"https://open.charging.cloud/contexts/wwcp+json/evseStatusReport\",\"count\":1,\"report\":{\"error\":{\"count\":1,\"percentage\":100.0}}}",
                                jsonStatusReport.ToString(Newtonsoft.Json.Formatting.None));

            }

        }

        #endregion


        #region EVSE_Tariff_Test()

        /// <summary>
        /// A test for charging tariffs at EVSEs.
        /// </summary>
        [Test]
        public void EVSE_Tariff_Test()
        {

            Assert.IsNotNull(roamingNetwork);
            Assert.IsNotNull(DE_GEF);
            Assert.IsNotNull(DE_GEF_P0001);
            Assert.IsNotNull(DE_GEF_S0001_AAAA);
            Assert.IsNotNull(DE_GEF_E0001_AAAA_1);

            if (roamingNetwork      is not null &&
                DE_GEF              is not null &&
                DE_GEF_P0001        is not null &&
                DE_GEF_S0001_AAAA   is not null &&
                DE_GEF_E0001_AAAA_1 is not null)
            {

                #region Setup OnEVSEDataChange listeners

                var evseDataChanges = new List<String>();

                DE_GEF_E0001_AAAA_1.OnDataChanged += async (Timestamp,
                                                            EventTrackingId,
                                                            EVSE,
                                                            PropertyName,
                                                            NewValue,
                                                            OldValue,
                                                            dataSource) => {

                    evseDataChanges.Add(String.Concat(EVSE.ToString(), ".", PropertyName, ": ", OldValue?.ToString() ?? "", " => ", NewValue?.ToString() ?? ""));

                };


                var chargingStationEVSEDataChanges = new List<String>();

                DE_GEF_S0001_AAAA.OnEVSEDataChanged += async (Timestamp,
                                                                EventTrackingId,
                                                                EVSE,
                                                                PropertyName,
                                                                NewValue,
                                                                OldValue,
                                                                dataSource) => {

                    chargingStationEVSEDataChanges.Add(String.Concat(EVSE.ToString(), ".", PropertyName, ": ", OldValue?.ToString() ?? "", " => ", NewValue?.ToString() ?? ""));

                };


                var chargingPoolEVSEDataChanges = new List<String>();

                DE_GEF_P0001.OnEVSEDataChanged += async (Timestamp,
                                                         EventTrackingId,
                                                         EVSE,
                                                         PropertyName,
                                                         NewValue,
                                                         OldValue,
                                                         dataSource) => {

                    chargingPoolEVSEDataChanges.Add(String.Concat(EVSE.ToString(), ".", PropertyName, ": ", OldValue?.ToString() ?? "", " => ", NewValue?.ToString() ?? ""));

                };


                var chargingStationOperatorEVSEDataChanges = new List<String>();

                DE_GEF.OnEVSEDataChanged += async (Timestamp,
                                                   EventTrackingId,
                                                   EVSE,
                                                   PropertyName,
                                                   NewValue,
                                                   OldValue,
                                                   dataSource) => {

                    chargingStationOperatorEVSEDataChanges.Add(String.Concat(EVSE.ToString(), ".", PropertyName, ": ", OldValue?.ToString() ?? "", " => ", NewValue?.ToString() ?? ""));

                };


                var roamingNetworkEVSEDataChanges = new List<String>();

                roamingNetwork.OnEVSEDataChanged += async (Timestamp,
                                                           EventTrackingId,
                                                           EVSE,
                                                           PropertyName,
                                                           NewValue,
                                                           OldValue,
                                                           dataSource) => {

                    roamingNetworkEVSEDataChanges.Add(String.Concat(EVSE.ToString(), ".", PropertyName, ": ", OldValue?.ToString() ?? "", " => ", NewValue?.ToString() ?? ""));

                };

                #endregion

                var tariffGroup_VW = DE_GEF.CreateChargingTariffGroup("_VW", I18NString.Create(Languages.de, "Volkswagen"));
                Assert.IsNotNull(tariffGroup_VW);

                if (tariffGroup_VW is not null)
                {

                    var tariff_3_98_60min = tariffGroup_VW.CreateChargingTariff(
                                                Id:              ChargingTariff_Id.Parse(DE_GEF.Id, tariffGroup_VW.Id, "_Tariff_3.98_per_60min"),
                                                Name:            I18NString.Create(Languages.de, "3,98 € / 60 min"),
                                                Description:     I18NString.Create(Languages.de, "3,98 € pro angefangener 60 min für alle Volkswagen"),
                                                Brand:           null,
                                                TariffURL:       null,
                                                Currency:        Currency.EUR,
                                                EnergyMix:       null,
                                                TariffElements:  new ChargingTariffElement[] {
                                                                     new ChargingTariffElement(

                                                                         new ChargingPriceComponent(
                                                                             Type:       ChargingDimensionTypes.TIME,
                                                                             Price:      3.98M,
                                                                             StepSize:   60*60
                                                                         )

                                                                     )
                                                                 }
                                            );
                    Assert.IsNotNull(tariff_3_98_60min);


                    var tariff_0_25_kWh   = tariffGroup_VW.CreateChargingTariff(
                                                Id:              ChargingTariff_Id.Parse(DE_GEF.Id, tariffGroup_VW.Id, "_Tariff_0.25_per_kWh"),
                                                Name:            I18NString.Create(Languages.de, "0,25 € / kWh"),
                                                Description:     I18NString.Create(Languages.de, "0,25 € pro kWh für alle Volkswagen"),
                                                Brand:           null,
                                                TariffURL:       null,
                                                Currency:        Currency.EUR,
                                                EnergyMix:       null,
                                                TariffElements:  new ChargingTariffElement[] {
                                                                     new ChargingTariffElement(

                                                                         new ChargingPriceComponent(
                                                                             Type:       ChargingDimensionTypes.ENERGY,
                                                                             Price:      0.25M,
                                                                             StepSize:   1000
                                                                         )

                                                                     )
                                                                 }
                                            );
                    Assert.IsNotNull(tariff_0_25_kWh);


                    if (tariff_3_98_60min is not null &&
                        tariff_0_25_kWh   is not null)
                    {

                        DE_GEF_E0001_AAAA_1.ChargingTariffs.Add(tariff_3_98_60min);
                        DE_GEF_E0001_AAAA_1.ChargingTariffs.Add(tariff_0_25_kWh);

                        Assert.AreEqual(2, DE_GEF_E0001_AAAA_1.ChargingTariffs.   Count);

                        Assert.AreEqual(2, evseDataChanges.                       Count);
                        Assert.AreEqual(2, chargingStationEVSEDataChanges.        Count);
                        Assert.AreEqual(2, chargingPoolEVSEDataChanges.           Count);
                        Assert.AreEqual(2, chargingStationOperatorEVSEDataChanges.Count);
                        Assert.AreEqual(2, roamingNetworkEVSEDataChanges.         Count);


                        var evseGroup_0_25_kWh  = DE_GEF.CreateEVSEGroup(
                                                      Id:         EVSEGroup_Id.Parse(DE_GEF.Id, tariffGroup_VW.Id, "_T0.25_per_kWh"),
                                                      Name:       I18NString.Create(Languages.de, "0,25 € / kWh"),
                                                      Tariff:     tariff_0_25_kWh,
                                                      MemberIds:  new[] {
                                                                      DE_GEF_E0001_AAAA_1.Id
                                                                  }
                                                  );

                        Assert.IsNotNull(evseGroup_0_25_kWh);


                    }

                }

            }

        }

        #endregion


    }

}
