using UnityEngine;

[CreateAssetMenu(fileName = "NewConfig", menuName = "VisioMED/Config")]
public class AppConfig : ScriptableObject {
    public string apiUrl;
    public bool showDebugConsole;
}
mpty sein wenn arzt noch nicht beigetreten ist