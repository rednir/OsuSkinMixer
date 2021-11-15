using System;

namespace OsuSkinMixer
{
    public class SkinExistsException : Exception
    {
        public SkinExistsException()
        {
        }

        public SkinExistsException(string message)
            : base(message)
        {
        }

        public SkinExistsException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}