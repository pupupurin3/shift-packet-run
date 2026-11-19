using UnityEngine;

namespace ShiftRunner.Presentation
{
    internal sealed class PacketAudio
    {
        readonly AudioSource source;
        readonly AudioClip[] clips=new AudioClip[5];
        public bool Muted { get { return source.mute; } set { source.mute=value; } }

        public PacketAudio(GameObject host)
        {
            source=host.AddComponent<AudioSource>(); source.playOnAwake=false; source.spatialBlend=0;
            clips[(int)GameCue.Attack]=Chirp("Attack",650,260,.07f);
            clips[(int)GameCue.Build]=Chirp("Build",410,975,.16f);
            clips[(int)GameCue.Erase]=Chirp("Erase",860,280,.12f);
            clips[(int)GameCue.Death]=Chirp("Reboot",290,54,.25f);
            clips[(int)GameCue.Boost]=Chirp("Boost",480,1460,.22f);
        }

        static AudioClip Chirp(string name,float start,float end,float duration)
        {
            const int sampleRate=22050;
            float[] samples=new float[Mathf.RoundToInt(sampleRate*duration)];
            float phase=0;
            for(int i=0;i<samples.Length;i++)
            {
                float t=i/(float)samples.Length;
                phase+=Mathf.Lerp(start,end,t)*Mathf.PI*2/sampleRate;
                float envelope=Mathf.Clamp01(t*23f)*(1-t)*(1-t);
                samples[i]=(Mathf.Sin(phase)+.15f*Mathf.Sin(phase*2))*envelope*.19f;
            }
            var clip=AudioClip.Create(name,samples.Length,1,sampleRate,false);
            clip.SetData(samples,0);
            return clip;
        }
        public void Play(GameCue cue) { if(!Muted) source.PlayOneShot(clips[(int)cue]); }
        public void Dispose() { foreach(AudioClip clip in clips) Object.Destroy(clip); }
    }
}
