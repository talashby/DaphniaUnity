using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        PPh.ObserverClient.Instance.StartSimulation();
    }

    private static Texture2D _staticRectTexture;
    private static GUIStyle _staticRectStyle;

    // Note that this function is only meant to be called from OnGUI() functions.
    public static void GUIDrawRect(Rect position, Color color)
    {
        if (_staticRectTexture == null)
        {
            _staticRectTexture = new Texture2D(1, 1);
        }

        if (_staticRectStyle == null)
        {
            _staticRectStyle = new GUIStyle();
        }

        _staticRectTexture.SetPixel(0, 0, color);
        _staticRectTexture.Apply();

        _staticRectStyle.normal.background = _staticRectTexture;

        GUI.Box(position, GUIContent.none, _staticRectStyle);


    }

    void OnGUI()
    {
        var eyeColorArray = PPh.ObserverClient.Instance.GetEyeTexture();
        for (uint yy = 0; yy < eyeColorArray.GetLength(0); ++yy)
        {
            for (uint xx = 0; xx < eyeColorArray.GetLength(1); ++xx)
            {
                var color = eyeColorArray[yy, xx];
                GUIDrawRect(new Rect(xx*20, yy*20, 20, 20), new Color(color.m_colorR / 255.0f, color.m_colorG / 255.0f, color.m_colorB / 255.0f, color.m_colorA / 255.0f));
            }
        }


        // check crumb eaten
        VectorInt32Math outPosition; ushort outMovingProgress; short outLatitude; short outLongitude; bool outIsEatenCrumb;
        PPh.ObserverClient.Instance.GetStateExtParams(out outPosition, out outMovingProgress, out outLatitude, out outLongitude, out outIsEatenCrumb);
        if (outIsEatenCrumb)
        {
            PPh.ObserverClient.Instance.GrabEatenCrumbPos();
            Camera.main.GetComponent<AudioSource>().Play();
        }

        string strOut = "STATISTICS:";
        {
            uint outQuantumOfTimePerSecond;
            uint outUniverseThreadsNum;
            uint outTickTimeMusAverageUniverseThreadsMin;
            uint outTickTimeMusAverageUniverseThreadsMax;
            uint outTickTimeMusAverageObserverThread;
            ulong outClientServerPerformanceRatio;
            ulong outServerClientPerformanceRatio;
            PPh.ObserverClient.Instance.GetStatisticsParams(out outQuantumOfTimePerSecond, out outUniverseThreadsNum,
                out outTickTimeMusAverageUniverseThreadsMin, out outTickTimeMusAverageUniverseThreadsMax,
                out outTickTimeMusAverageObserverThread, out outClientServerPerformanceRatio, out outServerClientPerformanceRatio);
            strOut += "\nFPS (quantum of time per second): " + outQuantumOfTimePerSecond;
            strOut += "\nUniverse threads count: " + outUniverseThreadsNum;
            if (outUniverseThreadsNum > 0)
            {
                strOut += "\nTick time(ms). Observer thread: " + (outTickTimeMusAverageObserverThread / 1000.0f);
                strOut += "\nTick time(ms). Fastest universe thread: " + (outTickTimeMusAverageUniverseThreadsMin / 1000.0f);
                strOut += "\nTick time(ms). Slowest universe thread: " + (outTickTimeMusAverageUniverseThreadsMax / 1000.0f);
            }
            strOut += "\nClient-Server performance ratio: " + (outClientServerPerformanceRatio / 1000.0f);
            strOut += "\nServer-Client performance ratio: " + (outServerClientPerformanceRatio / 1000.0f);
            strOut += "\nPosition: (" + outPosition.m_posX + ", " + outPosition.m_posY + ", " + outPosition.m_posZ + ")";
            strOut += ("\nLattitude: ") + (outLatitude);
            strOut += ("\nLongitude: ") + (outLongitude);
        }
        GUI.enabled = true;
        Camera cam = Camera.main;
        Vector3 pos = cam.WorldToScreenPoint(new Vector3(0, 5, 0));
        GUI.Label(new Rect(pos.x, Screen.height - pos.y, 300, 230), strOut);
    }

    // Update is called once per frame
    void Update()
    {
        // Set motor neurons
        PPh.ObserverClient.Instance.SetIsLeft(Input.GetKey(KeyCode.LeftArrow));
        PPh.ObserverClient.Instance.SetIsRight(Input.GetKey(KeyCode.RightArrow));
        PPh.ObserverClient.Instance.SetIsUp(Input.GetKey(KeyCode.UpArrow));
        PPh.ObserverClient.Instance.SetIsDown(Input.GetKey(KeyCode.DownArrow));
        PPh.ObserverClient.Instance.SetIsForward(Input.GetKey(KeyCode.Space));
        PPh.ObserverClient.Instance.SetIsBackward(Input.GetKey(KeyCode.Slash));
    }

    void OnApplicationQuit()
    {
        PPh.ObserverClient.Instance.StopSimulation();
    }
}
