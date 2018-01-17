﻿/*
 * Copyright (c) 2014-2018 GraphDefined GmbH <achim.friedland@graphdefined.com>
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

using org.GraphDefined.Vanaheimr.Illias;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections;

#endregion

namespace org.GraphDefined.WWCP
{

    public class PriorityList<T> : IEnumerable<T>
    {

        #region Data

        private readonly ConcurrentDictionary<UInt32, T> _Services;

        #endregion

        public PriorityList()
        {

            this._Services = new ConcurrentDictionary<UInt32, T>();

        }


        public void Add(T iRemoteAuthorizeStartStop)
        {
            lock (_Services)
            {

                _Services.TryAdd(_Services.Count > 0
                                       ? _Services.Keys.Max() + 1
                                       : 1,
                                   iRemoteAuthorizeStartStop);

            }
        }



        public Task<T2[]> WhenAll<T2>(Func<T, Task<T2>> Work)
        {

            return Task.WhenAll(_Services.
                                    OrderBy(kvp => kvp.Key).
                                    Select (kvp => Work(kvp.Value)));

        }


        //private async Task<Tuple<T, T2>> Create<T, T2>(T TA, Task<T2> TB)
        //{
        //    return new Tuple<T, T2>(TA, await TB);
        //}


        public async Task<T2>

            WhenFirst<T2>(Func<T, Task<T2>>   Work,
                          Func<T2, Boolean>   Test,
                          Func<TimeSpan, T2>  Default)

        {

            var StartTime  = DateTime.UtcNow;

            var AllTasks = _Services.
                            OrderBy(kvp => kvp.Key).
                            Select (kvp => Work(kvp.Value)).
                            ToList();

            Task<T2> Result;

            try
            {

                do
                {

                    Result = await Task.WhenAny(AllTasks).ConfigureAwait(false);

                    AllTasks.Remove(Result);

                    if (!EqualityComparer<T2>.Default.Equals(Result.Result, default(T2)) &&
                        Test(Result.Result))
                    {
                        return Result.Result;
                    }

                }
                while (AllTasks.Count > 0);

            }
            catch (Exception e)
            {
                DebugX.LogT(e.Message);
            }

            return Default(DateTime.UtcNow - StartTime);

        }

        IEnumerator IEnumerable.GetEnumerator()
        {

            foreach (var service in _Services.
                                        OrderBy(kvp => kvp.Key).
                                        Select (kvp => kvp.Value))
            {
                yield return service;
            }

        }

        public IEnumerator<T> GetEnumerator()
        {

            foreach (var service in _Services.
                                        OrderBy(kvp => kvp.Key).
                                        Select (kvp => kvp.Value))
            {
                yield return service;
            }

        }

    }

}
