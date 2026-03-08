using System;
using System.Collections.Generic;
using UnityEngine;

public class AssetManager : MonoBehaviour
{
    public static AssetManager Instance { get; private set; }
    private Dictionary<string, GameObject> _loadedAssets = new Dictionary<string, GameObject>();
    public event Action OnAssetsReady;
    public bool debugMode = false;
    public GameObject
        debugBonePrefab,
        debugTissuePrefab;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (debugMode)
        {
            RegisterAsset("Bone", debugBonePrefab);
            RegisterAsset("Tissue", debugTissuePrefab);
        }
    }

    public void RegisterAsset(string assetName, GameObject prefab)
    {
        if (!_loadedAssets.ContainsKey(assetName))
        {
            _loadedAssets.Add(assetName, prefab);
        }

        if (_loadedAssets.Count == 2)
        {
            OnAssetsReady?.Invoke();
        }
    }

    public GameObject GetAsset(string assetName)
    {
        _loadedAssets.TryGetValue(assetName, out GameObject asset);
        return asset;
    }

    public GameObject GetBoneAsset()
    {
        foreach (var kvp in _loadedAssets)
        {
            if (kvp.Key.ToLower().Contains("bone"))
            {
                return kvp.Value;
            }
        }
        return null;
    }

    public GameObject GetTissueAsset()
    {
        foreach (var kvp in _loadedAssets)
        {
            if (kvp.Key.ToLower().Contains("tissue"))
            {
                return kvp.Value;
            }
        }
        return null;
    }
}