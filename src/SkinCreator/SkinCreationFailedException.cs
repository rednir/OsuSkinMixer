using System;

namespace OsuSkinMixer
{
    public class SkinCreationFailedException : Exception
    {
        public SkinCreationFailedException()
        {
        }

        public SkinCreationFailedException(string message)
            : base(message)
        {
        }

        public SkinCreationFailedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}