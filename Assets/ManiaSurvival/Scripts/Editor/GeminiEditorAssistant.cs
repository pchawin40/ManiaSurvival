using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Threading.Tasks;
using System.Text;
using System.IO;

public class GeminiEditorAssistant : EditorWindow
{
    private string apiKey = "";
    private string userPrompt = "Explain what this script does and add comments.";
    private string responseText = "";
    private Vector2 scrollPosition;
    private bool isWaiting = false;
    private bool includeSelectedFile = true;

    // Up-to-date Gemini 3.x and 2.x models
    private string[] availableModels = new string[] 
    { 
        "gemini-3.1-pro-preview",
        "gemini-3.5-flash", 
        "gemini-3.1-pro", 
        "gemini-3.1-flash-lite",
        "gemini-2.5-pro",
        "gemini-2.5-flash"
    };
    private int selectedModelIndex = 0;

    [MenuItem("Mania Survival/Gemini Assistant V2")]
    public static void ShowWindow()
    {
        GetWindow<GeminiEditorAssistant>("Gemini AI");
    }

    private void OnGUI()
    {
        GUILayout.Label("Gemini AI API Configuration", EditorStyles.boldLabel);

        GUILayout.Label("API Key:", EditorStyles.label);
        apiKey = EditorGUILayout.TextField(apiKey);

        GUILayout.Space(5);

        GUILayout.Label("Model:", EditorStyles.label);
        selectedModelIndex = EditorGUILayout.Popup(selectedModelIndex, availableModels);

        GUILayout.Space(15);
        
        // --- V2 Context Selection Section ---
        GUILayout.Label("Context & Prompt", EditorStyles.boldLabel);
        
        includeSelectedFile = EditorGUILayout.Toggle("Include Selected File", includeSelectedFile);
        
        // Dynamically display what the user has clicked on in the Project window
        string selectedFileText = "None";
        bool canIncludeFile = false;
        
        if (Selection.activeObject != null)
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path))
            {
                if (path.EndsWith(".cs") || path.EndsWith(".txt") || path.EndsWith(".md") || path.EndsWith(".json"))
                {
                    selectedFileText = Path.GetFileName(path);
                    canIncludeFile = true;
                }
                else
                {
                    selectedFileText = Path.GetFileName(path) + " (Unsupported format)";
                }
            }
        }

        // Color code the selected file text so you know if it's working
        GUI.contentColor = canIncludeFile ? new Color(0.2f, 0.8f, 0.2f) : Color.gray;
        GUILayout.Label("Selected File: " + selectedFileText, EditorStyles.wordWrappedLabel);
        GUI.contentColor = Color.white; // Reset color to default
        
        GUILayout.Space(5);

        GUILayout.Label("Instructions:", EditorStyles.label);
        userPrompt = EditorGUILayout.TextArea(userPrompt, GUILayout.Height(80));

        GUILayout.Space(15);

        // --- Action Button ---
        EditorGUI.BeginDisabledGroup(isWaiting || string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(userPrompt));
        if (GUILayout.Button("Send to Gemini", GUILayout.Height(35)))
        {
            SendRequest(canIncludeFile);
        }
        EditorGUI.EndDisabledGroup();

        GUILayout.Space(15);

        // --- Response Section ---
        GUILayout.Label("Response:", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, EditorStyles.helpBox, GUILayout.ExpandHeight(true));
        
        GUIStyle wordWrapStyle = new GUIStyle(EditorStyles.textArea);
        wordWrapStyle.wordWrap = true;
        wordWrapStyle.richText = true;
        responseText = EditorGUILayout.TextArea(responseText, wordWrapStyle, GUILayout.ExpandHeight(true));
        
        EditorGUILayout.EndScrollView();
    }

    private async void SendRequest(bool hasValidFile)
    {
        isWaiting = true;
        responseText = "Thinking... (Reading context and querying API)";
        Repaint(); 

        string selectedModel = availableModels[selectedModelIndex];
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{selectedModel}:generateContent?key={apiKey}";

        // Combine the user's prompt with the file context
        string finalPrompt = userPrompt;
        
        if (includeSelectedFile && hasValidFile && Selection.activeObject != null)
        {
            string path = AssetDatabase.GetAssetPath(Selection.activeObject);
            try
            {
                string fileContent = File.ReadAllText(path);
                finalPrompt += $"\n\n--- Context File: {Path.GetFileName(path)} ---\n{fileContent}";
            }
            catch (System.Exception e)
            {
                Debug.LogError("Gemini Tool couldn't read the file: " + e.Message);
            }
        }

        // Use Unity's JsonUtility to perfectly escape C# code and syntax
        GeminiRequestData requestData = new GeminiRequestData
        {
            contents = new GeminiContentData[]
            {
                new GeminiContentData
                {
                    parts = new GeminiPartData[]
                    {
                        new GeminiPartData { text = finalPrompt }
                    }
                }
            }
        };

        string jsonBody = JsonUtility.ToJson(requestData);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            var operation = request.SendWebRequest();

            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (request.result == UnityWebRequest.Result.Success)
            {
                responseText = ExtractTextFromJson(request.downloadHandler.text);
            }
            else
            {
                responseText = "Error connecting to Gemini:\n" + request.error + "\n\n" + request.downloadHandler.text;
            }
        }

        isWaiting = false;
        Repaint();
    }

    private string ExtractTextFromJson(string jsonResponse)
    {
        try
        {
            GeminiResponseData response = JsonUtility.FromJson<GeminiResponseData>(jsonResponse);
            if (response != null && response.candidates != null && response.candidates.Length > 0)
            {
                return response.candidates[0].content.parts[0].text;
            }
            return "Could not parse response. Raw JSON:\n" + jsonResponse;
        }
        catch (System.Exception e)
        {
            return "Parsing error: " + e.Message + "\nRaw JSON:\n" + jsonResponse;
        }
    }

    // --- JSON Serialization Classes ---
    
    // Classes for sending data
    [System.Serializable]
    private class GeminiRequestData { public GeminiContentData[] contents; }
    [System.Serializable]
    private class GeminiContentData { public GeminiPartData[] parts; }
    [System.Serializable]
    private class GeminiPartData { public string text; }

    // Classes for receiving data
    [System.Serializable]
    private class GeminiResponseData { public Candidate[] candidates; }
    [System.Serializable]
    private class Candidate { public Content content; }
    [System.Serializable]
    private class Content { public Part[] parts; }
    [System.Serializable]
    private class Part { public string text; }
}