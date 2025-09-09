using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;
using Scripts.Utility;

public class TimelineXMLUI : BasicSingleton<TimelineXMLUI>
{
    [Header("UI References")]
    public Button importButton;
    public Button exportButton;
    public Button clearButton;
    public TMP_InputField filePathInput;
    public TextMeshProUGUI statusText;

    [Header("Viewer Core")]
    public ViewerCore viewerCore;

    [Header("File Settings")]
    public string defaultFileName = "timeline_project.xml";
    public string defaultPath = "";

    //private string defaultTestPath = @"C:\Users\ege-0\OneDrive\Masaüstü\test.xml";

    void Start()
    {
        Debug.Log("TimelineXMLUI initialized on " + this.name);
        // Set default path to desktop if empty
        if (string.IsNullOrEmpty(defaultPath))
        {
            defaultPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        }

        SetupButtons();
        UpdateStatusText("Ready");

        //if (File.Exists(defaultTestPath))
        //{
        //    ImportXML(defaultTestPath);
        //}


    }

    void SetupButtons()
    {
        if (importButton != null)
        {
            importButton.onClick.AddListener(OnImportButtonClicked);
        }
    }

    public static void DisposeXML()
    {
        Instance.Dispose();
    }

    private void Dispose()
    {
        viewerCore.InternalDispose();
    }

    

    public void OnImportButtonClicked()
    {


        if (filePathInput != null && !string.IsNullOrEmpty(filePathInput.text))
        {
            ImportXML(filePathInput.text);
        }
        else
        {
            string defaultFile = Path.Combine(defaultPath, defaultFileName);
            if (File.Exists(defaultFile))
            {
                ImportXML(defaultFile);
            }
            else
            {
                UpdateStatusText("Please enter a valid file path", true);
            }
        }
    }


    void ImportXML(string path)
    {
        if (!File.Exists(path))
        {
            UpdateStatusText($"File not found: {path}", true);
            return;
        }

        UpdateStatusText($"Importing from {Path.GetFileName(path)}...");

        try
        {
            viewerCore.LoadProject(path);
            UpdateStatusText($"Successfully imported: {Path.GetFileName(path)}", false);

            if (filePathInput != null)
            {
                filePathInput.text = path;
            }

        }
        catch (System.Exception e)
        {
            UpdateStatusText($"Import failed: {e.Message}", true);
            Debug.LogError($"Import error: {e}");
        }
    }

    void UpdateStatusText(string message, bool isError = false)
    {
        if (statusText != null)
        {
            statusText.text = message;
            statusText.color = isError ? Color.red : Color.green;
        }

        Debug.Log($"[TimelineXML] {message}");

    }

}