using UnityEngine;

public class MergeGimicManager : MonoBehaviour
{
    [SerializeField] MergeBoxGimic[] mergeBoxGimics;

    private MergeBoxGimic currentBox;

    public void SetBox(int index) => currentBox = mergeBoxGimics[index];
}
