using Godot;

namespace MineYourBusiness;

/// <summary>Small original synthesized cues; no external audio license is required.</summary>
public partial class AudioCuePlayer : AudioStreamPlayer
{
    private double _volume = 0.75;

    public void SetVolume(double volume)
    {
        _volume = Math.Clamp(volume, 0, 1);
        VolumeDb = _volume <= 0 ? -80 : Mathf.LinearToDb((float)_volume);
    }

    public void PlayClick() => PlayTone(330, 0.045, 0.12);

    public void PlaySuccess() => PlayTone(660, 0.11, 0.18);

    public void PlayWarning() => PlayTone(165, 0.13, 0.16);

    public void PlayReveal() => PlayTone(440, 0.18, 0.14);

    private void PlayTone(double frequency, double durationSeconds, double amplitude)
    {
        if (_volume <= 0)
        {
            return;
        }

        const int sampleRate = 22050;
        int sampleCount = (int)(sampleRate * durationSeconds);
        byte[] data = new byte[sampleCount * sizeof(short)];
        for (int sample = 0; sample < sampleCount; sample++)
        {
            double progress = sample / (double)sampleCount;
            double envelope = Math.Sin(Math.PI * progress);
            short value = (short)(Math.Sin(2 * Math.PI * frequency * sample / sampleRate) * short.MaxValue * amplitude * envelope);
            data[sample * 2] = (byte)(value & 0xff);
            data[(sample * 2) + 1] = (byte)((value >> 8) & 0xff);
        }

        Stream = new AudioStreamWav
        {
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = false,
            Data = data,
        };
        Play();
    }
}
