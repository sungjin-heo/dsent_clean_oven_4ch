using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using log4net.Config;
using log4net.Repository.Hierarchy;

namespace Log
{
    class TextBoxLogAppender : log4net.Appender.IAppender
    {
        System.Windows.Controls.TextBox __textbox;
        readonly object __lockey__ = new object();

        public string Name { get; set; }

        public TextBoxLogAppender(System.Windows.Controls.TextBox textbox)
        {
            __textbox = textbox;
            this.Name = "TextBoxLogAppender";
        }
        public static void ConfigureTextboxAppender(System.Windows.Controls.TextBox textbox)
        {
            var hierachy = (Hierarchy)LogManager.GetRepository();
            var appender = new TextBoxLogAppender(textbox);
            hierachy.Root.AddAppender(appender);
        }
        public void Close()
        {
            try
            {
                lock (__lockey__)
                {
                    __textbox = null;
                }
                var hierachy = (Hierarchy)LogManager.GetRepository();
                hierachy.Root.RemoveAppender(this);
            }
            catch
            {
            }
        }
        public void DoAppend(log4net.Core.LoggingEvent loggingEvent)
        {
            try
            {
                if (__textbox == null)

                    return;

                //                 if (loggingEvent.LoggerName.Contains("XXX_YYY"))     // 로그 필터를 위한 기능으로 사용 가능.
                //                     return;

                var time = DateTime.Now;
                string ts = string.Format("{0:d4}-{1:d2}-{2:d2} {3:d2}:{4:d2}:{5:d2}.{6:d3}",
                    time.Year,
                    time.Month,
                    time.Day,
                    time.Hour,
                    time.Minute,
                    time.Second,
                    time.Millisecond);

                string message = string.Format("{0} {1} {2}\r\n", ts, loggingEvent.Level.ToString().ToUpper(), loggingEvent.RenderedMessage);

                lock (__lockey__)
                {
                    if (__textbox == null) return;
                    var del = new Action<string>(s => {
                        __textbox.AppendText(s);
                    });
                    __textbox.Dispatcher.BeginInvoke(del, message);
                }
            }
            catch
            {
            }
        }
    }
}