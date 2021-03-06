﻿/*
    This file is part of BabBot.

    BabBot is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    BabBot is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with BabBot.  If not, see <http://www.gnu.org/licenses/>.
  
    Copyright 2009 BabBot Team
*/
using System.Collections.Generic;

namespace BabBot.Bot
{
    public class PlayerActionList : Dictionary<string, PlayerAction>
    {
    }

    public class PlayerAction
    {
        public bool Active;
        public Binding Binding;
        public float CoolDown;
        public string Name;
        public float Range;
        public bool SelfCast;
        public bool Toggle;

        // TODO: we have to keep track of time for cooldowns

        public PlayerAction(string iName, Binding iBinding, float iRange, float iCoolDown, bool iSelfCast, bool iToggle)
        {
            Name = iName;
            Binding = iBinding;
            Range = iRange;
            CoolDown = iCoolDown;
            SelfCast = iSelfCast;
            Toggle = iToggle;
        }
    }
}