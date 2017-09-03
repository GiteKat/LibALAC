using System;

namespace LibALAC
{
    class LibALACException : Exception
    {
        public LibALACException()
        {
        }

        public LibALACException(string message) : base(message)
        {
        }

        public LibALACException(string message, Exception inner) : base(message, inner)
        {
        }
    }

}
