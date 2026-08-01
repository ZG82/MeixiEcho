using UnityEngine;

namespace MeixiEcho
{
    /// <summary>
    /// Lightweight stateful interaction for the theatre's secondary props.
    /// It deliberately owns no input code: ChapterTwoAdventureRuntime keeps a
    /// single focus/raycast path, while this component only animates one part.
    /// </summary>
    public sealed class ChapterTwoSetProp : MonoBehaviour
    {
        public enum MotionKind { Hinge, Slide, Turn, Pulse }
        public enum SoundKind { Wood, Metal, Cloth, Drum }

        private static AudioClip woodClip;
        private static AudioClip metalClip;
        private static AudioClip clothClip;
        private static AudioClip drumClip;

        private Transform movingPart;
        private MotionKind motion;
        private SoundKind sound;
        private Vector3 closedPosition;
        private Vector3 openPosition;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Vector3 closedScale;
        private AudioSource audioSource;
        private float progress;
        private float targetProgress;
        private float pulseClock;
        private bool oneShot;

        public string Prompt { get; private set; }
        public string OpenedMessage { get; private set; }
        public string ClosedMessage { get; private set; }

        public void ConfigureHinge(Transform part, Vector3 openEuler, string prompt,
            string openedMessage, string closedMessage, SoundKind soundKind = SoundKind.Wood,
            bool oneWay = false)
        {
            Configure(part, MotionKind.Hinge, prompt, openedMessage, closedMessage, soundKind, oneWay);
            openRotation = closedRotation * Quaternion.Euler(openEuler);
        }

        public void ConfigureSlide(Transform part, Vector3 localDelta, string prompt,
            string openedMessage, string closedMessage, SoundKind soundKind = SoundKind.Wood,
            bool oneWay = false)
        {
            Configure(part, MotionKind.Slide, prompt, openedMessage, closedMessage, soundKind, oneWay);
            openPosition = closedPosition + localDelta;
        }

        public void ConfigureTurn(Transform part, Vector3 openEuler, string prompt,
            string openedMessage, string closedMessage, SoundKind soundKind = SoundKind.Metal,
            bool oneWay = false)
        {
            Configure(part, MotionKind.Turn, prompt, openedMessage, closedMessage, soundKind, oneWay);
            openRotation = closedRotation * Quaternion.Euler(openEuler);
        }

        public void ConfigurePulse(Transform part, string prompt, string message,
            SoundKind soundKind = SoundKind.Drum)
        {
            Configure(part, MotionKind.Pulse, prompt, message, message, soundKind, false);
        }

        private void Configure(Transform part, MotionKind kind, string prompt, string openedMessage,
            string closedMessage, SoundKind soundKind, bool oneWay)
        {
            movingPart = part != null ? part : transform;
            motion = kind;
            sound = soundKind;
            Prompt = prompt;
            OpenedMessage = openedMessage;
            ClosedMessage = closedMessage;
            oneShot = oneWay;
            closedPosition = movingPart.localPosition;
            openPosition = closedPosition;
            closedRotation = movingPart.localRotation;
            openRotation = closedRotation;
            closedScale = movingPart.localScale;
            // The saved EditorOnly preview never plays prop audio. Avoid adding an
            // AudioSource while the scene builder is destroying preview-only audio
            // components; Unity 6 can otherwise return a component already queued
            // for edit-mode destruction. Runtime instances still receive full audio.
            if (Application.isPlaying && !Application.isEditor)
            {
                audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
                if (audioSource != null)
                {
                    audioSource.playOnAwake = false;
                    audioSource.spatialBlend = 1f;
                    audioSource.minDistance = 1.1f;
                    audioSource.maxDistance = 13f;
                    audioSource.rolloffMode = AudioRolloffMode.Linear;
                }
            }
            else audioSource = null;
        }

        public string Interact()
        {
            if (movingPart == null) return string.Empty;
            if (motion == MotionKind.Pulse)
            {
                pulseClock = .42f;
                PlaySound();
                return OpenedMessage;
            }
            if (oneShot && targetProgress > .5f) return OpenedMessage;
            targetProgress = targetProgress > .5f ? 0f : 1f;
            PlaySound();
            return targetProgress > .5f ? OpenedMessage : ClosedMessage;
        }

        private void Update()
        {
            if (movingPart == null) return;
            float rate = motion == MotionKind.Slide ? 7.5f : 6.2f;
            progress = Mathf.MoveTowards(progress, targetProgress, rate * Time.deltaTime);
            float eased = progress * progress * (3f - 2f * progress);
            if (motion == MotionKind.Slide)
                movingPart.localPosition = Vector3.LerpUnclamped(closedPosition, openPosition, eased);
            else if (motion == MotionKind.Hinge || motion == MotionKind.Turn)
                movingPart.localRotation = Quaternion.SlerpUnclamped(closedRotation, openRotation, eased);

            if (motion != MotionKind.Pulse || pulseClock <= 0f) return;
            pulseClock = Mathf.Max(0f, pulseClock - Time.deltaTime);
            float normalized = 1f - pulseClock / .42f;
            float impact = Mathf.Sin(normalized * Mathf.PI) * (1f - normalized) * .065f;
            movingPart.localScale = closedScale + new Vector3(impact, -impact * .65f, impact);
            if (pulseClock <= 0f) movingPart.localScale = closedScale;
        }

        private void PlaySound()
        {
            if (audioSource == null) return;
            AudioClip clip = sound switch
            {
                SoundKind.Metal => metalClip ??= CreateClip("后台旧铜件", SoundKind.Metal),
                SoundKind.Cloth => clothClip ??= CreateClip("后台旧戏服", SoundKind.Cloth),
                SoundKind.Drum => drumClip ??= CreateClip("排练小鼓", SoundKind.Drum),
                _ => woodClip ??= CreateClip("后台旧木器", SoundKind.Wood)
            };
            audioSource.pitch = 1f + Random.Range(-.035f, .035f);
            audioSource.volume = sound == SoundKind.Drum ? .48f : .34f;
            audioSource.PlayOneShot(clip);
        }

        private static AudioClip CreateClip(string name, SoundKind kind)
        {
            const int rate = 22050;
            float duration = kind == SoundKind.Drum ? .48f : .25f;
            int sampleCount = Mathf.CeilToInt(rate * duration);
            float[] samples = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float time = i / (float)rate;
                float envelope = Mathf.Exp(-(kind == SoundKind.Drum ? 8.5f : 16f) * time);
                float baseFrequency = kind switch
                {
                    SoundKind.Metal => 520f,
                    SoundKind.Cloth => 82f,
                    SoundKind.Drum => 116f,
                    _ => 154f
                };
                float tone = Mathf.Sin(time * baseFrequency * Mathf.PI * 2f);
                float overtone = Mathf.Sin(time * baseFrequency * 2.37f * Mathf.PI * 2f) * .32f;
                float noise = Mathf.Sin(i * 1.713f) * Mathf.Sin(i * .071f) *
                              (kind == SoundKind.Cloth ? .44f : .12f);
                samples[i] = (tone + overtone + noise) * envelope * .42f;
            }
            AudioClip clip = AudioClip.Create(name, sampleCount, 1, rate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
