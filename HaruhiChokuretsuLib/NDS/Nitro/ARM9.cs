﻿// This code is heavily based on code Gericom wrote for ErmiiBuild

using System;
using System.Collections.Generic;
using System.Linq;

namespace HaruhiChokuretsuLib.NDS.Nitro
{
    public class ARM9
    {
        private readonly uint _ramAddress;
        private readonly List<byte> _staticData;
        private readonly uint _start_ModuleParamsOffset;
        private readonly CRT0.ModuleParams _start_ModuleParams;

        private readonly List<CRT0.AutoLoadEntry> _autoLoadList;

        public ARM9(byte[] Data, uint RamAddress)
            : this(Data, RamAddress, FindModuleParams(Data)) { }

        public ARM9(byte[] data, uint ramAddress, uint moduleParamsOffset)
        {
            //Unimportant static footer! Use it for _start_ModuleParamsOffset and remove it.
            if (BitConverter.ToUInt32(data.Skip(data.Length - 0x0C).Take(4).ToArray()) == 0xDEC00621)
            {
                moduleParamsOffset = BitConverter.ToUInt32(data.Skip(data.Length - 8).Take(4).ToArray());
                data = data.Take(data.Length - 0x0C).ToArray();
            }

            _ramAddress = ramAddress;
            _start_ModuleParamsOffset = moduleParamsOffset;
            _start_ModuleParams = new CRT0.ModuleParams(data, moduleParamsOffset);
            if (_start_ModuleParams.CompressedStaticEnd != 0)
            {
                _start_ModuleParams = new CRT0.ModuleParams(data, moduleParamsOffset);
            }

            _staticData = data.Take((int)(_start_ModuleParams.AutoLoadStart - ramAddress)).ToList();

            _autoLoadList = new List<CRT0.AutoLoadEntry>();
            uint nr = (_start_ModuleParams.AutoLoadListEnd - _start_ModuleParams.AutoLoadListOffset) / 0xC;
            uint offset = _start_ModuleParams.AutoLoadStart - ramAddress;
            for (int i = 0; i < nr; i++)
            {
                var entry = new CRT0.AutoLoadEntry(data, _start_ModuleParams.AutoLoadListOffset - ramAddress + (uint)i * 0xC);
                entry.Data = data.Skip((int)offset).Take((int)entry.Size).ToList();
                _autoLoadList.Add(entry);
                offset += entry.Size;
            }
        }

        public byte[] GetBytes()
        {
            List<byte> bytes = new();
            bytes.AddRange(_staticData);
            _start_ModuleParams.AutoLoadStart = (uint)bytes.Count + _ramAddress;
            foreach (var autoLoad in _autoLoadList)
            {
                bytes.AddRange(autoLoad.Data);
            }
            _start_ModuleParams.AutoLoadListOffset = (uint)bytes.Count + _ramAddress;
            foreach (var autoLoad in _autoLoadList)
            {
                bytes.AddRange(autoLoad.GetEntryBytes());
            }
            _start_ModuleParams.AutoLoadListEnd = (uint)bytes.Count + _ramAddress;
            List<byte> moduleParamsBytes = _start_ModuleParams.GetBytes();
            bytes.RemoveRange((int)_start_ModuleParamsOffset, moduleParamsBytes.Count);
            bytes.InsertRange((int)_start_ModuleParamsOffset, moduleParamsBytes);
            return bytes.ToArray();
        }

        public void AddAutoLoadEntry(uint address, byte[] data)
        {
            _autoLoadList.Add(new CRT0.AutoLoadEntry(address, data));
        }

        public bool WriteU16LE(uint address, ushort value)
        {
            if (address > _ramAddress && address < _start_ModuleParams.AutoLoadStart)
            {
                _staticData.RemoveRange((int)(address - _ramAddress), 2);
                _staticData.InsertRange((int)(address - _ramAddress), BitConverter.GetBytes(value));
                return true;
            }
            foreach (var v in _autoLoadList)
            {
                if (address > v.Address && address < (v.Address + v.Size))
                {
                    v.Data.RemoveRange((int)(address - _ramAddress), 2);
                    v.Data.InsertRange((int)(address - _ramAddress), BitConverter.GetBytes(value));
                    return true;
                }
            }
            return false;
        }

        public uint ReadU32LE(uint address)
        {
            if (address > _ramAddress && address < _start_ModuleParams.AutoLoadStart)
            {
                return BitConverter.ToUInt32(_staticData.ToArray(), (int)(address - _ramAddress));
            }
            foreach (var v in _autoLoadList)
            {
                if (address > v.Address && address < (v.Address + v.Size))
                {
                    return BitConverter.ToUInt32(v.Data.ToArray(), (int)(address - v.Address));
                }
            }
            return 0xFFFFFFFF;
        }

        public bool WriteU32LE(uint address, uint value)
        {
            if (address > _ramAddress && address < _start_ModuleParams.AutoLoadStart)
            {
                _staticData.RemoveRange((int)(address - _ramAddress), 4);
                _staticData.InsertRange((int)(address - _ramAddress), BitConverter.GetBytes(value));
                return true;
            }
            foreach (var v in _autoLoadList)
            {
                if (address > v.Address && address < (v.Address + v.Size))
                {
                    v.Data.RemoveRange((int)(address - _ramAddress), 4);
                    v.Data.InsertRange((int)(address - _ramAddress), BitConverter.GetBytes(value));
                    return true;
                }
            }
            return false;
        }

        private static uint FindModuleParams(byte[] data)
        {
            return (uint)(data.IndexOfSequence(new byte[] { 0x21, 0x06, 0xC0, 0xDE, 0xDE, 0xC0, 0x06, 0x21 }) - 0x1C);
        }
    }
}
