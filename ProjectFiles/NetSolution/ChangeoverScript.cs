using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FTOptix.Core;
using FTOptix.CoreBase;
using FTOptix.HMIProject;
using FTOptix.NetLogic;
using UAManagedCore;
using System.Net;


public class ChangeoverScript : BaseNetLogic
{

    [ExportMethod]
    public void CreateNewChangePoint()
    {
        var proj = Project.Current;

        // Retrieve the CP_Index (Int32), Info_Text (LocalizedText), and CP_Description (LocalizedText) from the COscript Properties
        string CP_Index = LogicObject.GetVariable("CP Index").Value;
        LocalizedText Info_Text = (LocalizedText)LogicObject.GetVariable("CP Instructions").Value;
        LocalizedText CP_Description = (LocalizedText)LogicObject.GetVariable("CP Description").Value;


        bool OverwriteExistingCPPath = LogicObject.GetVariable("OverwriteExisting_CPImagePath").Value;
        bool OverwriteExistingCPInfo = LogicObject.GetVariable("OverwriteExisting_CPInstructions").Value;
        bool OverwriteExistingCPDesc = LogicObject.GetVariable("OverwriteExisting_CPDescription").Value;

        // Retrieve Key converters File Path to Write to later
        FTOptix.CoreBase.ValueMapConverterType converterCOPath = (ValueMapConverterType)Project.Current.Get("Data/Converters/COMapImagePath");
        FTOptix.CoreBase.ValueMapConverterType converterCOInfo = (ValueMapConverterType)Project.Current.Get("Data/Converters/COMapImageInfo");

        //Retrieve the CPDescription Object File Path to Write to later
        UAObject cpDescriptionObject = (UAObject)Project.Current.Get("Model/Descriptions/CPDescriptions");

        // Retrieve collections of child objects from the first element of the converter's children
        var converterMapPathPairs = converterCOPath.Children.First().Children.Cast<UAObject>(); //use First() instead of Element(0)
        var converterInfoPairs = converterCOInfo.Children.First().Children.Cast<UAObject>();

        // Find the child pair in the path and info converter based on CP_Index match
        UAObject pathChildPair = FindChildPair(converterMapPathPairs, CP_Index);
        UAObject infoChildPair = FindChildPair(converterInfoPairs, CP_Index);

        var imagePathVariable = LogicObject.GetVariable("CP Image Path");

        //Grab corresponding key variable or Index for the image path and info text
        UAVariable imageKey_Index = GetInfoKeyIndex(pathChildPair);
        UAVariable infoKey_Index = GetInfoKeyIndex(infoChildPair);

        // Convert the string image path to ResourceUri
        string imagePathConvert = imagePathVariable.Value.ToString();
        ResourceUri ImagePathUri = ResourceUri.FromProjectRelativePath(imagePathConvert);

        // Find the target description index variable in the CPDescriptions/custom Object
        UAVariable targetDescriptionIndex = FindCPDescriptionIndex(CP_Index);



        #region Error Handling

        if (pathChildPair == null || imagePathVariable == null || infoChildPair == null || infoKey_Index == null || imageKey_Index == null ||
            targetDescriptionIndex == null)
        {
            if (infoKey_Index == null || imageKey_Index == null)
            {
                Log.Error("Failed to retrieve Index. Check Key changes, missing Keys, or object type edits.");
            }

            var missingElements = new List<string>();
            var missingPaths = new List<string>();

            if (pathChildPair == null || imagePathVariable == null)
            {
                missingElements.Add("COMapImagePath");
                missingPaths.Add("Data/Converters/COMapImagePath");
            }

            if (infoChildPair == null)
            {
                missingElements.Add("COMapImageInfo");
                missingPaths.Add("Data/Converters/COMapImageInfo");
            }

            if (missingElements.Count > 0)
            {
                Log.Error($"Update Aborted. <b>CP_Index: {CP_Index}</b> is missing in {string.Join(" and ", missingElements)}.");
                Log.Warning($"Ensure the converter(s) in the following file paths contain the expected key: {string.Join(" and ", missingPaths)}.");
            }

            if (targetDescriptionIndex == null)
            {
                Log.Error($"Update Aborted. <b>CPDescription{CP_Index}</b> does not exist in Model/Descriptions/CPDescriptions.");
            }

            return;
        }



        #endregion

        // Overwrite Image_Path
        imagePathVariable = pathChildPair.Children.ElementAt(1) as UAVariable;
        var imagePathValue = imagePathVariable?.Value;

        if (!OverwriteExistingCPPath)
        {
            if (imagePathValue == null || imagePathValue == "")
            {
                UpdateResourceURI(imageKey_Index, ImagePathUri, CP_Index);
            }
            else
            {
                Log.Info("Update Aborted: Overwrite not enabled for CP Image Path");
            }
        }
        else
        {
            UpdateResourceURI(imageKey_Index, ImagePathUri, CP_Index);
        }

        // Overwrite Info_Text
        UAVariable infoTextVariable = infoChildPair.Children.ElementAt(1) as UAVariable;
        LocalizedText infoTextVariableValue = infoTextVariable?.Value.Value as LocalizedText ?? new LocalizedText("", "");

        if (!OverwriteExistingCPInfo)
        {
            if (string.IsNullOrEmpty(infoTextVariableValue.Text))
            {
                UpdateLocalizedText(infoKey_Index, Info_Text, CP_Index);
            }
            else
            {
                Log.Info("Update Aborted: Overwrite not enabled for CP Insctructions");
            }
        }
        else
        {
            UpdateLocalizedText(infoKey_Index, Info_Text, CP_Index);
        }


        // Overwrite CP_Descriptions
        UAVariable cpDescriptionVariable = FindCPDescriptionIndex(CP_Index);
        LocalizedText cpDescription = cpDescriptionVariable?.Value.Value as LocalizedText ?? new LocalizedText("", "");

        if (!OverwriteExistingCPDesc)
        {
            if (cpDescription.Text.ToLower() == "spare" || cpDescription.TextId.ToLower() == "spare")
            {
                UpdateLocalizedTextObject(targetDescriptionIndex, CP_Description, CP_Index);
            }
            else
            {
                Log.Info("Update Aborted: Overwrite not enabled for CP Description");
            }
        }
        else
        {
            UpdateLocalizedTextObject(targetDescriptionIndex, CP_Description, CP_Index);
        }

    }




    #region Helper Methods: Locate Matching Indices for both Key converters and returns index info

    //Itterates through to find the matching Index (CP_Index) for both the Image Path and Info converters
    private UAObject FindChildPair(IEnumerable<UAObject> converterPairs, string CP_Index)
    {
        int.TryParse(CP_Index, out int index);
        return converterPairs.FirstOrDefault(pair =>
        {
            var keyVar = pair?.Children.ElementAtOrDefault(0) as UAVariable;
            return keyVar?.Value.Value.ToString() == index.ToString();
        });
    }


    // retrieves the second child (index 1) of a UAObject and returns it as a UAVariable or null
    private UAVariable GetInfoKeyIndex(UAObject infoChildPair)
    {
        return infoChildPair?.Children.Skip(1).FirstOrDefault() as UAVariable;
    }

    #endregion



    #region CP_Description moved to CPDescription (Object)

    private void UpdateLocalizedTextObject(UAVariable targetDescriptionVar, LocalizedText newDescription, string CP_Index)
    {
        try
        {
            string handleLocalizedText;
            if (newDescription.TextId == "")
            {
                handleLocalizedText = newDescription.Text;
            }
            else
            {
                handleLocalizedText = newDescription.TextId;
            }
            if (newDescription == null)
            {
                Log.Error($"Invalid LocalizedText for <b>CP_Index: {CP_Index}</b>");
                return;
            }

            if (targetDescriptionVar != null)
            {
                var currentValue = targetDescriptionVar.Value;
                targetDescriptionVar.SetValue(newDescription);

                string encodedText = WebUtility.HtmlEncode(handleLocalizedText); // Encodes only for log so we can see '&" symbol


                Log.Info($"<b>CPDescription{CP_Index}</b> has been updated to: <b>{encodedText}</b> at the path Model/Descriptions/CPDescriptions");
            }
            else
            {
                Log.Error($"The target Description is null for CP_Index: <b>{CP_Index}</b>. Verify CPDescription index exists.");
            }
        }
        catch (Exception ex)
        {
            Log.Error($"Error while updating the description paired with index {CP_Index}: {ex.Message}");
        }
    }

    private UAVariable FindCPDescriptionIndex(string CP_Index)
    {
        // string index = CP_Index.Value
        UAVariable foundCPDesc = new UAVariable();
        try
        {
            foundCPDesc = (UAVariable)Project.Current.GetVariable($"Model/Descriptions/CPDescriptions/CPDescription{CP_Index}");
        }
        catch (Exception ex)
        {
            Log.Error($"Trouble parsing CPDescription at CP {CP_Index}: {ex}");
            return null;
        }
        return foundCPDesc;

    }

    #endregion



    #region Info_Text moved to COMapImageInfo (Key Converter)

    //Updates the LocalizedText Value in the CoMapImageInfo key Converter
    private void UpdateLocalizedText(UAVariable infoKey_Index, LocalizedText Info_Text, string CP_Index)
    {
        try
        {
            string handleLocalizedText;

            if (Info_Text.TextId == "")
            {
                handleLocalizedText = Info_Text.Text;
            }
            else
            {
                handleLocalizedText = Info_Text.TextId;
            }

            infoKey_Index.Value = handleLocalizedText; // Set the value to CP_Description

            //string newText = infoKey_Index.Value?.ToString().Split(new string[] { "Text: " }, StringSplitOptions.None).Last().Split(',')[0].Trim();

            string encodedText = WebUtility.HtmlEncode(handleLocalizedText); // Encodes only for log so we can see '&" symbol
            Log.Info($" The <b>informational text</b> for ChangePoint {CP_Index} has been updated to: <b>{encodedText}</b> at the path Data/Converters/COMapImageInfo");
        }
        catch (Exception ex)
        {
            Log.Error($"Error while updating the text paired with index {CP_Index}: {ex.Message}");
        }
    }

    #endregion





    #region ImagePath Existance Validation and moves ImagePath to COMapImagePath (key Converter)

    private bool IsPathValid(string imagePathConvert)
    {
        if (string.IsNullOrWhiteSpace(imagePathConvert))
        {
            Log.Info("Image_Path is null or empty");
            return true;
        }

        string absolutePath = ResourceUri.FromProjectRelativePath(imagePathConvert)?.Uri.Replace("\\%PROJECTDIR%", "");
        bool fileExists = File.Exists(absolutePath);

        return fileExists;
    }

    private void UpdateResourceURI(UAVariable imageKey_Index, ResourceUri resourceURI, string CP_Index)
    {
        if (resourceURI == null)
        {
            Log.Error($" Invalid ResourceUri for CP_Index: <b>{CP_Index}</b>");
            return;
        }

        string resourceUriString = resourceURI.ProjectRelativePath.ToString();

        // Capture namespace -> ( ns = 96;) to populate image
        Match nsMatch = Regex.Match(resourceUriString, @"^ns=\d+;");
        string namespacePrefix = nsMatch.Success ? nsMatch.Value : "";

        // Remove namespace for validation purposes
        string strippedPath = Regex.Replace(resourceUriString, @"\s*\(String\)|^ns=\d+;", "");


        // Check if the path is valid before updating
        if (IsPathValid(strippedPath))
        {
            try
            {
                // Restore namespace before assignment
                string finalPath = namespacePrefix + strippedPath;

                imageKey_Index.SetValue(finalPath);

                Log.Info($"The <b>image path</b> for ChangePoint {CP_Index} has been updated to: <b>{strippedPath}</b> at the path Data/Converters/COMapImagePath");
            }
            catch (Exception ex)
            {
                Log.Error($" Exception while updating URI for CP_Index {CP_Index}: {ex.Message}");
            }
        }
        else
        {
            Log.Error($" Update aborted. The path {strippedPath} does not exist in the project directory");
        }
    }

    #endregion
}
