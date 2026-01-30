using Microsoft.Xna.Framework.Audio;

namespace DemonAttackClone.Managers;

public class SoundManager
{
    private SoundEffect? _shootSound;
    private SoundEffect? _explosionSound;
    private SoundEffect? _playerHitSound;
    private SoundEffect? _waveCompleteSound;
    private SoundEffect? _gameOverSound;
    private SoundEffect? _demonShootSound;

    private float _masterVolume = 0.5f;

    public void Initialize()
    {
        _shootSound = CreateShootSound();
        _explosionSound = CreateExplosionSound();
        _playerHitSound = CreatePlayerHitSound();
        _waveCompleteSound = CreateWaveCompleteSound();
        _gameOverSound = CreateGameOverSound();
        _demonShootSound = CreateDemonShootSound();
    }

    public void PlayShoot() => _shootSound?.Play(_masterVolume * 0.6f, 0f, 0f);
    public void PlayExplosion() => _explosionSound?.Play(_masterVolume * 0.8f, 0f, 0f);
    public void PlayPlayerHit() => _playerHitSound?.Play(_masterVolume, 0f, 0f);
    public void PlayWaveComplete() => _waveCompleteSound?.Play(_masterVolume * 0.7f, 0f, 0f);
    public void PlayGameOver() => _gameOverSound?.Play(_masterVolume * 0.8f, 0f, 0f);
    public void PlayDemonShoot() => _demonShootSound?.Play(_masterVolume * 0.4f, 0f, 0f);

    private SoundEffect CreateShootSound()
    {
        // Short high-pitched laser sound
        int sampleRate = 44100;
        int duration = (int)(sampleRate * 0.08); // 80ms
        byte[] buffer = new byte[duration * 2];

        for (int i = 0; i < duration; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = 1f - (float)i / duration;
            float frequency = 880 + (1 - envelope) * 440; // Descending pitch
            float sample = MathF.Sin(2 * MathF.PI * frequency * t) * envelope;

            // Add some harmonics for richness
            sample += MathF.Sin(4 * MathF.PI * frequency * t) * envelope * 0.3f;

            short value = (short)(sample * 16000);
            buffer[i * 2] = (byte)(value & 0xFF);
            buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }

    private SoundEffect CreateExplosionSound()
    {
        // Noise-based explosion
        int sampleRate = 44100;
        int duration = (int)(sampleRate * 0.25); // 250ms
        byte[] buffer = new byte[duration * 2];
        Random random = new Random(42);

        for (int i = 0; i < duration; i++)
        {
            float t = (float)i / duration;
            float envelope = MathF.Pow(1f - t, 1.5f);

            // White noise with low-pass filter effect
            float noise = (float)(random.NextDouble() * 2 - 1);

            // Add low frequency rumble
            float rumble = MathF.Sin(2 * MathF.PI * 60 * t) * 0.5f;
            float sample = (noise * 0.7f + rumble * 0.3f) * envelope;

            short value = (short)(sample * 20000);
            buffer[i * 2] = (byte)(value & 0xFF);
            buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }

    private SoundEffect CreatePlayerHitSound()
    {
        // Low thud with rumble
        int sampleRate = 44100;
        int duration = (int)(sampleRate * 0.3); // 300ms
        byte[] buffer = new byte[duration * 2];
        Random random = new Random(123);

        for (int i = 0; i < duration; i++)
        {
            float t = (float)i / duration;
            float envelope = MathF.Pow(1f - t, 2f);

            // Low frequency thud
            float thud = MathF.Sin(2 * MathF.PI * 80 * (float)i / sampleRate);
            // Noise burst
            float noise = (float)(random.NextDouble() * 2 - 1) * (1f - t * 2);
            if (noise < 0) noise = 0;

            float sample = (thud * 0.6f + noise * 0.4f) * envelope;

            short value = (short)(sample * 24000);
            buffer[i * 2] = (byte)(value & 0xFF);
            buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }

    private SoundEffect CreateWaveCompleteSound()
    {
        // Ascending arpeggio
        int sampleRate = 44100;
        int duration = (int)(sampleRate * 0.5); // 500ms
        byte[] buffer = new byte[duration * 2];

        float[] notes = { 523.25f, 659.25f, 783.99f, 1046.50f }; // C5, E5, G5, C6
        int noteLength = duration / notes.Length;

        for (int i = 0; i < duration; i++)
        {
            int noteIndex = Math.Min(i / noteLength, notes.Length - 1);
            float frequency = notes[noteIndex];

            float t = (float)i / sampleRate;
            float noteT = (float)(i % noteLength) / noteLength;
            float envelope = 1f - noteT * 0.3f;

            float sample = MathF.Sin(2 * MathF.PI * frequency * t) * envelope;
            sample += MathF.Sin(4 * MathF.PI * frequency * t) * envelope * 0.2f;

            short value = (short)(sample * 12000);
            buffer[i * 2] = (byte)(value & 0xFF);
            buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }

    private SoundEffect CreateGameOverSound()
    {
        // Descending sad tones
        int sampleRate = 44100;
        int duration = (int)(sampleRate * 0.8); // 800ms
        byte[] buffer = new byte[duration * 2];

        for (int i = 0; i < duration; i++)
        {
            float t = (float)i / duration;
            float frequency = 400 - t * 200; // Descend from 400Hz to 200Hz
            float envelope = (1f - t * 0.5f) * MathF.Pow(1f - t, 0.3f);

            float sample = MathF.Sin(2 * MathF.PI * frequency * (float)i / sampleRate);
            // Add wobble
            sample *= 1f + MathF.Sin(2 * MathF.PI * 6 * t) * 0.1f;
            sample *= envelope;

            short value = (short)(sample * 16000);
            buffer[i * 2] = (byte)(value & 0xFF);
            buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }

    private SoundEffect CreateDemonShootSound()
    {
        // Alien zap sound
        int sampleRate = 44100;
        int duration = (int)(sampleRate * 0.1); // 100ms
        byte[] buffer = new byte[duration * 2];

        for (int i = 0; i < duration; i++)
        {
            float t = (float)i / duration;
            float envelope = 1f - t;

            // Descending frequency with wobble
            float frequency = 300 + (1 - t) * 200;
            float wobble = MathF.Sin(2 * MathF.PI * 30 * t) * 50;

            float sample = MathF.Sin(2 * MathF.PI * (frequency + wobble) * (float)i / sampleRate);
            sample *= envelope;

            short value = (short)(sample * 10000);
            buffer[i * 2] = (byte)(value & 0xFF);
            buffer[i * 2 + 1] = (byte)((value >> 8) & 0xFF);
        }

        return new SoundEffect(buffer, sampleRate, AudioChannels.Mono);
    }
}
