using BeauUtil;
using UnityEngine;
using UnityEngine.Video;

public class QuickVideoStreamLoader : MonoBehaviour {
    [HideInInspector] public VideoPlayer Video;
    
    [StreamingPath][SerializeField]
    public string AssetPath;

    private void OnEnable() {
        Video = GetComponent<VideoPlayer>();

        Video.gameObject.SetActive(true);
        Video.url = default;
        Video.url = Application.streamingAssetsPath + '/' + AssetPath;
        Video.Prepare();

        if (Video.url == null || Video.url.Length <= 0) {
            Debug.LogWarning("[PostcardVideoRenderer] Unable to load video on postcard: " + name);
            return;
        }else{
            Video.prepareCompleted += OnVideoPrepared;
        }
    }

    private void OnDisable() {
        Video.Stop();
        Video.gameObject.SetActive(false); 
    }

    private void OnVideoPrepared(VideoPlayer src) {
        src.frame = 0;
        src.Play();
    }

}
