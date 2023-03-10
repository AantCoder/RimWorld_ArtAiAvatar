using System;
using UnityEngine;

namespace ArtAi.data
{
    public class GeneratedImage
    {
        private const int UpToDateSeconds = 3;
            
        public readonly GenerationStatus Status;
        public readonly Texture2D Texture;
        public readonly string Description;
        private readonly DateTime _timestamp;

        private GeneratedImage(GenerationStatus status, Texture2D texture, string description)
        {
            Texture = texture;
            Status = status;
            Description = description;
            _timestamp = DateTime.Now;
        }
        
        public static GeneratedImage InProgress(String description)
        {
            return new GeneratedImage(GenerationStatus.InProgress, null, description);
        }
        
        public static GeneratedImage Done(Texture2D texture)
        {
            return new GeneratedImage(GenerationStatus.Done, texture, null);
        }

        public static GeneratedImage Error()
        {
            return new GeneratedImage(GenerationStatus.Error, null, "Error");
        }

        public bool NeedUpdate()
        {
            var needUpdate = (DateTime.Now - _timestamp).Seconds > UpToDateSeconds;
            return Status != GenerationStatus.Done && needUpdate;
        }
    }

    public enum GenerationStatus
    {
        InProgress,
        Done,
        Error,
    }
}