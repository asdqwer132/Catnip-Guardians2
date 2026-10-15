using System;
using System.Collections.Generic;
using UnityEngine;
namespace Unity.VisualScripting { }
public enum language { korean, English }
public class LanguageManager { public static LanguageManager instance; public language selectedLan; }
public class EquipmentBag : MonoBehaviour { public BagData bagData; public int currentSlotCount; public List<InventoryItem> equippedItems = new List<InventoryItem>(); public float GetCurrentWeight()=>0; public float GetMaxWeight()=>1; public void RefreshUI(){} }
public class GameManager { public static GameManager instance; public bool isGameEnd; public void Victory(){} public void GameOver(){} }
public class GameSession { public static GameSession instance, Instance; public bool HasEquipmentBagData, HasSkillBuffData; public void LoadSkillBuff(BuffSkillManager x){} public void LoadEquipmentBags(object x){} public void SaveEquipmentBags(object x){} public void SaveSkillBuff(object x){} }
public class GameItemBagManager : MonoBehaviour { public static GameItemBagManager instance; public List<BagItemUseManager> bagItemUseManagers; public void Init(){} public EquipmentBag GetBagData(string id)=>null; }
public class GameStatisticsManager { public static GameStatisticsManager instance; public static GameStatisticsManager Instance; public void AddCurrency(CurrencyType t,int amount){} public void RecordItemUse(ItemData d){} public void ResetRound(){} }
public class TutorialEventManager { public static TutorialEventManager instance; public void OnItemUsed(ItemData d){} }
public class SettingManager { public static SettingManager instance; public GameSettingData setting; public IndicatorSpriteSize GetIndicatorSpriteSize()=>IndicatorSpriteSize.Medium; }
public class GameSettingData { public bool showTargetRange; public bool showDamagePopup; }
public class AudioManager { public static AudioManager instance; public void PlaySfx(string s){} public void PlaySfx(string c,string s){} public void PlayItemAudio(string s){} }
public static class UnlockCheckUtility { public static bool CanUse(IUnlockable x)=>true; }

public class ImageFillUI : MonoBehaviour { public void SetFill01(float v){} public void SetFill(float a,float b){} public void SetVisible(bool v){} }
public class AudioClipNameWithCategory { public string categoryName; public string clipName; }

public class OffscreenTargetIndicatorManager { public static OffscreenTargetIndicatorManager Instance; public void ShowIndicator(GameObject x){} public void ClearAll(){} }
