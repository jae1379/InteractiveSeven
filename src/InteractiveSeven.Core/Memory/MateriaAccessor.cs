﻿using InteractiveSeven.Core.Memory.Model;
using InteractiveSeven.Core.Settings;
using System;
using System.Linq;

namespace InteractiveSeven.Core.Memory
{
    public class MateriaAccessor : IMateriaAccessor
    {
        private readonly IMemoryAccessor _memory;
        private const ushort InvCapacity = 200;
        private const int ItemSize = 4;
        private static readonly IntPtr FirstAddress = new IntPtr(0xDC04B4);
        private ApplicationSettings Settings => ApplicationSettings.Instance;

        public MateriaAccessor(IMemoryAccessor memory)
        {
            _memory = memory;
        }

        public void AddMateria(byte materiaId, uint experience = 0)
        {
            var scanResult = _memory.ScanMem(Settings.ProcessName,
                FirstAddress, ItemSize, InvCapacity, IsEmpty);

            if (scanResult.BaseAddrOffset == -1) return;

            IntPtr address = IntPtr.Add(FirstAddress, scanResult.BaseAddrOffset);
            var materiaSlot = new MateriaSlot(materiaId, experience);
            _memory.WriteMem(Settings.ProcessName, address, materiaSlot.AsBytes());

            bool IsEmpty(byte[] bytes) => bytes.All(b => b == byte.MaxValue);
        }

        public void RemoveAllMateria()
        {
            int totalOffset = 0;
            int inventoryTotalSize = (InvCapacity * ItemSize);
            IntPtr nextAddress = IntPtr.Add(FirstAddress, totalOffset);

            do
            {
                var scanResult = _memory.ScanMem(Settings.ProcessName,
                    nextAddress, ItemSize, InvCapacity, HasItem);
                if (scanResult.BaseAddrOffset == -1) return;

                RemoveItem(totalOffset + scanResult.BaseAddrOffset);

                totalOffset += scanResult.BaseAddrOffset + ItemSize;
                nextAddress = IntPtr.Add(FirstAddress, totalOffset);
            } while (totalOffset < inventoryTotalSize);

            bool HasItem(byte[] bytes) => bytes.Any(b => b != byte.MaxValue);
            void RemoveItem(int addrOffset)
                => _memory.WriteMem(Settings.ProcessName, IntPtr.Add(FirstAddress, addrOffset),
                    new[] { byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue });
        }

    }
}