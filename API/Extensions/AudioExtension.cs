using System.IO;
using UnityEngine;
using MEC;
using Exiled.API.Features;

namespace SCP1356Main.API.Extensions
{
    public static class AudioExtensions
    {
        public static void PlayAudioAt(this Vector3 position, string clipPath, float maxDistance, float duration)
        {
            Log.Debug($"Playing audio step 1");
            string playerName = GenerateRandomString(6);
            string speakerName = GenerateRandomString(6);
            string clipName = GenerateRandomString(6);
            Log.Debug($"Playing audio step 2");
            string fullPath = Path.Combine(Paths.Plugins, "audio", clipPath);
            Log.Debug($"Playing audio step 3");
            AudioClipStorage.LoadClip(fullPath, clipName);
            Log.Debug($"Playing audio step 4");
            var player = AudioPlayer.Create(playerName);
            var speaker = player.AddSpeaker(speakerName, 1f, true, 1, maxDistance);
            Log.Debug($"Playing audio at {fullPath}");

            speaker.Position = position;

            player.AddClip(clipName, 1f, false, true);

            bool cleaned = false;

            void Cleanup()
            {
                if (cleaned) return;
                cleaned = true;

                try
                {
                    AudioClipStorage.DestroyClip(clipName);
                    player.RemoveSpeaker(speakerName);
                    player.Destroy();
                }
                catch { }
            }

            Timing.CallDelayed(duration, () =>
            {
                if (!Round.IsEnded)
                    Cleanup();
            });

            if (Round.IsEnded)
                Cleanup();
        }

        private static string GenerateRandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            char[] buffer = new char[length];

            for (int i = 0; i < length; i++)
                buffer[i] = chars[Plugin.Random.Next(chars.Length)];

            return new string(buffer);
        }
    }
}