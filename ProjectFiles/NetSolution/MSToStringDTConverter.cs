#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.CoreBase;
using FTOptix.RAEtherNetIP;
using FTOptix.WebUI;
using FTOptix.Alarm;
using FTOptix.EventLogger;
using FTOptix.SQLiteStore;
using FTOptix.Store;
using FTOptix.System;
using FTOptix.Retentivity;
using FTOptix.Report;
using FTOptix.CommunicationDriver;
using FTOptix.SerialPort;
using FTOptix.UI;
using FTOptix.Core;
#endregion

public class MSToStringDTConverter : BaseNetLogic
{
    private PeriodicTask updateLogs;
    bool firstLoop = true;

    public override void Start()
    {
        updateLogs = new PeriodicTask(IncrementalVariable, 250, LogicObject);
        updateLogs.Start();
         
    }

    public override void Stop()
    {
        updateLogs?.Dispose();
        firstLoop = true;
    }
    private void IncrementalVariable()
    {
        // Action to be executed every tick of the periodic task

        int ms = LogicObject.GetVariable("MilliSeconds").Value;
        string time = LogicObject.GetVariable("DateTimeString").Value;

        // Convert milliseconds to TimeSpan
        TimeSpan timeSpan = TimeSpan.FromMilliseconds(ms);

        // Initialize formatted time string
        string mstoString;

        // Check if days should be displayed
        if (timeSpan.Days > 0)
        {
            string dayLabel = timeSpan.Days == 1 ? "Day" : "Days";
            mstoString = string.Format("{0} {1}, {2:D2}:{3:D2}:{4:D2}",
                                       timeSpan.Days,
                                       dayLabel,
                                       timeSpan.Hours,
                                       timeSpan.Minutes,
                                       timeSpan.Seconds);
        }
        else
        {
            mstoString = string.Format("{0:D2}:{1:D2}:{2:D2}",
                                       timeSpan.Hours,
                                       timeSpan.Minutes,
                                       timeSpan.Seconds);
        }

        // Set the formatted string back to the "time" variable
        LogicObject.GetVariable("DateTimeString").Value = mstoString;
    }



}
