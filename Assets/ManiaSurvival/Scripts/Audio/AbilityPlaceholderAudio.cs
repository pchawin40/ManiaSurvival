using System.Collections.Generic;
using UnityEngine;

public enum AbilityPlaceholderSound
{
    Spray,
    HookFire,
    HookHit,
    Tonic,
    BarrageStart,
    BarrageBomb,
    BarrageEnd,
    Heal,
    NoMana,
    CooldownFail,
    Generic
}

public static class AbilityPlaceholderAudio
{
    private static readonly Dictionary<AbilityPlaceholderSound, AudioClip> cachedClips = new Dictionary<AbilityPlaceholderSound, AudioClip>();
    private static readonly HashSet<AbilityPlaceholderSound> loggedMissing = new HashSet<AbilityPlaceholderSound>();

    public static void Play(
        AudioSource source,
        AudioClip assignedClip,
        AbilityPlaceholderSound fallbackType,
        float volume,
        bool allowPlaceholder = true,
        float pitch = 1f)
    {
        if (source == null)
        {
            return;
        }

        AudioClip clip = assignedClip;
        if (clip == null && allowPlaceholder)
        {
            clip = GetPlaceholderClip(fallbackType);
        }

        if (clip == null)
        {
            LogMissingOnce(fallbackType);
            return;
        }

        source.pitch = pitch;
        source.PlayOneShot(clip, Mathf.Clamp01(volume));
        source.pitch = 1f;
    }

    public static AudioClip GetPlaceholderClip(AbilityPlaceholderSound type)
    {
        if (cachedClips.TryGetValue(type, out AudioClip existing) && existing != null)
        {
            return existing;
        }

        AudioClip generated = GenerateTone(type);
        cachedClips[type] = generated;
        return generated;
    }

    private static void LogMissingOnce(AbilityPlaceholderSound type)
    {
        if (loggedMissing.Add(type))
        {
            Debug.Log("[AbilityAudio] No clip assigned for " + type + " and placeholder generation failed.");
        }
    }

    private static AudioClip GenerateTone(AbilityPlaceholderSound type)
    {
        float frequency;
        float duration;
        float volumeScale;

        switch (type)
        {
            case AbilityPlaceholderSound.Spray:
                frequency = 180f;
                duration = 0.14f;
                volumeScale = 0.55f;
                break;
            case AbilityPlaceholderSound.HookFire:
                frequency = 520f;
                duration = 0.1f;
                volumeScale = 0.4f;
                break;
            case AbilityPlaceholderSound.HookHit:
                frequency = 320f;
                duration = 0.12f;
                volumeScale = 0.45f;
                break;
            case AbilityPlaceholderSound.Tonic:
                frequency = 240f;
                duration = 0.22f;
                volumeScale = 0.35f;
                break;
            case AbilityPlaceholderSound.BarrageStart:
                frequency = 140f;
                duration = 0.28f;
                volumeScale = 0.5f;
                break;
            case AbilityPlaceholderSound.BarrageBomb:
                frequency = 95f;
                duration = 0.16f;
                volumeScale = 0.6f;
                break;
            case AbilityPlaceholderSound.BarrageEnd:
                frequency = 110f;
                duration = 0.2f;
                volumeScale = 0.35f;
                break;
            case AbilityPlaceholderSound.Heal:
                frequency = 660f;
                duration = 0.12f;
                volumeScale = 0.3f;
                break;
            case AbilityPlaceholderSound.NoMana:
            case AbilityPlaceholderSound.CooldownFail:
                frequency = 220f;
                duration = 0.05f;
                volumeScale = 0.18f;
                break;
            default:
                frequency = 440f;
                duration = 0.08f;
                volumeScale = 0.25f;
                break;
        }

        return BuildToneClip(frequency, duration, volumeScale, type);
    }

    private static AudioClip BuildToneClip(float frequency, float duration, float volumeScale, AbilityPlaceholderSound type)
    {
        int sampleRate = 44100;
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(sampleRate * duration));
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = i / (float)sampleRate;
            float envelope = 1f - (t / duration);
            float wave = Mathf.Sin(2f * Mathf.PI * frequency * t);

            if (type == AbilityPlaceholderSound.Spray || type == AbilityPlaceholderSound.BarrageBomb)
            {
                wave += Mathf.Sin(2f * Mathf.PI * frequency * 0.5f * t) * 0.35f;
                wave = Mathf.Clamp(wave, -1f, 1f);
            }

            if (type == AbilityPlaceholderSound.Tonic)
            {
                wave = Mathf.Sin(2f * Mathf.PI * frequency * t) * 0.6f
                    + Mathf.Sin(2f * Mathf.PI * (frequency * 1.6f) * t) * 0.4f;
            }

            samples[i] = wave * envelope * volumeScale;
        }

        AudioClip clip = AudioClip.Create("Placeholder_" + type, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
