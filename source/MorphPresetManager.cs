using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using MacGruber;

namespace ThatsLewd
{
  public class MorphPresetManager : MVRScript
  {
    class MorphPreset
    {
      public string Name;
      public Dictionary<string, float> Morphs;
    }

    Atom person = null;
    DAZCharacterSelector geometry = null;

    List<MorphPreset> morphPresets = new List<MorphPreset>();

    bool playing = false;
    float transitionTimer = 0f;
    string previousFrameTargetState;
    Dictionary<string, float> previousMorphs;
    Dictionary<string, float> targetMorphs;

    bool isUIOpenLastFrame = false;

    JSONStorableBool isEnabledStorable;
    JSONStorableFloat transitionTimeStorable;
    JSONStorableStringChooser transitionEasingStorable;
    JSONStorableStringChooser targetStateStorable;
    JSONStorableStringChooser presetToEditStorable;
    JSONStorableString presetNameStorable;

    JSONStorableStringChooser selectMorphStorable;
    HashSet<string> selectedMorphs = new HashSet<string>();
    List<object> dynamicUIObjects = new List<object>();

    public override void Init()
    {
      if (containingAtom == null || containingAtom.GetStorableByID("geometry") == null)
      {
        SuperController.LogError("MorphPresetManager must be attached to a Person atom!");
        return;
      }
      person = containingAtom;
      geometry = person.GetStorableByID("geometry") as DAZCharacterSelector;

      CreateUI();
    }

    void OnDestroy()
    {
      Utils.OnDestroyUI();
    }

    void CreateUI()
    {
      Utils.OnInitUI(CreateUIElement);

      // HIDDEN CONTROLS
      targetStateStorable = new JSONStorableStringChooser("Set State", new List<string>(), "", "Set State");
      RegisterStringChooser(targetStateStorable);

      // TRANSITION CONTROLS
      isEnabledStorable = Utils.SetupToggle(this, "Enabled", true, false);
      transitionTimeStorable = Utils.SetupSliderFloatWithRange(this, "Transition Time", 0.5f, 0f, 1f, false);
      transitionEasingStorable = Utils.SetupStringChooser(this, "Transition Easing", Easings.GetEasingOptions(), 0, false);
      Utils.SetupSpacer(this, 25, false);

      // PRESET EDITOR
      UIDynamicButton createPresetButton = Utils.SetupButton(this, "New Preset", UICreatePreset, false);
      createPresetButton.buttonColor = new Color(0.5f, 1.0f, 0.5f);

      presetToEditStorable = Utils.SetupStringChooser(this, "Preset", new List<string>(), false, false);
      presetToEditStorable.setCallbackFunction += UISetPresetToEdit;

      presetNameStorable = new JSONStorableString("Name", "", UISetPresetName);
      Utils.SetupTextInput(this, "Name", presetNameStorable, false);

      Utils.SetupSpacer(this, 15, false);

      UIDynamicButton deletePresetButton = Utils.SetupButton(this, "Delete Preset", UIDeletePreset, false);
      deletePresetButton.buttonColor = new Color(1.0f, 0.5f, 0.5f);

      // MORPH SELECTOR
      Utils.SetupButton(this, "Add All Favorited Morphs", UIAddAllFavoriteMorphs, true);
      selectMorphStorable = Utils.SetupStringChooser(this, "Add Morph", GetSortedFavoriteMorphs(), -1, true, false);
      selectMorphStorable.setCallbackFunction += UIMorphSelected;
    }

    void UICreatePreset()
    {
      int nameIncrement = 1;
      List<string> presetNames = GetPresetNameList();
      string name;
      do
      {
        name = $"Preset {nameIncrement}";
        nameIncrement++;
      } while (presetNames.Contains(name));

      morphPresets.Add(new MorphPreset()
      {
        Name = name,
        Morphs = GetCurrentMorphValues(),
      });

      UIUpdatePresetDropdownChoices();
      presetToEditStorable.val = name;
    }

    void UIDeletePreset()
    {
      if (targetStateStorable.val == presetToEditStorable.val)
      {
        targetStateStorable.val = null;
      }

      int index = GetPresetNameList().FindIndex((string name) => presetToEditStorable.val == name);
      MorphPreset preset = GetPresetByName(presetToEditStorable.val);
      morphPresets.Remove(preset);

      List<string> presetList = GetPresetNameList();
      // Attempt to select closest dropdown item
      string nextItem = presetList.Count > 0
        ? index < presetList.Count
          ? presetList[index]
          : presetList.Last()
        : null;

      UIUpdatePresetDropdownChoices();
      presetToEditStorable.val = nextItem;
    }

    void UISetPresetToEdit(string val)
    {
      presetNameStorable.valNoCallback = val;
      targetStateStorable.val = val;
      UIRefreshMorphList();
    }

    void UISetPresetName(string val)
    {
      MorphPreset preset = GetPresetByName(presetToEditStorable.val);
      preset.Name = val;
      if (targetStateStorable.val == presetToEditStorable.val)
      {
        targetStateStorable.valNoCallback = val;
      }
      presetToEditStorable.valNoCallback = val;
      UIUpdatePresetDropdownChoices();
    }

    void UIUpdatePresetDropdownChoices()
    {
      List<string> presetList = GetPresetNameList();
      presetList.Sort();
      targetStateStorable.choices = presetList;
      presetToEditStorable.choices = presetList;
    }

    void UIAddAllFavoriteMorphs()
    {
      GetSortedFavoriteMorphs().ForEach((morphName) =>
      {
        selectedMorphs.Add(morphName);
      });

      SyncMorphPresetsToSelectedMorphs();
      UIRefreshMorphList();
    }

    void UIMorphSelected(string value)
    {
      selectedMorphs.Add(value);
      selectMorphStorable.valNoCallback = "";

      SyncMorphPresetsToSelectedMorphs();
      UIRefreshMorphList();
    }

    void UIRefreshMorphList()
    {
      Utils.RemoveUIElements(this, dynamicUIObjects);

      List<string> morphList = selectedMorphs.ToList();
      morphList.Sort();
      foreach (string morphName in morphList)
      {
        UIDynamicLabelXButton label = Utils.SetupLabelXButton(this, morphName, () => { UIRemoveMorph(morphName); }, true);
        dynamicUIObjects.Add(label);

        MorphPreset currentPreset = GetPresetByName(presetToEditStorable.val);
        if (currentPreset != null)
        {
          DAZMorph morph = GetMorph(morphName);
          if (!currentPreset.Morphs.ContainsKey(morphName))
          {
            currentPreset.Morphs[morphName] = morph.morphValue;
          }

          float value = currentPreset.Morphs[morphName];
          JSONStorableFloat slider = Utils.SetupSliderFloatWithRange(this, morphName, morph.startValue, morph.min, morph.max, true, false);
          slider.setCallbackFunction += (v) => { UIUpdateMorphSlider(morph, currentPreset, v); };
          slider.valNoCallback = value;
          dynamicUIObjects.Add(slider);

          UIDynamic spacer = Utils.SetupSpacer(this, 5, true);
          dynamicUIObjects.Add(spacer);
        }
      }
    }

    void UIRemoveMorph(string morphName)
    {
      selectedMorphs.Remove(morphName);

      SyncMorphPresetsToSelectedMorphs();
      UIRefreshMorphList();
    }

    void UIUpdateMorphSlider(DAZMorph morph, MorphPreset preset, float value)
    {
      preset.Morphs[morph.displayName] = value;
      morph.morphValue = value;
    }

    public void Update()
    {
      UpdateTimeline();

      if (!isUIOpenLastFrame && UITransform.gameObject.activeInHierarchy)
      {
        isUIOpenLastFrame = true;
        UIRefreshMorphList();
      }
      else if (isUIOpenLastFrame && !UITransform.gameObject.activeInHierarchy)
      {
        isUIOpenLastFrame = false;
      }
    }


    void UpdateTimeline()
    {
      if (!isEnabledStorable.val)
      {
        playing = false;
        return;
      }

      if (targetStateStorable.val != previousFrameTargetState)
      {
        playing = true;
        transitionTimer = 0f;
        previousFrameTargetState = targetStateStorable.val;

        MorphPreset nextPreset = GetPresetByName(targetStateStorable.val);

        if (nextPreset == null)
        {
          playing = false;
          return;
        }

        targetMorphs = nextPreset.Morphs;
        previousMorphs = GetCurrentMorphValues();
      }

      if (playing && transitionTimer <= transitionTimeStorable.val)
      {
        transitionTimer += Time.deltaTime;
        float t = Mathf.Clamp01(transitionTimer / transitionTimeStorable.val);
        t = Easings.ApplyEasingFromSelection(t, transitionEasingStorable.val);

        foreach (string morphName in selectedMorphs)
        {
          if (!previousMorphs.ContainsKey(morphName) || !targetMorphs.ContainsKey(morphName))
          {
            continue;
          }
          DAZMorph morph = GetMorph(morphName);
          float prevVal = previousMorphs[morphName];
          float targetVal = targetMorphs[morphName];
          morph.morphValue = t * (targetVal - prevVal) + prevVal;
        }
      }
      else
      {
        transitionTimer = 0f;
        playing = false;
      }
    }

    void SyncMorphPresetsToSelectedMorphs()
    {
      morphPresets.ForEach((preset) =>
      {
        List<string> keys = preset.Morphs.Keys.ToList();
        keys.ForEach((key) =>
        {
          if (!selectedMorphs.Contains(key))
          {
            preset.Morphs.Remove(key);
          }
        });

        foreach (string key in selectedMorphs)
        {
          if (!preset.Morphs.ContainsKey(key))
          {
            preset.Morphs[key] = GetMorphValue(key);
          }
        }
      });
    }

    List<DAZMorph> GetAllFavoriteMorphs()
    {
      return geometry.morphsControlUI.GetMorphs().FindAll((DAZMorph morph) => morph.favorite);
    }

    List<string> GetSortedFavoriteMorphs()
    {
      List<string> list = GetAllFavoriteMorphs().Select((morph) => morph.displayName).ToList();
      list.Sort();
      return list;
    }

    DAZMorph GetMorph(string morphName)
    {
      return geometry.morphsControlUI.GetMorphByDisplayName(morphName);
    }

    float GetMorphValue(string morphName)
    {
      return geometry.morphsControlUI.GetMorphByDisplayName(morphName).morphValue;
    }

    MorphPreset GetPresetByName(string name)
    {
      return morphPresets.Find((preset) => preset.Name == name);
    }

    List<string> GetPresetNameList()
    {
      return morphPresets.Select((preset) => preset.Name).ToList();
    }

    Dictionary<string, float> GetCurrentMorphValues()
    {
      Dictionary<string, float> morphValues = new Dictionary<string, float>();
      foreach (string morphName in selectedMorphs)
      {
        morphValues[morphName] = GetMorphValue(morphName);
      }

      return morphValues;
    }

    public override JSONClass GetJSON(bool includePhysical = true, bool includeAppearance = true, bool forceStore = false)
    {
      JSONClass json = base.GetJSON(includePhysical, includeAppearance, forceStore);

      JSONArray presetsListJSON = new JSONArray();
      morphPresets.ForEach((preset) =>
      {
        JSONClass presetJSON = new JSONClass();

        JSONClass morphDataJSON = new JSONClass();
        foreach (var entry in preset.Morphs)
        {
          morphDataJSON[entry.Key].AsFloat = entry.Value;
        }

        presetJSON["name"] = preset.Name;
        presetJSON["morphData"] = morphDataJSON.AsObject;
        presetsListJSON.Add("", presetJSON);
      });
      json["presets"] = presetsListJSON.AsArray;

      JSONArray selectedMorphsJSON = new JSONArray();
      foreach (string morphName in selectedMorphs)
      {
        selectedMorphsJSON.Add("", morphName);
      }
      json["selectedMorphs"] = selectedMorphsJSON.AsArray;

      return json;
    }

    public override void RestoreFromJSON(JSONClass json, bool restorePhysical = true, bool restoreAppearance = true, JSONArray presetAtoms = null, bool setMissingToDefault = true)
    {
      base.RestoreFromJSON(json, restorePhysical, restoreAppearance, presetAtoms, setMissingToDefault);

      morphPresets = new List<MorphPreset>();
      if (json["presets"] != null)
      {
        foreach (JSONNode presetJSON in json["presets"].AsArray)
        {
          Dictionary<string, float> morphData = new Dictionary<string, float>();

          foreach (string key in presetJSON["morphData"].AsObject.Keys)
          {
            morphData[key] = presetJSON["morphData"][key].AsFloat;
          }

          morphPresets.Add(new MorphPreset()
          {
            Name = presetJSON["name"],
            Morphs = morphData,
          });
        }
      }

      selectedMorphs = new HashSet<string>();
      if (json["selectedMorphs"] != null)
      {
        foreach (JSONNode node in json["selectedMorphs"].AsArray)
        {
          selectedMorphs.Add(node);
        }
      }
      else
      {
        morphPresets.ForEach((preset) =>
        {
          foreach (string key in preset.Morphs.Keys.ToList())
          {
            selectedMorphs.Add(key);
          }
        });
      }

      UIUpdatePresetDropdownChoices();
    }
  }
}
