using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

namespace PPh
{

    public static class CommonParams
    {
        public const int PROTOCOL_VERSION = 2;
        public const int DEFAULT_BUFLEN = 512;
        public const int CLIENT_UDP_PORT_START = 50000;
        public const int MAX_CLIENTS = 10;
        public const int OBSERVER_EYE_SIZE = 16; // pixels
        public enum ObserverType
        {
            Daphnia8x8 = 1,
			Daphnia16x16
        }
    }

    public enum MsgType
    {
	    // client to server
	    CheckVersion = 0,
	    GetStatistics,
	    GetState,
	    GetStateExt,
	    RotateLeft,
	    RotateRight,
	    RotateUp,
	    RotateDown,
	    MoveForward,
	    MoveBackward,
	    ClientToServerEnd, // !!!Always last
	    // server to client
	    CheckVersionResponse,
	    SocketBusyByAnotherObserver,
	    GetStatisticsResponse,
	    GetStateResponse,
	    GetStateExtResponse,
	    SendPhoton,
	    ToAdminSomeObserverPosChanged
    }

    [StructLayout(LayoutKind.Sequential, Pack=1)]
    [Serializable()]
    public class MsgBase
    {
        public byte m_type;

        public MsgBase(byte type) { m_type = type; }

        // Convert an object to a byte array
        public byte[] GetBuffer()
        {
    //        Type objectType = GetType();
            int objectSize = Marshal.SizeOf(this);
            IntPtr buffer = Marshal.AllocHGlobal(objectSize);
            Marshal.StructureToPtr(this, buffer, false);
            byte[] array = new byte[objectSize];
            Marshal.Copy(buffer, array, 0, objectSize);
            Marshal.FreeHGlobal(buffer);
            return array;
            }
    };
    //**************************************************************************************
    //************************************** Client ****************************************
    //**************************************************************************************
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgCheckVersion : MsgBase
    {
        public MsgCheckVersion() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.CheckVersion; }
        public uint m_clientVersion;
        public ulong m_observerId;
        public byte m_observerType;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgGetStatistics : MsgBase
    {
        public MsgGetStatistics() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.GetStatistics; }
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgGetState : MsgBase
    {
        public MsgGetState() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.GetState; }
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgGetStateExt : MsgBase
    {
        public MsgGetStateExt() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.GetStateExt; }
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgRotateLeft : MsgBase
    {
        public MsgRotateLeft() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.RotateLeft; }

        public byte m_value;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgRotateRight : MsgBase
    {
        public MsgRotateRight() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.RotateRight; }

        public byte m_value;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgRotateUp : MsgBase
    {
        public MsgRotateUp() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.RotateUp; }

        public byte m_value;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgRotateDown : MsgBase
    {
        public MsgRotateDown() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.RotateDown; }

        public byte m_value;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgMoveForward : MsgBase
    {
        public MsgMoveForward() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.MoveForward; }

        public byte m_value;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgMoveBackward : MsgBase
    {
        public MsgMoveBackward() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.MoveBackward; }

        public byte m_value;
    };
    //**************************************************************************************
    //************************************** Server ****************************************
    //**************************************************************************************
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgCheckVersionResponse : MsgBase
    {
        public MsgCheckVersionResponse() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.CheckVersionResponse; }
        public uint m_serverVersion;
        public ulong m_observerId;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgSocketBusyByAnotherObserver : MsgBase
    {
        public MsgSocketBusyByAnotherObserver() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.SocketBusyByAnotherObserver; }
        public uint m_serverVersion;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgGetStatisticsResponse : MsgBase
    {
        public MsgGetStatisticsResponse() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.GetStatisticsResponse; }
        public ushort m_universeThreadsCount;
        public uint m_fps; // quantum of time per second
        public uint m_observerThreadTickTime; // in microseconds
        public uint m_universeThreadMaxTickTime; // in microseconds
        public uint m_universeThreadMinTickTime; // in microseconds
        public ulong m_clientServerPerformanceRatio; // in milli how much client ticks more often than server ticks
        public ulong m_serverClientPerformanceRatio; // in milli how much server ticks more often than client ticks
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgGetStateResponse : MsgBase
    {
        public MsgGetStateResponse() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.GetStateResponse; }
        public ulong m_time;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public struct MsgGetStateResponseStruct
    {
        public byte m_type;
        public ulong m_time;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgGetStateExtResponse : MsgBase
    {
        public MsgGetStateExtResponse() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.GetStateExtResponse; }
        public VectorInt32Math m_pos;
        public ushort m_movingProgress;
        public short m_latitude;
        public short m_longitude;
        public uint m_eatenCrumbNum;
        public VectorInt32Math m_eatenCrumbPos;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgSendPhoton : MsgBase
    {
        public MsgSendPhoton() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.SendPhoton; }
        public EtherColor m_color;
        public byte m_posX;
        public byte m_posY;
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Serializable()]
    public class MsgToAdminSomeObserverPosChanged : MsgBase
    {
        public MsgToAdminSomeObserverPosChanged() : base(GetTypeEnum()) { }
        public static byte GetTypeEnum() { return (byte)MsgType.ToAdminSomeObserverPosChanged; }
        public ulong m_observerId;
        public VectorInt32Math m_pos;
        public short m_latitude;
        public short m_longitude;
    };
    
    // -----------------------------------------------------------
    public class ServerProtocol
    {
        public static T QueryMessage<T>(byte[] buf)
        {
            Type objectType = typeof(T);
            var methodInfo = objectType.GetMethod("GetTypeEnum");
            if (methodInfo != null)
            {
                var returnValue = methodInfo.Invoke(null, null);
                if ((byte)returnValue == buf[0])
                {
                    T instance = (T)Activator.CreateInstance(objectType);
                    int size = Marshal.SizeOf(instance);
                    IntPtr ptr = Marshal.AllocHGlobal(size);

                    Marshal.Copy(buf, 0, ptr, size);

                    instance = (T)Marshal.PtrToStructure(ptr, instance.GetType());
                    Marshal.FreeHGlobal(ptr);

                    return instance;
                }
            }

            return default(T);
        }
    };

} // namespace PPh