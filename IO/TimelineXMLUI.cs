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
    public TimelineGrid timelineGrid;

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

        if (exportButton != null)
        {
            exportButton.onClick.AddListener(OnExportButtonClicked);
        }

        if (clearButton != null)
        {
            clearButton.onClick.AddListener(OnClearButtonClicked);
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

    void OnExportButtonClicked()
    {


        string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"timeline_{timestamp}.xml";
        string filePath = Path.Combine(defaultPath, fileName);

        // Eğer input field'da path varsa onu kullan
        if (filePathInput != null && !string.IsNullOrEmpty(filePathInput.text))
        {
            string inputPath = filePathInput.text;
            if (!inputPath.EndsWith(".xml"))
            {
                inputPath += ".xml";
            }
            filePath = inputPath;
        }

        ExportXML(filePath);
    }

    void OnClearButtonClicked()
    {
        // Confirmation dialog gösterilebilir
        if (UnityEngine.EventSystems.EventSystem.current.currentSelectedGameObject == clearButton.gameObject)
        {
            ClearTimeline();
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

    void ExportXML(string path)
    {
        UpdateStatusText($"Exporting to {Path.GetFileName(path)}...");

        try
        {
            xmlSerializer.ExportToXML(path);
            UpdateStatusText($"Successfully exported: {Path.GetFileName(path)}", false);

            if (filePathInput != null)
            {
                filePathInput.text = path;
            }
        }
        catch (System.Exception e)
        {
            UpdateStatusText($"Export failed: {e.Message}", true);
            Debug.LogError($"Export error: {e}");
        }
    }

    void ClearTimeline()
    {
        UpdateStatusText("Clearing timeline...");

        if (timelineGrid != null)
        {
            timelineGrid.ClearAll();
        }
        else
        {
            Debug.LogError("TimelineGrid referansı atanmamış!");
        }

        UpdateStatusText("Timeline cleared", false);
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