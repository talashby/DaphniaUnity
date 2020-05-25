﻿
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
        private const string SERVER_IP = "127.0.0.1";
        private bool m_isSimulationRunning = false;
        private const int UPDATE_EYE_TEXTURE_OUT = 20; // milliseconds

        // udpclient object
        private UdpClient m_clientUdp;
        private IPEndPoint m_remoteEndPoint;
        private long m_lastUpdateStateExtTime = 0;

        // vars
        private ulong m_timeOfTheUniverse = 0;
        private object m_observerStateParamsMutex = new object();

        short m_latitude = 0;
        short m_longitude = 0;
        VectorInt32Math m_position = VectorInt32Math.ZeroVector;
        ushort m_movingProgress = 0;
        uint m_eatenCrumbNum = 0;
        bool m_isEatenCrumb = false;
        VectorInt32Math m_eatenCrumbPos = VectorInt32Math.ZeroVector;

        // read Thread
        private Thread m_simulationThread;

        // motor neurons
        bool m_isLeft = false, m_isRight = false, m_isUp = false, m_isDown = false, m_isForward = false, m_isBackward = false;

        private ObserverClient() { }

        public static ObserverClient Instance { get { return Nested.source; } }

        private class Nested
        {
            static Nested()
            {
            }

            internal static readonly ObserverClient source = new ObserverClient();
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

        private void ThreadCycle()
        {
            while (m_isSimulationRunning)
            {
                PPhTick();
            }
        }

        private void PPhTick()
        {
            {
                MsgGetState msg = new MsgGetState();
                byte[] buffer = msg.GetBuffer();
                m_clientUdp.Send(buffer, buffer.Length, m_remoteEndPoint);
            }
            if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - m_lastUpdateStateExtTime > UPDATE_EYE_TEXTURE_OUT)
            {
                m_lastUpdateStateExtTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                MsgGetStateExt msg = new MsgGetStateExt();
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
                    }
                }
            }
        }

        public void StopSimulation()
        {
            if (m_isSimulationRunning)
            {
                // stop thread
            }
        }

    }

} // namespace PPh