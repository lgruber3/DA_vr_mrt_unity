using System;
using System.Text;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : MonoBehaviour
{
    public static ApiClient Instance { get; private set; }

    [SerializeField]
    private string baseUrl = "http://localhost:5022/";

    [SerializeField]
    private float timeoutSeconds = 10f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }


    public void Get<T>(string endpoint, Action<T> onSuccess, Action<string> onError)
    {
        StartCoroutine(Request<T>("GET", endpoint, null, onSuccess, onError));
    }

    public void Post<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError)
    {
        StartCoroutine(Request<T>("POST", endpoint, body, onSuccess, onError));
    }

    public void Put<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError)
    {
        StartCoroutine(Request<T>("PUT", endpoint, body, onSuccess, onError));
    }

    public void Patch<T>(string endpoint, object body, Action<T> onSuccess, Action<string> onError)
    {
        StartCoroutine(Request<T>("PATCH", endpoint, body, onSuccess, onError));
    }

    public void Delete<T>(string endpoint, Action<T> onSuccess, Action<string> onError)
    {
        StartCoroutine(Request<T>("DELETE", endpoint, null, onSuccess, onError));
    }

    private IEnumerator Request<T>(
        string method,
        string endpoint,
        object body,
        Action<T> onSuccess,
        Action<string> onError)
    {
        string url = $"{baseUrl}{endpoint}";
        Debug.Log($"API Request: {method} {url}");
        UnityWebRequest request;

        if (method == "GET" || method == "DELETE")
        {
            request = new UnityWebRequest(url, method);
        }
        else
        {
            string json = body != null ? JsonUtility.ToJson(body) : "";
            byte[] data = Encoding.UTF8.GetBytes(json);

            request = new UnityWebRequest(url, method);
            request.uploadHandler = new UploadHandlerRaw(data);
        }

        request.downloadHandler = new DownloadHandlerBuffer();
        request.timeout = Mathf.RoundToInt(timeoutSeconds);

        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
        request.SetRequestHeader("Accept", "application/json");

        string token = UserDataManager.Instance.bearerToken;

        if (!string.IsNullOrEmpty(token))
            request.SetRequestHeader("Authorization", $"Bearer {token}");

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            onError?.Invoke(request.error);
            yield break;
        }

        try
        {
            string json = request.downloadHandler.text;

            if (typeof(T) == typeof(string))
            {
                onSuccess?.Invoke((T)(object)json);
            }
            else if (typeof(T).IsArray)
            {
                string wrappedJson = "{\"items\":" + json + "}";
                
                T result = JsonHelper.FromJsonArray<T>(json);
                onSuccess?.Invoke(result);
            }
            else
            {
                Debug.Log($"Raw JSON response: {json}");
                T result = JsonUtility.FromJson<T>(json);
                onSuccess?.Invoke(result);
            }
        }
        catch (Exception e)
        {
            onError?.Invoke($"Parse error: {e.Message}");
        }
    }

    [Serializable]
    public class JsonArrayWrapper<T>
    {
        public T[] items;
    }

    public static class JsonHelper
    {
        public static T FromJsonArray<T>(string json)
        {
            string wrappedJson = "{\"items\":" + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(wrappedJson);
            return wrapper.items;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T items;
        }
    }
}
