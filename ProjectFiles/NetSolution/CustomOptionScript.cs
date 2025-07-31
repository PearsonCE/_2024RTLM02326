#region Using directives
using System;
using UAManagedCore;
using OpcUa = UAManagedCore.OpcUa;
using FTOptix.UI;
using FTOptix.NativeUI;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using FTOptix.CoreBase;
using FTOptix.Recipe;
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
using System.Linq;
using System.Net;
#endregion

public class CustomOptionScript : BaseNetLogic
{
    [ExportMethod]
    public void CreateNewCustomOption()
    {
        string Custom_Index = LogicObject.GetVariable("Custom Index").Value;
        LocalizedText Custom_Description = (LocalizedText)LogicObject.GetVariable("Custom Description").Value;
        bool OverwriteExistingCustomDesc = LogicObject.GetVariable("OverwriteExisting_CustomDescription").Value;
        UAObject customDescObject = (UAObject)Project.Current.Get("Model/Descriptions/CustomOptionsDescriptions");
        UAVariable customDescriptionIndex = FindCustomDescriptionIndex(Custom_Index);



        if (OverwriteExistingCustomDesc && (customDescriptionIndex == null))
        {
            Log.Error($"Update Aborted. <b>CustomOptionsDescription{Custom_Index}</b> does not exist in Model/Descriptions/CustomOptionsDescriptions.");
            return;
        }

        // Overwrite Custom_Descriptions
        UAVariable customDescriptionVariable = FindCustomDescriptionIndex(Custom_Index);
        LocalizedText customDescription = customDescriptionVariable?.Value.Value as LocalizedText ?? new LocalizedText("", "");

        if (customDescription.Text.ToLower() == "spare" || customDescription.TextId.ToLower() == "spare" || OverwriteExistingCustomDesc)
        {
            UpdateLocalizedTextCustom(customDescriptionIndex, Custom_Description, Custom_Index);

        }
        else
        {
            Log.Info("Update Aborted: Overwriting existing Custom Description is not enabled");
        }
    }

    #region Custom_Description moved to CustomOptionsDescription (Object)

    private void UpdateLocalizedTextCustom(UAVariable targetCustDescVar, LocalizedText newCustomDescription, string Custom_Index)
    {
        try
        {
            string handleLocalizedText;
            if (newCustomDescription.TextId == "")
            {
                handleLocalizedText = newCustomDescription.Text;
            }
            else
            {
                handleLocalizedText = newCustomDescription.TextId;
            }
            if (newCustomDescription == null)
            {
                Log.Error($"Invalid LocalizedText for <b>CP_Index: {Custom_Index}</b>");
                return;
            }

            if (targetCustDescVar != null)
            {
                var currentValue = targetCustDescVar.Value;
                targetCustDescVar.SetValue(newCustomDescription);

                string encodedText = WebUtility.HtmlEncode(handleLocalizedText); // Encodes only for log so we can see '&" symbol


                Log.Info($"<b>CustomOptionsDescription{Custom_Index}</b> has been updated to: <b>{encodedText}</b> at the path Model/Descriptions/CustomOptionsDescriptions");
            }
            else
            {
                Log.Error($"The target Description is null for Custom_Index: <b>{Custom_Index}</b>. Verify Custom Options index exists.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error while updating the description paired with index {Custom_Index}: {ex.Message}");
        }
    }

    private UAVariable FindCustomDescriptionIndex(string CP_Index)
    {
        // string index = CP_Index.Value
        UAVariable foundCustomDesc = new UAVariable();
        try
        {
            foundCustomDesc = (UAVariable)Project.Current.GetVariable($"Model/Descriptions/CustomOptionsDescriptions/CustomOptionsDescription{CP_Index}");
        }
        catch (Exception ex)
        {
            Log.Error($"Trouble parsing Custom Description at CustomOptionsDescription{CP_Index}: {ex}");
            return null;
        }
        return foundCustomDesc;

    }

    #endregion 



}
