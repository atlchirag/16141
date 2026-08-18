using System;
using System.Text;
using SuperSocket.SocketBase.Protocol;
using System.Threading;

namespace GPSTrackerListeners.ORSAC
{
    public class CustomReceiveFilter : IReceiveFilter<StringRequestInfo>
    {
        public StringRequestInfo Filter(byte[] readBuffer, int offset, int length, bool toBeCopied, out int rest)
         {

            string source = Encoding.ASCII.GetString(readBuffer, offset, length);
            source = source.Trim();
            string str = Encoding.UTF8.GetString(readBuffer, 0, length);


            rest = 0;

            char[] chars = source.ToCharArray();
            if (chars[0] == '$')
            {
                string[] data1 = source.Split('$');
                if (char.IsDigit(chars[1]) && char.IsDigit(chars[2]))
                    return new StringRequestInfo("response", source, data1);
            }

            if (source.Contains("EPB"))
            {
                string[] data1 = source.Split(new string[] { "EPB" }, StringSplitOptions.None);
                return new StringRequestInfo("EPB", source, data1);
            }

            string[] data = source.Split('$');

            if (source.Contains("SEC"))
            {

                string[] data1 = source.Split(new string[] { "SEC" }, StringSplitOptions.None);

                return new StringRequestInfo("", source, data1);
            }


            if (source.Contains("ATL"))
            {

                string[] data1 = source.Split(new string[] { "ATL" }, StringSplitOptions.None);

                return new StringRequestInfo("", source, data1);
            }

            if (source.Contains("ASPL"))
            {

                string[] data1 = source.Split(new string[] { "ASPL" }, StringSplitOptions.None);

                return new StringRequestInfo("", source, data1);
            }
            //if (source.Contains("Response"))
            //{
            //    return new StringRequestInfo("", source, data);
            //}
            //if (source.Contains("ATLOBD"))
            //{
            //    string[] data1 = source.Split(new string[] { "$ATLOBD" }, StringSplitOptions.None);
            //    return new StringRequestInfo("", source, data1);
            //}


            //if (source.Contains("ATLDTC"))
            //{
            //    string[] data1 = source.Split(new string[] { "ATLDTC" }, StringSplitOptions.None);

            //    return new StringRequestInfo("", source, data1);
            //}

            return new StringRequestInfo("", source, data);
        }

        public int LeftBufferSize { get; private set; }
        public IReceiveFilter<StringRequestInfo> NextReceiveFilter { get; private set; }
        public FilterState State { get; private set; }

        public void Reset()
        {

        }


    }
}
