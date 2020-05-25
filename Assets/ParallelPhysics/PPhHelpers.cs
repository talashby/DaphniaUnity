using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct EtherColor
{
    public byte m_colorB;
    public byte m_colorG;
    public byte m_colorR;
    public byte m_colorA;
};

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct VectorInt32Math
{
    public VectorInt32Math(int posX, int posY, int posZ)
    {
        m_posX = posX;
        m_posY = posY;
        m_posZ = posZ;
    }

    public int m_posX;
    public int m_posY;
    public int m_posZ;

    public static VectorInt32Math ZeroVector = new VectorInt32Math(0, 0, 0);
};
