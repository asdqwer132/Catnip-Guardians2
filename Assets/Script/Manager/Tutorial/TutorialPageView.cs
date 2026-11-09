using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Both authored panels reuse this presenter. It owns no tutorial progress.
public sealed class TutorialPageView : MonoBehaviour
{
    public DataInfoUI tutorialInfo;
    public DataInfoUI pageInfo;
    public TMP_Text pageCounter;
    public Image spriteImage;
    public RawImage videoImage;
    public VideoPlayer videoPlayer;
    public Button previousButton;
    public Button nextButton;
    public Button closeButton;

    private TutorialData currentData;
    private int currentPage = -1;
    private uint mediaRevision;
    private VideoPlayer.FrameReadyEventHandler frameReady;
    private VideoPlayer.ErrorEventHandler videoError;
    public TutorialData CurrentData => currentData;
    public int CurrentPage => currentPage;

    public void Show(TutorialData data, int index)
    {
        if (data == null || !data.HasPages || index < 0 || index >= data.pages.Length) { Clear(); return; }
        if (currentData == data && currentPage == index) return;
        Clear();
        currentData = data;
        currentPage = index;
        var page = data.pages[index];
        if (tutorialInfo != null) tutorialInfo.SetData(data);
        if (pageInfo != null) pageInfo.SetDefinedData(page);
        if (pageCounter != null) pageCounter.text = $"{index + 1} / {data.pages.Length}";
        if (page.mediaType == TutorialMediaType.Sprite)
        {
            if (spriteImage != null)
            {
                spriteImage.sprite = page.sprite;
                spriteImage.gameObject.SetActive(page.sprite != null);
            }
            return;
        }
        if (page.video == null || videoPlayer == null || videoImage == null) return;
        videoImage.gameObject.SetActive(true);
        videoImage.enabled = false; // Hide stale frames from the previously displayed clip.
        videoPlayer.gameObject.SetActive(true);
        videoPlayer.playOnAwake = false;
        videoPlayer.source = VideoSource.VideoClip;
        videoPlayer.clip = page.video;
        videoPlayer.isLooping = page.loopVideo;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.sendFrameReadyEvents = true;
        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        // APIOnly uses this player's texture, so two panels do not share a RenderTexture.
        videoPlayer.renderMode = VideoRenderMode.APIOnly;
        videoPlayer.targetTexture = null;
        uint revision = mediaRevision;
        frameReady = (player, frame) =>
        {
            if (revision != mediaRevision || !isActiveAndEnabled) return;
            videoImage.texture = player.texture;
            videoImage.enabled = player.texture != null;
        };
        videoError = (player, message) =>
        {
            if (revision != mediaRevision) return;
            videoImage.enabled = false;
            Debug.LogWarning($"Tutorial video '{data.name}': {message}", this);
        };
        videoPlayer.frameReady += frameReady;
        videoPlayer.errorReceived += videoError;
        videoPlayer.Play();
    }

    public void Clear()
    {
        mediaRevision++;
        if (videoPlayer != null)
        {
            if (frameReady != null) videoPlayer.frameReady -= frameReady;
            if (videoError != null) videoPlayer.errorReceived -= videoError;
            videoPlayer.Stop();
            videoPlayer.clip = null;
        }
        frameReady = null;
        videoError = null;
        if (videoImage != null) { videoImage.texture = null; videoImage.enabled = false; videoImage.gameObject.SetActive(false); }
        if (spriteImage != null) { spriteImage.sprite = null; spriteImage.gameObject.SetActive(false); }
        if (tutorialInfo != null) tutorialInfo.ClearData();
        if (pageInfo != null) pageInfo.ClearData();
        if (pageCounter != null) pageCounter.text = "";
        currentData = null;
        currentPage = -1;
    }
    private void OnDisable() => Clear();
}
