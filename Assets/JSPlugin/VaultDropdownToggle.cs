using UnityEngine;
using UnityEngine.SceneManagement;
using System.Runtime.InteropServices;

/// <summary>
/// Note: This class is designed to be used with the `vault-floating-dropdown` jslib pluggin. 
/// This script will remove the dropdown elment from the DOM after the target scene has been unloaded.
/// </summary>
public class VaultDropdownToggle : MonoBehaviour {
    [Tooltip("The cutoff scene where the Vault Dropdown will be removed")]
    public string cutoffScene = "Title";

    [DllImport("__Internal")]
    private static extern void DisableVaultButton();

    private void OnEnable() {
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnSceneUnloaded(Scene current) {
        if(current.name != cutoffScene) return;
#if UNITY_WEBGL && !UNITY_EDITOR
        DisableVaultButton();
#endif
    }
    
    private void OnDisable() {
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
    }
}