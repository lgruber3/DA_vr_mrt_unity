using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class BlobFetcher : MonoBehaviour
{
    [SerializeField] private string sasToken = "?sv=2022-11-02&ss=bfqt&srt=sco...";
    [SerializeField] private string baseUrl = "https://myaccount.blob.core.windows.net/models/";
    [SerializeField] private string model1Name = "bone_object";
    [SerializeField] private string model2Name = "tissue_object";
    [SerializeField] private GameObject scanObjectBoneVisuals;
    [SerializeField] private GameObject scanObjectTissueVisuals;

    void Start()
    {
        StartCoroutine(LoadAllAssets());
    }

   IEnumerator LoadAllAssets()
    {
        IEnumerator download1 = DownloadAndInstantiate(baseUrl + model1Name + sasToken);
        IEnumerator download2 = DownloadAndInstantiate(baseUrl + model2Name + sasToken);

        yield return StartCoroutine(download1);
        yield return StartCoroutine(download2);
    }

    IEnumerator DownloadAndInstantiate(string fullUrl)
    {
        using (UnityWebRequest uwr = UnityWebRequestAssetBundle.GetAssetBundle(fullUrl))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Failed to download: {uwr.error}");
            }
            else
            {
                AssetBundle bundle = DownloadHandlerAssetBundle.GetContent(uwr);
                GameObject prefab = bundle.LoadAsset<GameObject>(bundle.GetAllAssetNames()[0]);
                
                if (prefab.name.Contains("bone"))
                {
                    Instantiate(prefab, scanObjectBoneVisuals.transform);
                }
                else if (prefab.name.Contains("tissue"))
                {
                    Instantiate(prefab, scanObjectTissueVisuals.transform);
                }

                AssetManager.Instance.RegisterAsset(prefab.name, prefab);

                bundle.Unload(false);
            }
        }
    }
}