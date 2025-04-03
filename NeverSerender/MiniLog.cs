using System;
using System.IO;

namespace NeverSerender
{
    public class MiniLog
    {
        private readonly StreamWriter writer;
        private readonly object @lock;
        private readonly string name;
        private readonly bool autoFlush;
        private DateTime lastFlushed;

        private MiniLog(StreamWriter writer, object @lock, string name, bool autoFlush)
        {
            this.writer = writer;
            this.@lock = @lock;
            this.name = name;
            this.autoFlush = autoFlush;
            lastFlushed = DateTime.Now;
        }

        public MiniLog(StreamWriter writer, bool autoFlush) : this(writer, new object(), "", autoFlush)
        {
        }

        public MiniLog Named(string childName)
        {
            var newName = this.name != null ? this.name + "/" + childName : childName;
            return new MiniLog(writer, @lock, newName, autoFlush);
        }

        public void WriteLine(string message)
        {
            lock (@lock)
            {
                var firstPrefix = $"[{name}]: ";
                var prefix = new string(' ', firstPrefix.Length);
                var first = true;
                foreach (var line in message.Split('\n'))
                {
                    var linePrefix = first ? firstPrefix : prefix;
                    writer.Write(linePrefix + line + '\n');
                    first = false;
                }

                if (!autoFlush && lastFlushed - DateTime.Now <= TimeSpan.FromSeconds(1)) return;
                writer.Flush();
                lastFlushed = DateTime.Now;
            }
        }

        public void Flush()
        {
            lock (@lock)
            {
                writer.Flush();
            }
        }
    }
}