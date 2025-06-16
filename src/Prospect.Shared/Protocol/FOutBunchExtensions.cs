using System;
using Prospect.Unreal.Net.Packets.Bunch;
using Prospect.Unreal.Serialization;

namespace Prospect.Shared.Protocol;

public static class FOutBunchExtensions
{
    public static void WriteBoolean(this FOutBunch bunch, bool value)
    {
        bunch.WriteBit(value);
    }

    public static unsafe void WriteFloat(this FOutBunch bunch, float value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        fixed (byte* ptr = bytes)
        {
            bunch.Serialize(ptr, sizeof(float));
        }
    }
} 