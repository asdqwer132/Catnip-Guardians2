using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(Image))]
public class ShopBoxOpenAnimationUI : MonoBehaviour
{
    [Header("Target")]
    public Image boxImage;
    public Animator boxAnimator;

    [Header("Base Animator")]
    [Tooltip("Idle/Open 상태와 Open Trigger가 들어있는 공통 컨트롤러")]
    public RuntimeAnimatorController baseController;

    [Header("Base Clip Names")]
    [Tooltip("공통 컨트롤러 안의 기본 Idle 클립 이름")]
    public string baseIdleClipName = "Idle";

    [Tooltip("공통 컨트롤러 안의 기본 Open 클립 이름")]
    public string baseOpenClipName = "Open";

    [Header("State / Trigger")]
    public string idleStateName = "Idle";
    public string openStateName = "Open";

    [Header("Fallback")]
    public bool useSpriteWhenAnimationMissing = true;

    [Header("Debug")]
    public bool debugOverrideClip = true;
    public bool debugLogAllOverridePairs = false;
    public bool debugCheckImageSpriteBinding = true;

    private ItemBoxData currentBoxData;
    private AnimatorOverrideController overrideController;

    private void Reset()
    {
        boxImage = GetComponent<Image>();
        boxAnimator = GetComponent<Animator>();
    }

    private void Awake()
    {
        if (boxImage == null)
            boxImage = GetComponent<Image>();

        if (boxAnimator == null)
            boxAnimator = GetComponent<Animator>();
    }

    public void SetBox(ItemBoxData boxData)
    {
        currentBoxData = boxData;

        if (boxData == null)
        {
            Clear();
            return;
        }

        if (boxImage != null)
            boxImage.enabled = true;

        bool canUseAnimation =
            baseController != null &&
            boxAnimator != null &&
            boxData.idleClip != null &&
            boxData.openClip != null;

        if (canUseAnimation)
        {
            ApplyOverrideController(boxData);

            boxAnimator.enabled = true;
            boxAnimator.Rebind();
            boxAnimator.Update(0f);

            PlayIdle();
            return;
        }

        if (debugOverrideClip)
            DebugAnimationMissingReason(boxData);

        if (boxAnimator != null)
            boxAnimator.enabled = false;

        if (useSpriteWhenAnimationMissing && boxImage != null)
        {
            boxImage.sprite = boxData.icon;
            boxImage.enabled = boxData.icon != null;
        }
    }
    public void PlayOpen()
    {
        if (currentBoxData == null)
        {
            Debug.LogWarning($"{name} 현재 상자 데이터가 없습니다.", this);
            return;
        }

        if (boxAnimator == null)
        {
            Debug.LogWarning($"{name} Animator가 없습니다.", this);
            return;
        }

        if (boxAnimator.runtimeAnimatorController == null)
        {
            Debug.LogWarning($"{name} Animator Runtime Controller가 없습니다.", this);
            return;
        }

        boxAnimator.enabled = true;

        bool played = false;

        // 핵심:
        // 이미 Open 상태에 있어도 Open State를 0초부터 다시 강제 재생한다.
        if (!string.IsNullOrEmpty(openStateName))
        {
            int openStateHash = Animator.StringToHash(openStateName);

            if (boxAnimator.HasState(0, openStateHash))
            {
                boxAnimator.Play(openStateHash, 0, 0f);
                boxAnimator.Update(0f);
                played = true;
            }
        }

        // Open State 직접 재생이 실패했을 때만 Trigger 사용
        if (!played &&
            !string.IsNullOrEmpty(currentBoxData.openTriggerName) &&
            HasAnimatorParameter(currentBoxData.openTriggerName, AnimatorControllerParameterType.Trigger))
        {
            boxAnimator.ResetTrigger(currentBoxData.openTriggerName);
            boxAnimator.SetTrigger(currentBoxData.openTriggerName);
            boxAnimator.Update(0f);
            played = true;
        }

        if (!played)
            Debug.LogWarning($"{name} 상자 열기 애니메이션을 재생하지 못했습니다.", this);
    }
    public void PlayIdle()
    {
        if (boxAnimator == null)
            return;

        if (boxAnimator.runtimeAnimatorController == null)
            return;

        if (string.IsNullOrEmpty(idleStateName))
            return;

        int stateHash = Animator.StringToHash(idleStateName);

        if (boxAnimator.HasState(0, stateHash))
            boxAnimator.Play(stateHash, 0, 0f);
        else if (debugOverrideClip)
            Debug.LogWarning($"{name} Animator 안에서 Idle State '{idleStateName}'를 찾지 못했습니다.", this);
    }

    private void ApplyOverrideController(ItemBoxData boxData)
    {
        overrideController = new AnimatorOverrideController(baseController);

        List<KeyValuePair<AnimationClip, AnimationClip>> overrides =
            new List<KeyValuePair<AnimationClip, AnimationClip>>();

        overrideController.GetOverrides(overrides);

        bool idleBaseFound = false;
        bool openBaseFound = false;
        bool idleClipApplied = false;
        bool openClipApplied = false;

        for (int i = 0; i < overrides.Count; i++)
        {
            AnimationClip originalClip = overrides[i].Key;
            AnimationClip newClip = overrides[i].Value;

            if (originalClip == null)
                continue;

            if (originalClip.name == baseIdleClipName)
            {
                idleBaseFound = true;

                if (boxData.idleClip != null)
                {
                    newClip = boxData.idleClip;
                    idleClipApplied = true;
                }
            }

            if (originalClip.name == baseOpenClipName)
            {
                openBaseFound = true;

                if (boxData.openClip != null)
                {
                    newClip = boxData.openClip;
                    openClipApplied = true;
                }
            }

            overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(originalClip, newClip);
        }

        overrideController.ApplyOverrides(overrides);
        boxAnimator.runtimeAnimatorController = overrideController;

        if (debugOverrideClip)
        {
            DebugOverrideResult(
                boxData,
                overrides,
                idleBaseFound,
                openBaseFound,
                idleClipApplied,
                openClipApplied
            );
        }
    }

    public void Clear()
    {
        currentBoxData = null;
        overrideController = null;

        if (boxAnimator != null)
        {
            boxAnimator.runtimeAnimatorController = null;
            boxAnimator.enabled = false;
        }

        if (boxImage != null)
        {
            boxImage.sprite = null;
            boxImage.enabled = false;
        }
    }

    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType type)
    {
        if (boxAnimator == null)
            return false;

        if (string.IsNullOrEmpty(parameterName))
            return false;

        AnimatorControllerParameter[] parameters = boxAnimator.parameters;

        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == parameterName && parameters[i].type == type)
                return true;
        }

        return false;
    }

    private void DebugAnimationMissingReason(ItemBoxData boxData)
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"[{name}] 상자 애니메이션 사용 불가");
        sb.AppendLine($"BoxData: {(boxData != null ? boxData.name : "NULL")}");
        sb.AppendLine($"Base Controller: {(baseController != null ? baseController.name : "NULL")}");
        sb.AppendLine($"Animator: {(boxAnimator != null ? boxAnimator.name : "NULL")}");
        sb.AppendLine($"Idle Clip: {(boxData != null && boxData.idleClip != null ? boxData.idleClip.name : "NULL")}");
        sb.AppendLine($"Open Clip: {(boxData != null && boxData.openClip != null ? boxData.openClip.name : "NULL")}");

        if (baseController == null)
            sb.AppendLine("문제: baseController가 비어 있음");

        if (boxAnimator == null)
            sb.AppendLine("문제: boxAnimator가 비어 있음");

        if (boxData != null && boxData.idleClip == null)
            sb.AppendLine("문제: ItemBoxData.idleClip이 비어 있음");

        if (boxData != null && boxData.openClip == null)
            sb.AppendLine("문제: ItemBoxData.openClip이 비어 있음");

        sb.AppendLine("결과: 애니메이션 대신 boxData.icon 스프라이트를 표시합니다.");

        Debug.LogWarning(sb.ToString(), this);
    }

    private void DebugOverrideResult(
        ItemBoxData boxData,
        List<KeyValuePair<AnimationClip, AnimationClip>> overrides,
        bool idleBaseFound,
        bool openBaseFound,
        bool idleClipApplied,
        bool openClipApplied
    )
    {
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"[{name}] 상자 애니메이션 Override 확인");
        sb.AppendLine($"BoxData: {(boxData != null ? boxData.name : "NULL")}");
        sb.AppendLine($"Base Controller: {(baseController != null ? baseController.name : "NULL")}");
        sb.AppendLine($"Animator Runtime Controller: {(boxAnimator != null && boxAnimator.runtimeAnimatorController != null ? boxAnimator.runtimeAnimatorController.name : "NULL")}");
        sb.AppendLine("");

        sb.AppendLine($"Base Idle Clip Name: {baseIdleClipName}");
        sb.AppendLine($"Box Idle Clip: {(boxData != null && boxData.idleClip != null ? boxData.idleClip.name : "NULL")}");
        sb.AppendLine($"Idle Base Found: {idleBaseFound}");
        sb.AppendLine($"Idle Applied: {idleClipApplied}");
        sb.AppendLine("");

        sb.AppendLine($"Base Open Clip Name: {baseOpenClipName}");
        sb.AppendLine($"Box Open Clip: {(boxData != null && boxData.openClip != null ? boxData.openClip.name : "NULL")}");
        sb.AppendLine($"Open Base Found: {openBaseFound}");
        sb.AppendLine($"Open Applied: {openClipApplied}");
        sb.AppendLine("");

        bool idleOk = idleBaseFound && idleClipApplied;
        bool openOk = openBaseFound && openClipApplied;

        if (idleOk && openOk)
            sb.AppendLine("결과: Idle / Open 클립 둘 다 정상 적용됨");
        else
            sb.AppendLine("결과: 클립 적용 문제 있음");

        if (!idleBaseFound)
            sb.AppendLine($"문제: 공통 컨트롤러 안에서 '{baseIdleClipName}' 이름의 기본 클립을 못 찾음");

        if (!openBaseFound)
            sb.AppendLine($"문제: 공통 컨트롤러 안에서 '{baseOpenClipName}' 이름의 기본 클립을 못 찾음");

        if (boxData != null && boxData.idleClip == null)
            sb.AppendLine("문제: ItemBoxData.idleClip이 비어 있음");

        if (boxData != null && boxData.openClip == null)
            sb.AppendLine("문제: ItemBoxData.openClip이 비어 있음");

        if (boxData != null && !HasAnimatorParameter(boxData.openTriggerName, AnimatorControllerParameterType.Trigger))
            sb.AppendLine($"주의: Trigger '{boxData.openTriggerName}'를 Animator에서 못 찾음. Trigger 대신 State 직접 재생으로 넘어갈 수 있음.");

        if (!string.IsNullOrEmpty(idleStateName))
        {
            int idleStateHash = Animator.StringToHash(idleStateName);
            bool hasIdleState = boxAnimator != null && boxAnimator.HasState(0, idleStateHash);
            sb.AppendLine($"Idle State '{idleStateName}' Found: {hasIdleState}");
        }

        if (!string.IsNullOrEmpty(openStateName))
        {
            int openStateHash = Animator.StringToHash(openStateName);
            bool hasOpenState = boxAnimator != null && boxAnimator.HasState(0, openStateHash);
            sb.AppendLine($"Open State '{openStateName}' Found: {hasOpenState}");
        }

        if (debugLogAllOverridePairs)
        {
            sb.AppendLine("");
            sb.AppendLine("전체 Override 목록:");

            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip original = overrides[i].Key;
                AnimationClip replaced = overrides[i].Value;

                sb.AppendLine(
                    $"- Original: {(original != null ? original.name : "NULL")} -> Override: {(replaced != null ? replaced.name : "NULL")}"
                );
            }
        }

#if UNITY_EDITOR
        if (debugCheckImageSpriteBinding)
        {
            sb.AppendLine("");
            sb.AppendLine("클립 바인딩 확인:");

            if (boxData != null)
            {
                AppendClipBindingCheck(sb, "Idle Clip", boxData.idleClip);
                AppendClipBindingCheck(sb, "Open Clip", boxData.openClip);
            }
        }
#endif

        if (idleOk && openOk)
            Debug.Log(sb.ToString(), this);
        else
            Debug.LogWarning(sb.ToString(), this);
    }

#if UNITY_EDITOR
    private void AppendClipBindingCheck(StringBuilder sb, string label, AnimationClip clip)
    {
        if (clip == null)
        {
            sb.AppendLine($"{label}: NULL");
            return;
        }

        EditorCurveBinding[] bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

        bool hasImageSpriteBinding = false;
        int spriteKeyCount = 0;

        for (int i = 0; i < bindings.Length; i++)
        {
            EditorCurveBinding binding = bindings[i];

            if (binding.type == typeof(Image) && binding.propertyName == "m_Sprite")
            {
                hasImageSpriteBinding = true;

                ObjectReferenceKeyframe[] keys =
                    AnimationUtility.GetObjectReferenceCurve(clip, binding);

                spriteKeyCount = keys != null ? keys.Length : 0;
                break;
            }
        }

        sb.AppendLine($"{label}: {clip.name}");
        sb.AppendLine($"- Image.m_Sprite Binding: {hasImageSpriteBinding}");
        sb.AppendLine($"- Sprite Key Count: {spriteKeyCount}");

        if (!hasImageSpriteBinding)
            sb.AppendLine($"- 문제: {clip.name} 클립이 UI Image Sprite를 바꾸는 클립이 아님");
    }
#endif
}