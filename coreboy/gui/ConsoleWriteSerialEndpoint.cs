using System;
using coreboy.serial;

namespace coreboy.gui
{
    public class ConsoleWriteSerialEndpoint : SerialEndpoint
    {
        public int transfer(int b)
        {
            Console.Write((char) b);
            return 0;
        }
    }
}