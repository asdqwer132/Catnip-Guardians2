using UnityEngine;

public class MovePingController : MonoBehaviour
{
    [Header("Ping Object")]
    public GameObject pingObject;

    [Header("Option")]
    public bool matchPlayerZ = true;
    public bool restartAnimatorOnPing = true;

    private Animator pingAnimator;

    private void Awake()
    {
        if (pingObject != null)
            pingAnimator = pingObject.GetComponent<Animator>();

        HidePing();
    }

    public void ShowPing(Vector3 position)
    {
        if (pingObject == null)
            return;

        if (matchPlayerZ)
            position.z = transform.position.z;

        pingObject.transform.position = position;

        if (!pingObject.activeSelf)
            pingObject.SetActive(true);

        if (restartAnimatorOnPing)
            RestartAnimator();
    }

    public void HidePing()
    {
        if (pingObject == null)
            return;

        pingObject.SetActive(false);
    }

    private void RestartAnimator()
    {
        if (pingAnimator == null)
            return;

        pingAnimator.Rebind();
        pingAnimator.Update(0f);
    }
}