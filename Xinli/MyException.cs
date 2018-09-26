using System;

namespace Xinli
{
    class MyException : ApplicationException
    {
        public MyException(string message) : base(message) { }

        public override string Message
        {
            get
            {
                return base.Message;
            }
        }
    }
}