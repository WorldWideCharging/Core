﻿/*
 * Copyright (c) 2013 Achim Friedland <achim.friedland@belectric.com>
 * This file is part of eMI3 Mockup <http://www.github.com/eMI3/Mockup>
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

#endregion

namespace de.eMI3
{

    [Flags]
    public enum ChargingStationFeatures
    {
        Pinpad,
        Screen
    }

}
