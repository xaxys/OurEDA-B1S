using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI
{
    class OperationDecoder
    {
        public static (int, int) Detect(byte[] data)
        {
            var buffer = BitConverter.ToString(data).Replace("-", "");
            var index = buffer.IndexOf("25", 0);
            if (index == -1)
            {
                return (0, 0);
            }
            var tem_ttt = buffer.Substring(index, 60);
            if (!tem_ttt.EndsWith("21"))
            {
                return (0, 0);
            }
            return (index / 2, index / 2 + 30);
        }

        public static byte[] Decode(byte[] data)
        {
            // 最终还是扔掉了
            // 也不知道干嘛的神必代码，好像啥也没动，编码解码编码一遍原封不动返回去了，但是咱也不敢动，能跑就先这样吧
            // var buffer = BitConverter.ToString(data).Replace("-", "");
            // var index = buffer.IndexOf("25", 0);
            // var tem_ttt = buffer.Substring(index, 60);
            // byte[] buft = new byte[30];
            // for (int j1 = 0, j2 = 0; j2 <= tem_ttt.Length - 2; j1++)
            // {
            //     buft[j1] = Convert.ToByte(tem_ttt.Substring(j2, 2), 16);
            //     j2 = j2 + 2;
            // }
            // return buft;
            return data;
        }
    }
}
