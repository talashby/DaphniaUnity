
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace PPh
{

    public sealed class ObserverClient
    {
        // CONSTANTS
        const string SERVER_IP = "127.0.0.1";
        const ulong EYE_IMAGE_DELAY = 5000; // quantum of time. Eye inertion
        const int STATISTIC_REQUEST_PERIOD = 900; // milliseconds

        bool m_isSimulationRunning = false;

        // udpclient object
        UdpClient m_clientUdp;
        IPEndPoint m_remoteEndPoint;

        ulong m_lastUpdateStateExtTime = 0;
        ulong m_lastStatisticRequestTime = 0;

        // vars
        ulong m_timeOfTheUniverse = 0;
        object m_observerStateParamsMutex = new object();
        object m_eyeTextureMutex = new object ();

        short m_latitude = 0;
        short m_longitude = 0;
        VectorInt32Math m_position = VectorInt32Math.ZeroVector;
        ushort m_movingProgress = 0;
        uint m_eatenCrumbNum = 0;
        bool m_isEatenCrumb = false;
        VectorInt32Math m_eatenCrumbPos = VectorInt32Math.ZeroVector;
        ulong m_lastTextureUpdateTime;

        EtherColor[,] m_eyeColorArray = new EtherColor[CommonParams.OBSERVER_EYE_SIZE, CommonParams.OBSERVER_EYE_SIZE]; // photon (x,y) placed to [CommonParams.OBSERVER_EYE_SIZE - y -1][x] for simple copy to texture
        ulong[,] m_eyeUpdateTimeArray = new ulong[CommonParams.OBSERVER_EYE_SIZE, CommonParams.OBSERVER_EYE_SIZE];

        // read Thread
        Thread m_simulationThread;

        // motor neurons
        bool m_isLeft = false, m_isRight = false, m_isUp = false, m_isDown = false, m_isForward = false, m_isBackward = false;

        // statistics
        object m_serverStatisticsMutex = new object();
        uint m_quantumOfTimePerSecond = 0;
        uint m_universeThreadsNum = 0;
        uint m_TickTimeMusAverageUniverseThreadsMin = 0;
        uint m_TickTimeMusAverageUniverseThreadsMax = 0;
        uint m_TickTimeMusAverageObserverThread = 0;
        ulong m_clientServerPerformanceRatio = 0;
        ulong m_serverClientPerformanceRatio = 0;

        ObserverClient() { }

        public static ObserverClient Instance { get { return Nested.source; } }

        class Nested
        {
            static Nested()
            {
            }

            internal static readonly ObserverClient source = new ObserverClient();
        }

        public void GetStateExtParams(out VectorInt32Math outPosition, out ushort outMovingProgress, out short outLatitude,
            out short outLongitude, out bool outIsEatenCrumb)
        {
            lock (m_observerStateParamsMutex)
            {
                outPosition = m_position;
                outMovingProgress = m_movingProgress;
                outLatitude = m_latitude;
                outLongitude = m_longitude;
                outIsEatenCrumb = m_isEatenCrumb;
            }
        }

        public void GetStatisticsParams(out uint outQuantumOfTimePerSecond, out uint outUniverseThreadsNum,
            out uint outTickTimeMusAverageUniverseThreadsMin, out uint outTickTimeMusAverageUniverseThreadsMax,
            out uint outTickTimeMusAverageObserverThread, out ulong outClientServerPerformanceRatio,
            out ulong outServerClientPerformanceRatio)
        {
            lock (m_serverStatisticsMutex)
            {
                outQuantumOfTimePerSecond = m_quantumOfTimePerSecond;
                outUniverseThreadsNum = m_universeThreadsNum;
                outTickTimeMusAverageUniverseThreadsMin = m_TickTimeMusAverageUniverseThreadsMin;
                outTickTimeMusAverageUniverseThreadsMax = m_TickTimeMusAverageUniverseThreadsMax;
                outTickTimeMusAverageObserverThread = m_TickTimeMusAverageObserverThread;
                outClientServerPerformanceRatio = m_clientServerPerformanceRatio;
                outServerClientPerformanceRatio = m_serverClientPerformanceRatio;
            }
        }

        public VectorInt32Math GrabEatenCrumbPos()
        {
            lock (m_observerStateParamsMutex)
            {
                if (m_isEatenCrumb)
                {
                    m_isEatenCrumb = false;
                    return m_eatenCrumbPos;
                }
            }
            return VectorInt32Math.ZeroVector;
        }

        public void StartSimulation()
        {
            m_isSimulationRunning = true;

            //size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MsgGetStateResponse));
            /*MsgGetStateResponse msg1 = new MsgGetStateResponse();
            msg1.m_time = 21;
            byte[] buf = msg1.GetBuffer();
            for (MsgGetStateResponse msg = ServerProtocol.QueryMessage<MsgGetStateResponse>(buf); msg != null; msg = null)
            {
                int eee = 0;
            }
            for (MsgGetState msg = ServerProtocol.QueryMessage<MsgGetState>(buf); msg != null; msg = null)
            {
                int eee = 0;
            }*/

            m_clientUdp = new UdpClient();

            for (int port = CommonParams.CLIENT_UDP_PORT_START; port < CommonParams.CLIENT_UDP_PORT_START + CommonParams.MAX_CLIENTS; ++port)
            {
                m_remoteEndPoint = new IPEndPoint(IPAddress.Parse(SERVER_IP), port);
                m_clientUdp.Client.ReceiveTimeout = 1000;
                {
                    MsgCheckVersion msg = new MsgCheckVersion();
                    msg.m_clientVersion = CommonParams.PROTOCOL_VERSION;
                    byte[] buffer = msg.GetBuffer();
                    m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
                }
                {
                    // receive bytes
                    IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
                    byte[] buffer = m_clientUdp.Receive(ref anyIP);
                    PPh.MsgCheckVersionResponse msgReceive = ServerProtocol.QueryMessage<MsgCheckVersionResponse>(buffer);
                    if ( msgReceive != null)
                    {
                        if (msgReceive.m_serverVersion == CommonParams.PROTOCOL_VERSION)
                        {
                            break;
                        }
                        else
                        {
                            // wrong protocol
                            m_remoteEndPoint = null;
                            break;
                        }
                    }
                }
                m_remoteEndPoint = null;
            }
            
 
            if (m_remoteEndPoint != null)
            {
                m_clientUdp.Client.Blocking = false;  // to enable non-blocking socket

                // create thread for UDP messages
                m_simulationThread = new Thread(new ThreadStart(ThreadCycle));
                m_simulationThread.IsBackground = true;
                m_simulationThread.Start();
            }
            else
            {
                // server not found
            }
        }

        public void StopSimulation()
        {
            if (m_isSimulationRunning)
            {
                // stop thread
            }
        }

        // Set motor neurons
        public void SetIsLeft(bool value) { m_isLeft = value; }
        public void SetIsRight(bool value) { m_isRight = value; }
        public void SetIsUp(bool value) { m_isUp = value; }
        public void SetIsDown(bool value) { m_isDown = value; }
        public void SetIsForward(bool value) { m_isForward = value; }
        public void SetIsBackward(bool value) { m_isBackward = value; }

        public EtherColor[,] GetEyeTexture()
        {
            EtherColor[,] eyeColorArray;
            ulong[,] eyeUpdateTimeArray;
            lock (m_eyeTextureMutex)
            {
                eyeColorArray = m_eyeColorArray;
                eyeUpdateTimeArray = m_eyeUpdateTimeArray;
            }
            for (uint yy = 0; yy < eyeColorArray.GetLength(0); ++yy)
            {
                for (uint xx = 0; xx < eyeColorArray.GetLength(1); ++xx)
                {
                    ulong timeDiff = m_timeOfTheUniverse - eyeUpdateTimeArray[yy, xx];
                    byte alpha = m_eyeColorArray[yy, xx].m_colorA;
                    if (timeDiff < EYE_IMAGE_DELAY)
                    {
                        alpha = (byte)(alpha * (EYE_IMAGE_DELAY - timeDiff) / EYE_IMAGE_DELAY);
                    }
                    else
                    {
                        alpha = 0;
                    }
                    eyeColorArray[yy, xx].m_colorA = alpha;
                }
            }

            return eyeColorArray;
        }

        void ThreadCycle()
        {
            while (m_isSimulationRunning)
            {
                PPhTick();
            }
        }

        ulong GetTimeMs()
        {
            return (ulong)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        }

        void PPhTick()
        {
            {
                MsgGetState msg = new MsgGetState();
                byte[] buffer = msg.GetBuffer();
                m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
            }
            if (GetTimeMs() - m_lastUpdateStateExtTime > 20)  // get position/orientation data every n milliseconds
            {
                m_lastUpdateStateExtTime = GetTimeMs();
                MsgGetStateExt msg = new MsgGetStateExt();
                byte[] buffer = msg.GetBuffer();
                m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
            }
            if (GetTimeMs() - m_lastStatisticRequestTime > STATISTIC_REQUEST_PERIOD)
            {
                m_lastStatisticRequestTime = GetTimeMs();
                MsgGetStatistics msg = new MsgGetStatistics();
                byte[] buffer = msg.GetBuffer();
                m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
            }
            if (m_isLeft)
            {
                MsgRotateLeft msg = new MsgRotateLeft();
                msg.m_value = 4;
                byte[] buffer = msg.GetBuffer();
                m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
            }
            if (m_isRight)
            {
                MsgRotateRight msg = new MsgRotateRight();
                msg.m_value = 4;
                byte[] buffer = msg.GetBuffer();
                m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
            }
            if (m_isUp)
            {
                MsgRotateDown msg = new MsgRotateDown();
                msg.m_value = 4;
                byte[] buffer = msg.GetBuffer();
                m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
            }
            if (m_isDown)
            {
                MsgRotateUp msg = new MsgRotateUp();
                msg.m_value = 4;
                byte[] buffer = msg.GetBuffer();
                m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
            }
            if (m_isForward)
            {
                MsgMoveForward msg = new MsgMoveForward();
                msg.m_value = 16;
                byte[] buffer = msg.GetBuffer();
                m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
            }
            if (m_isBackward)
            {
                MsgMoveBackward msg = new MsgMoveBackward();
                msg.m_value = 16;
                byte[] buffer = msg.GetBuffer();
                m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
            }

            // receive bytes
            IPEndPoint anyIP = new IPEndPoint(IPAddress.Any, 0);
            while (m_clientUdp.Available > 0)
		    {
                byte[] buffer = m_clientUdp.Receive(ref anyIP);
                if (buffer.Length > 0)
                {
                    switch ((MsgType)buffer[0])
                    {
                        case MsgType.GetStateResponse:
                            {
                                MsgGetStateResponse msg = ServerProtocol.QueryMessage<MsgGetStateResponse>(buffer);
                                m_timeOfTheUniverse = msg.m_time;
                            }
                            break;
                        case MsgType.GetStateExtResponse:
                            {
                                MsgGetStateExtResponse msg = ServerProtocol.QueryMessage<MsgGetStateExtResponse>(buffer);
                                lock (m_observerStateParamsMutex)
                                {
                                    m_latitude = msg.m_latitude;
                                    m_longitude = msg.m_longitude;
                                    m_position = msg.m_pos;
                                    m_movingProgress = msg.m_movingProgress;
                                    if (m_eatenCrumbNum < msg.m_eatenCrumbNum)
                                    {
                                        m_eatenCrumbNum = msg.m_eatenCrumbNum;
                                        m_eatenCrumbPos = msg.m_eatenCrumbPos;
                                        m_isEatenCrumb = true;
                                    }
                                }
                            }
                            break;
                        case MsgType.SendPhoton:
                			{
                                MsgSendPhoton msg = ServerProtocol.QueryMessage<MsgSendPhoton>(buffer);
                                // receive photons back // revert Y-coordinate because of texture format
                                // photon (x,y) placed to [CommonParams.OBSERVER_EYE_SIZE - y -1][x] for simple copy to texture
                                lock (m_eyeTextureMutex)
                                {
                                    m_eyeColorArray[CommonParams.OBSERVER_EYE_SIZE - msg.m_posY - 1, msg.m_posX] = msg.m_color;
                                    m_eyeUpdateTimeArray[CommonParams.OBSERVER_EYE_SIZE - msg.m_posY - 1, msg.m_posX] = m_timeOfTheUniverse;
                                }
                            }
                            break;
                        case MsgType.GetStatisticsResponse:
                            {
                                MsgGetStatisticsResponse msg = ServerProtocol.QueryMessage<MsgGetStatisticsResponse>(buffer);
                                lock (m_serverStatisticsMutex)
                                {
                                    m_quantumOfTimePerSecond = msg.m_fps;
                                    m_TickTimeMusAverageObserverThread = msg.m_observerThreadTickTime;
                                    m_TickTimeMusAverageUniverseThreadsMin = msg.m_universeThreadMinTickTime;
                                    m_TickTimeMusAverageUniverseThreadsMax = msg.m_universeThreadMaxTickTime;
                                    m_universeThreadsNum = msg.m_universeThreadsCount;
                                    m_clientServerPerformanceRatio = msg.m_clientServerPerformanceRatio;
                                    m_serverClientPerformanceRatio = msg.m_serverClientPerformanceRatio;
                                }
                            }
                            break;
                    }
                }
            }
        }

    }

} // namespace PPh