﻿using System;
using System.IO;
using Voron.Impl;

namespace Voron
{
    public unsafe class ValueReader
    {
        private readonly byte* _val;
        private readonly int _len;
        private int _pos;
        public int Length { get { return _len; } }

        public void Reset()
        {
            _pos = 0;
        }

        public ValueReader(byte* val, int len)
        {
            _val = val;
            _len = len;
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            fixed (byte* b = buffer)
                return Read(b + offset, count);
        }

        public int Read(byte* buffer, int count)
        {
            count = Math.Min(count, _len - _pos);

            NativeMethods.memcpy(buffer, _val + _pos, count);

            _pos += count;

            return count;
        }

        public int ReadInt32()
        {
            if (_len - _pos < sizeof(int))
                throw new EndOfStreamException();
            var val = *(int*)(_val + _pos);
            _pos += sizeof(int);
            return val;
        }

        public long ReadInt64()
        {
            if (_len - _pos < sizeof(long))
                throw new EndOfStreamException();
            var val = *(long*)(_val + _pos);
            _pos += sizeof(long);
            return val;
        }

        public string ToStringValue()
        {
            return new string((sbyte*)_val, 0, _len);
        }

        public byte[] ReadBytes(int length)
        {
            var size = Math.Min(length, _len - _pos);
            var buffer = new byte[size];
            Read(buffer, 0, size);
            return buffer;
        }

        public void CopyTo(Stream stream)
        {
            var buffer = new byte[4096];
            while (true)
            {
                var read = Read(buffer, 0, buffer.Length);
                if (read == 0)
                    return;
                stream.Write(buffer, 0, read);
            }
        }
    }
}