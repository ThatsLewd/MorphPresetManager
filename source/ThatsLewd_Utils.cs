using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ThatsLewd
{
  public static class Easings
  {
    public static float Linear(float t)
    {
      return t;
    }

    public static float EaseInQuad(float t)
    {
      return t * t;
    }

    public static float EaseOutQuad(float t)
    {
      return 1 - (1 - t) * (1 - t);
    }

    public static float EaseInOutQuad(float t)
    {
      return t < 0.5
        ? 2 * t * t
        : 1 - (-2 * t + 2) * (-2 * t + 2) / 2;
    }

    public static float EaseInCubic(float t)
    {
      return t * t * t;
    }

    public static float EaseOutCubic(float t)
    {
      return 1 - (1 - t) * (1 - t) * (1 - t);
    }

    public static float EaseInOutCubic(float t)
    {
      return t < 0.5
        ? 4 * t * t * t
        : 1 - (-2 * t + 2) * (-2 * t + 2) * (-2 * t + 2) / 2;
    }

    public static float EaseInQuint(float t)
    {
      return t * t * t * t * t;
    }

    public static float EaseOutQuint(float t)
    {
      return 1 - (1 - t) * (1 - t) * (1 - t) * (1 - t) * (1 - t);
    }

    public static float EaseInOutQuint(float t)
    {
      return t < 0.5
        ? 16 * t * t * t * t * t
        : 1 - (-2 * t + 2) * (-2 * t + 2) * (-2 * t + 2) * (-2 * t + 2) * (-2 * t + 2) / 2;
    }

    public static float EaseInExp(float t)
    {
      return t == 0 ? 0 : Mathf.Exp(7 * t - 7);
    }

    public static float EaseOutExp(float t)
    {
      return t == 1 ? 1 : 1 - Mathf.Exp(-7 * t);
    }

    public static float EaseInOutExp(float t)
    {
      return t == 0 ? 0
        : t == 1 ? 1
        : t < 0.5
          ? Mathf.Exp(14 * t - 7) / 2
          : 1 - Mathf.Exp(-14 * t + 7) / 2;
    }

    public static List<string> GetEasingOptions()
    {
      return new List<string>()
      {
        "Linear",
        "EaseInQuad",
        "EaseOutQuad",
        "EaseInOutQuad",
        "EaseInCubic",
        "EaseOutCubic",
        "EaseInOutCubic",
        "EaseInQuint",
        "EaseOutQuint",
        "EaseInOutQuint",
        "EaseInExp",
        "EaseOutExp",
        "EaseInOutExp",
      };
    }

    public static float ApplyEasingFromSelection(float t, string easing)
    {
      switch (easing)
      {
        case "Linear":
          return Linear(t);
        case "EaseInQuad":
          return EaseInQuad(t);
        case "EaseOutQuad":
          return EaseOutQuad(t);
        case "EaseInOutQuad":
          return EaseInOutQuad(t);
        case "EaseInCubic":
          return EaseInCubic(t);
        case "EaseOutCubic":
          return EaseOutCubic(t);
        case "EaseInOutCubic":
          return EaseInOutCubic(t);
        case "EaseInQuint":
          return EaseInQuint(t);
        case "EaseOutQuint":
          return EaseOutQuint(t);
        case "EaseInOutQuint":
          return EaseInOutQuint(t);
        case "EaseInExp":
          return EaseInExp(t);
        case "EaseOutExp":
          return EaseOutExp(t);
        case "EaseInOutExp":
          return EaseInOutExp(t);
        default:
          return Linear(t);
      }
    }
  }
}
