using System;

namespace OsuSkinMixer
{
    public class SkinCreationInvalidException : Exception
    {
        public SkinCreationInvalidException()
        {
        }

        public SkinCreationInvalidException(string message)
            : base(message)
        {
        }

        public SkinCreationInvalidException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}