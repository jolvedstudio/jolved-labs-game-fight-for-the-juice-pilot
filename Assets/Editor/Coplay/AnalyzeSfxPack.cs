using System;
using System.IO;
using System.Text;
using UnityEngine;

public class AnalyzeSfxPack
{
    public static string Execute()
    {
        string dir = "Assets/Casual Game Sounds U6/CasualGameSounds";
        var sb = new StringBuilder();
        var files = Directory.GetFiles(dir, "*.wav");
        Array.Sort(files);

        foreach (var f in files)
        {
            try
            {
                byte[] bytes = File.ReadAllBytes(f);
                // Parse WAV header
                int channels = BitConverter.ToInt16(bytes, 22);
                int sampleRate = BitConverter.ToInt32(bytes, 24);
                int bitsPerSample = BitConverter.ToInt16(bytes, 34);

                // find 'data' chunk
                int idx = 12;
                int dataSize = 0;
                int dataOffset = 0;
                while (idx + 8 <= bytes.Length)
                {
                    string chunkId = Encoding.ASCII.GetString(bytes, idx, 4);
                    int chunkSize = BitConverter.ToInt32(bytes, idx + 4);
                    if (chunkId == "data")
                    {
                        dataSize = chunkSize;
                        dataOffset = idx + 8;
                        break;
                    }
                    idx += 8 + chunkSize + (chunkSize % 2);
                }

                int bytesPerSample = bitsPerSample / 8;
                int totalSamples = dataSize / bytesPerSample / Mathf.Max(1, channels);
                float duration = (float)totalSamples / sampleRate;

                // Compute simple energy envelope: split into early/late halves to detect attack vs sustain,
                // and zero-crossing rate as a crude brightness proxy (mono, first channel only).
                double sumSq = 0;
                int zeroCross = 0;
                int prevSign = 0;
                int sampleCount = 0;
                int step = Mathf.Max(1, totalSamples / 4000); // subsample for speed
                double firstHalfEnergy = 0, secondHalfEnergy = 0;
                int half = totalSamples / 2;

                for (int s = 0; s < totalSamples; s += step)
                {
                    int pos = dataOffset + s * bytesPerSample * channels;
                    if (pos + 1 >= bytes.Length) break;
                    short sample = BitConverter.ToInt16(bytes, pos);
                    float v = sample / 32768f;
                    sumSq += v * v;
                    int sign = v > 0 ? 1 : (v < 0 ? -1 : 0);
                    if (sign != 0 && prevSign != 0 && sign != prevSign) zeroCross++;
                    if (sign != 0) prevSign = sign;
                    if (s < half) firstHalfEnergy += v * v; else secondHalfEnergy += v * v;
                    sampleCount++;
                }

                float rms = sampleCount > 0 ? (float)Math.Sqrt(sumSq / sampleCount) : 0;
                float zcr = sampleCount > 0 ? (float)zeroCross / sampleCount : 0;
                string shape = secondHalfEnergy > firstHalfEnergy * 1.2f ? "rising" :
                               (firstHalfEnergy > secondHalfEnergy * 1.5f ? "decay" : "flat");

                sb.AppendLine($"{Path.GetFileNameWithoutExtension(f)} | dur={duration:F2}s | rms={rms:F3} | zcr(bright)={zcr:F3} | shape={shape} | {channels}ch {sampleRate}Hz");
            }
            catch (Exception e)
            {
                sb.AppendLine($"{Path.GetFileName(f)} | ERROR {e.Message}");
            }
        }
        return sb.ToString();
    }
}
