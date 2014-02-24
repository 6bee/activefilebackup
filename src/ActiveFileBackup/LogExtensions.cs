using log4net;
using log4net.Core;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace ActiveFileBackup
{
    public static class LogExtensions
    {
        public static void Trace(this ILog log)
        {
            if (log.Logger.IsEnabledFor(Level.Trace))
            {
                var frame = new StackFrame(1, true);
                var method = frame.GetMethod();
                var message = string.Format("[{0} {1}] {3}{4}({5}) \n\t{6}:{7}",
                    Thread.CurrentThread.ManagedThreadId,
                    Thread.CurrentThread.Name,
                    null, //Thread.CurrentThread.Priority,
                    method.DeclaringType.Name,
                    method.Name,
                    string.Join(", ", method.GetParameters().Select(x => string.Format("{0} {1}", x.ParameterType, x.Name))),
                    frame.GetFileName(),
                    frame.GetFileLineNumber());
                log.Logger.Log(new LoggingEvent(new LoggingEventData()
                {
                    Level = Level.Trace,
                    LoggerName = log.Logger.Name,
                    Message = message,
                    TimeStamp = DateTime.Now,
                }));
            }
        }
    }
}
