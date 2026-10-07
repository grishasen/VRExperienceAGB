using System;
using UnityEngine;

namespace VRExperienceAGB.Presentation
{
    /// <summary>Original, restrained procedural ambience and positional selection feedback.</summary>
    public sealed class ForestAtmosphere : MonoBehaviour
    {
        private AudioSource breeze, selection;
        public WinterNightSky Sky { get; private set; }
        public HorizonWolves Wildlife { get; private set; }
        private AudioClip breezeClip, selectionClip;
        private bool active, soundEnabled = true;
        public bool SoundEnabled
        {
            get => soundEnabled;
            set { soundEnabled = value; Wildlife?.SetSoundEnabled(value); UpdateSound(); }
        }
        public void Configure(ForestGardenView garden)
        {
            Sky = gameObject.AddComponent<WinterNightSky>(); Sky.Configure(garden);
            Wildlife = gameObject.AddComponent<HorizonWolves>(); Wildlife.Configure(garden);
            var wind = new GameObject("GardenBreeze"); wind.transform.SetParent(garden.transform, false); wind.transform.localPosition = new Vector3(0, 2, 8);
            breeze = wind.AddComponent<AudioSource>(); breeze.loop = true; breeze.playOnAwake = false;
            breeze.spatialBlend = 1; breeze.rolloffMode = AudioRolloffMode.Linear; breeze.minDistance = 6; breeze.maxDistance = 35; breeze.volume = .08f;
            var cue = new GameObject("GardenSelectionCue"); cue.transform.SetParent(garden.transform, false);
            selection = cue.AddComponent<AudioSource>(); selection.playOnAwake = false;
            selection.spatialBlend = 1; selection.rolloffMode = AudioRolloffMode.Linear; selection.minDistance = 1; selection.maxDistance = 15; selection.volume = .12f;
            const int rate = 22050, count = rate * 8;
            var samples = new float[count]; var random = new System.Random(5005); float filtered = 0;
            for (int i = 0; i < count; i++)
            {
                filtered += ((float)random.NextDouble() * 2 - 1 - filtered) * .045f;
                // A periodic envelope brings both ends to silence for a click-free loop.
                float envelope = Mathf.Sin(Mathf.PI * i / (count - 1));
                samples[i] = filtered * envelope * envelope;
            }
            breezeClip = AudioClip.Create("OriginalForestBreeze", count, 1, rate, false); breezeClip.SetData(samples, 0); breeze.clip = breezeClip;
            samples = new float[rate / 7];
            for (int i = 0; i < samples.Length; i++)
            {
                float t = (float)i / rate, envelope = Mathf.Sin(Mathf.PI * i / (samples.Length - 1));
                samples[i] = (Mathf.Sin(2 * Mathf.PI * 660 * t) + .3f * Mathf.Sin(2 * Mathf.PI * 990 * t)) * envelope * envelope * .22f;
            }
            selectionClip = AudioClip.Create("OriginalForestSelection", samples.Length, 1, rate, false); selectionClip.SetData(samples, 0); selection.clip = selectionClip;
        }
        public void SetActive(bool value) { active = value; UpdateSound(); }
        public void SelectAt(Vector3 position)
        {
            if (!soundEnabled || selection == null) return;
            selection.transform.position = position; selection.Play();
        }
        private void UpdateSound()
        {
            if (breeze == null) return;
            if (active && soundEnabled) { if (!breeze.isPlaying) breeze.Play(); }
            else { breeze.Stop(); if (!soundEnabled) selection.Stop(); }
        }
        private void OnDestroy()
        {
            if (breezeClip != null) Destroy(breezeClip);
            if (selectionClip != null) Destroy(selectionClip);
        }
    }
}
