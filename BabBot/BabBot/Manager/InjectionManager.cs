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

using System;
using System.Collections.Generic;
using System.Threading;
using BabBot.Wow;
using Magic;

namespace BabBot.Manager
{
    public class InjectionManager
    {
        // Shortcut to the BM instance
        // cache of GUID vs Relation to local player. These are static (more or less)
        private readonly IDictionary<ulong, Descriptor.eUnitReaction> RelationCache =
            new Dictionary<ulong, Descriptor.eUnitReaction>();

        private readonly IDictionary<string, uint> SpellIdCache = new Dictionary<string, uint>();

        private BlackMagic wow
        {
            get { return ProcessManager.WowProcess; }
        }


        /// <summary>
        /// Adds code to the FASM assembly stack to update the Current Manager. 
        /// Used at the beginning of many injected function calls.
        /// </summary>
        private void AsmUpdateCurMgr()
        {
            wow.Asm.AddLine("FS mov eax, [0x2C]");
            wow.Asm.AddLine("mov eax, [eax]");
            wow.Asm.AddLine("add eax, 0x10");
            wow.Asm.AddLine("mov dword [eax], {0}", Globals.CurMgr);
        }

        #region VMT

        /// <summary>
        /// Calls a VMT method for the specified object.
        /// Based on: http://www.mmowned.com/forums/wow-memory-editing/178133-event-based-framework-2.html#post1162121
        /// </summary>
        /// <param name="objAddress">Base Address of the object</param>
        /// <param name="method">Method offset. Note that this offset should already have been multiplied by 4 to get the absolute offset.</param>
        /// <returns>Address of the result if there is one.</returns>
        public uint CallVMT(uint objAddress, uint method)
        {
            ProcessManager.SuspendMainWowThread();

            uint codecave = wow.AllocateMemory();
            uint VMT = wow.ReadUInt(objAddress);
            uint result;

            wow.Asm.Clear();
            AsmUpdateCurMgr();
            wow.Asm.AddLine("mov ecx, {0}", objAddress);
            wow.Asm.AddLine("call {0}", wow.ReadUInt(VMT + method));
            wow.Asm.AddLine("retn");

            try
            {
                result = wow.Asm.InjectAndExecute(codecave);
                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                wow.FreeMemory(codecave);
                ProcessManager.ResumeMainWowThread();
            }
            return result;
        }

        /// <summary>
        /// Helper override which takes a WowObject instance instead of base address.
        /// </summary>
        /// <param name="obj">WowObject Instance</param>
        /// <param name="method">Method offset. Note that this offset should already have been multiplied by 4 to get the absolute offset.</param>
        /// <returns>Address of the result if there is one.</returns>
        public uint CallVMT(WowObject obj, uint method)
        {
            return CallVMT(obj.ObjectPointer, method);
        }

        public uint Interact(uint objAddress)
        {
            return CallVMT(objAddress, Globals.VMT.Interact);
        }

        public uint Interact(WowObject obj)
        {
            return Interact(obj);
        }

        public string GetName(uint objAddress)
        {
            return wow.ReadASCIIString(CallVMT(objAddress, Globals.VMT.GetName), 255).Trim();
        }

        public string GetName(WowObject obj)
        {
            return GetName(obj.ObjectPointer);
        }

        #endregion

        #region SelectUnit

        /// <summary>
        /// Selects a given entity in game.
        /// Based on: http://www.mmowned.com/forums/wow-memory-editing/213915-using-selecttarget.html
        /// </summary>
        /// <param name="guid">GUID of the object to select.</param>
        public void SelectUnit(ulong guid)
        {
            ProcessManager.SuspendMainWowThread();

            uint codecave = wow.AllocateMemory();
            var hi = (uint) ((guid >> 32) & 0xFFFFFFFF);
            var lo = (uint) (guid & 0xFFFFFFFF);

            wow.Asm.Clear();
            AsmUpdateCurMgr();
            wow.Asm.AddLine("push {0}", hi);
            wow.Asm.AddLine("push {0}", lo);
            wow.Asm.AddLine("call {0}", Globals.Functions.SelectUnit);
            wow.Asm.AddLine("add esp, 0x08");
            wow.Asm.AddLine("retn");

            try
            {
                wow.Asm.InjectAndExecute(codecave);
                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                wow.FreeMemory(codecave);
                ProcessManager.ResumeMainWowThread();
            }
        }

        /// <summary>
        /// Helper override which takes a WowObject instance instead of GUID.
        /// </summary>
        /// <param name="obj">WowObject Instance</param>
        public void SelectUnit(WowObject obj)
        {
            SelectUnit(obj.Guid);
        }

        #endregion

        #region Spells

        /// <summary>
        /// Casts a spell using the numeric ID of the spell. This ID is the same as used by wowhead, 
        /// or can be retrieved using GetSpellIdByName()
        /// </summary>
        /// <param name="id">The ID of the spell to cast.</param>
        public void CastSpellByID(uint id)
        {
            ProcessManager.SuspendMainWowThread();

            uint codecave = wow.AllocateMemory();

            wow.Asm.Clear();
            AsmUpdateCurMgr();
            wow.Asm.AddLine("push 0");
            wow.Asm.AddLine("push 0");
            wow.Asm.AddLine("push 0");
            wow.Asm.AddLine("push {0}", id);
            wow.Asm.AddLine("call {0}", Globals.Functions.CastSpellById);
            wow.Asm.AddLine("add esp,16");
            wow.Asm.AddLine("retn");

            try
            {
                wow.Asm.InjectAndExecute(codecave);
                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                wow.FreeMemory(codecave);
                ProcessManager.ResumeMainWowThread();
            }
        }

        /// <summary>
        /// Returns the name of the spell given the ID.
        /// </summary>
        /// <param name="name">ID of the spell</param>
        /// <returns>Spell name</returns>
        public uint GetSpellIdByName(string name)
        {
            // If the spell id is in the cache, return that instead of hitting wow
            if (SpellIdCache.ContainsKey(name))
            {
                return SpellIdCache[name];
            }

            ProcessManager.SuspendMainWowThread();

            uint codecave = wow.AllocateMemory();
            uint stringcave = wow.AllocateMemory(name.Length + 1);
            uint result = 0;

            wow.WriteASCIIString(stringcave, name);

            wow.Asm.Clear();
            AsmUpdateCurMgr();
            wow.Asm.AddLine("push " + 0x0019F9BC); //not sure what this is, seems to be static in olly
            wow.Asm.AddLine("push " + stringcave);
            wow.Asm.AddLine("call " + Globals.Functions.GetSpellIdByName);
            wow.Asm.AddLine("add esp,8");
            wow.Asm.AddLine("retn");

            try
            {
                result = wow.Asm.InjectAndExecute(codecave);
                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                wow.FreeMemory(codecave);
                wow.FreeMemory(stringcave);
                ProcessManager.ResumeMainWowThread();
            }
            if (result != uint.MaxValue)
            {
                SpellIdCache.Add(name, result);
                return result;
            }
            else
            {
                return 0;
            }
        }

        /// <summary>
        /// Convenience function that casts a spell given the name of the spell.
        /// Note, this is somewhat unstable.
        /// </summary>
        /// <param name="name">Name of the spell to cast.</param>
        public void CastSpellByName(string name)
        {
            CastSpellByID(GetSpellIdByName(name));
        }

        /// <summary>
        /// Checks if a unit has a given buff by ID
        /// </summary>
        /// <param name="unit">Unit to check for buffs</param>
        /// <param name="BuffToCheck">Spell ID of the buff to check</param>
        /// <returns>True if unit has the given buff.</returns>
        public bool HasBuffById(WowUnit unit, uint BuffToCheck)
        {
            uint CurrentBuff = unit.ObjectPointer + Globals.FirstBuff;

            uint Buff = 1;
            while (Buff != 0 && BuffToCheck != 0)
            {
                Buff = wow.ReadUInt(CurrentBuff);
                if (Buff == BuffToCheck)
                {
                    return true;
                }

                CurrentBuff += Globals.NextBuff;
            }
            return false;
        }

        /// <summary>
        /// Convenience function that checks a buff given the name of the buff.
        /// </summary>
        /// <param name="name">Name of the buff to check.</param>
        public bool HasBuffByName(WowUnit unit, string BuffToCheck)
        {
            return HasBuffById(unit, GetSpellIdByName(BuffToCheck));
        }

        #endregion

        #region Unit Relation

        public Descriptor.eUnitReaction GetUnitRelation(WowUnit u1, WowUnit u2)
        {
            if (u1 == null || u2 == null)
            {
                return Descriptor.eUnitReaction.Unknown;
            }
            if (u1.ObjectPointer == ProcessManager.Player.ObjectPointer && RelationCache.ContainsKey(u2.Guid))
            {
                return RelationCache[u2.Guid];
            }

            ProcessManager.SuspendMainWowThread();

            uint codecave = wow.AllocateMemory();
            uint result;

            wow.Asm.Clear();
            AsmUpdateCurMgr();
            wow.Asm.AddLine("mov ecx, {0}", u1.ObjectPointer);
            wow.Asm.AddLine("push {0}", u2.ObjectPointer);
            wow.Asm.AddLine("call {0}", Globals.Functions.GetUnitRelation);
            wow.Asm.AddLine("retn");

            try
            {
                result = wow.Asm.InjectAndExecute(codecave);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                wow.FreeMemory(codecave);
                ProcessManager.ResumeMainWowThread();
            }

            if (u1.ObjectPointer == ProcessManager.Player.ObjectPointer)
            {
                RelationCache.Add(u2.Guid, (Descriptor.eUnitReaction) result);
            }
            return (Descriptor.eUnitReaction) result;
        }

        #endregion

        #region SetMovementFlag

        /// <summary>
        /// Sets the player's movement flag, which starts or stops movement in the direction
        /// specified by flag.
        /// </summary>
        /// <param name="flag">Movement flag represented the desired direction.</param>
        /// <param name="enable">True to start movement, False to stop.</param>
        public void SetMovementFlag(Globals.MovementFlags flag, bool enable)
        {
            ProcessManager.SuspendMainWowThread();

            uint codecave = wow.AllocateMemory();

            wow.Asm.Clear();
            AsmUpdateCurMgr();
            wow.Asm.AddLine("mov eax, {0}", Environment.TickCount);
            wow.Asm.AddLine("mov ecx, {0}", wow.ReadUInt(Globals.Functions.CInputControl));
                // Base of the CInputControl class
            wow.Asm.AddLine("push {0}", 0);
            wow.Asm.AddLine("push eax");
            wow.Asm.AddLine("push {0}", enable ? 1 : 0); // Enable
            wow.Asm.AddLine("push {0}", (uint) flag);
            wow.Asm.AddLine("call {0}", Globals.Functions.CInputControl_SetFlags);
            wow.Asm.AddLine("retn");

            try
            {
                wow.Asm.InjectAndExecute(codecave);
                // don't sleep
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                wow.FreeMemory(codecave);
                ProcessManager.ResumeMainWowThread();
            }
        }

        #endregion

        #region FindPattern

        /// <summary>
        /// Quick and dirty method for finding patterns in the wow executable.
        /// </summary>
        public uint FindPattern(string pattern, string mask)
        {
            return FindPattern(pattern, mask, 0, 0);
        }

        /// <summary>
        /// Quick and dirty method for finding patterns in the wow executable.
        /// </summary>
        public uint FindPattern(string pattern, string mask, int add)
        {
            return FindPattern(pattern, mask, add, 0);
        }

        /// <summary>
        /// Quick and dirty method for finding patterns in the wow executable.
        /// Note: this is not perfect and should not be relied on, verify results.
        /// </summary>
        public uint FindPattern(string pattern, string mask, int add, uint rel)
        {
            uint loc = SPattern.FindPattern(wow.ProcessHandle, wow.MainModule, pattern, mask);
            uint mod = 0;
            if (add != 0)
            {
                mod = wow.ReadUInt((uint) (loc + add));
            }

            Console.WriteLine("final: 0x{0:X08} + 0x{1:X} + 0x{2:X} =  0x{0:X08}", loc, mod, rel, loc + mod + rel);
            return loc + mod + rel;
        }

        #endregion

        #region LUA

        /// <summary>
        /// Execute a LUA command in the game. This will not return values from LUA functions.
        /// </summary>
        /// <param name="command">A LUA command string to execute.</param>
        public void Lua_DoString(string command)
        {
            // TODO: Replace this with a proper LUA implementation.

            ProcessManager.SuspendMainWowThread();

            uint codecave = wow.AllocateMemory();
            uint stringcave = wow.AllocateMemory(command.Length + 1);
            wow.WriteASCIIString(stringcave, command);

            wow.Asm.Clear();
            AsmUpdateCurMgr();

            wow.Asm.AddLine("mov eax, 0");
            wow.Asm.AddLine("push eax");
            wow.Asm.AddLine("mov eax, {0}", stringcave);
            wow.Asm.AddLine("push eax");
            wow.Asm.AddLine("push eax");
            wow.Asm.AddLine("call {0}", Globals.Functions.Lua_DoString);
            wow.Asm.AddLine("add esp, 0xC");
            wow.Asm.AddLine("retn");

            try
            {
                wow.Asm.InjectAndExecute(codecave);
                Thread.Sleep(10);
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                wow.FreeMemory(codecave);
                wow.FreeMemory(stringcave);
                ProcessManager.ResumeMainWowThread();
            }
        }

        /// <summary>
        /// This calls the LUA GetLocalizedText function to return the value of a variable obtained through DoString
        /// </summary>
        /// <param name="variable">Name of the variable</param>
        /// <returns>Content of the variable we're querying</returns>
        public String Lua_GetLocalizedText(string variable)
        {
            // TODO: Replace this with a proper LUA implementation.
            // TODO: Check if this works

            ProcessManager.SuspendMainWowThread();

            String sResult = "null";

            uint codecave = wow.AllocateMemory();
            uint stringcave = wow.AllocateMemory(variable.Length + 1);
            wow.WriteASCIIString(stringcave, variable);

            wow.Asm.Clear();
            AsmUpdateCurMgr();

            wow.Asm.AddLine("mov ecx, {0}", ProcessManager.Player.ObjectPointer);
            wow.Asm.AddLine("push {0}", -1);
            wow.Asm.AddLine("push {0}", stringcave);
            wow.Asm.AddLine("call {0}", Globals.Functions.Lua_GetLocalizedText);
            wow.Asm.AddLine("retn");

            try
            {
                uint result = wow.Asm.InjectAndExecute(codecave);
                Thread.Sleep(10);

                if (result != 0)
                {
                    sResult = wow.ReadASCIIString(result, 256);
                }
            }
            catch (Exception e)
            {
                throw e;
            }
            finally
            {
                wow.FreeMemory(codecave);
                wow.FreeMemory(stringcave);
                ProcessManager.ResumeMainWowThread();
            }

            return sResult;
        }

        #endregion
    }
}