using UnityEngine;
using UnityEngine.UI;
using System.IO;
using TMPro;

public class TimelineXMLUI : MonoBehaviour
{
    [Header("UI References")]
    public Button importButton;
    public Button exportButton;
    public Button clearButton;
    public TMP_InputField filePathInput;
    public TextMeshProUGUI statusText;

    [Header("Timeline")]
    public TimelineXMLSerializer xmlSerializer;

    [Header("File Settings")]
    public string defaultFileName = "timeline_project.xml";
    public string defaultPath = "";

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
    }

    void SetupButtons()
    {
        if (importButton != null)
        {
            importButton.onClick.AddListener(OnImportButtonClicked);
        }
    }

    void OnImportButtonClicked()
    {


        if (filePathInput != null && !string.IsNullOrEmpty(filePathInput.text))
        {
            ImportXML(filePathInput.text);
        }
        else
        {
            // Varsayılan dosyayı dene
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
            xmlSerializer.ImportFromXML(path);
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