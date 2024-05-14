using System.IO;

namespace NeverSerender
{
    public class MiniLog
    {
        private readonly StreamWriter writer;
        private readonly object _lock = new object();

        public MiniLog(StreamWriter writer)
        {
            this.writer = writer;
        }

        public void WriteLine(string message)
        {
            lock (_lock)
            {
                writer.WriteLine(message);
                writer.Flush();
            }
        }
    }
}
