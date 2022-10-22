using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UI
{
    class SensorDecoder
    {
        public class SensorData
        {
            public enum CabType { PowerCab, ControlCab }
            public CabType Cab;
            public bool Leak;
            public bool Online;
            public double Temperature;
            public double Pressure;
            public double Humidity;
            // 加速度
            public double Ax;
            public double Ay;
            public double Az;
            // 角速度
            public double Wx;
            public double Wy;
            public double Wz;
            // 角度
            public double Roll;
            public double Pitch;
            public double Yaw;
            // 磁场
            public double Hx;
            public double Hy;
            public double Hz;
            // 声呐
            public double Height;
            // 声呐确信度
            public double SonarAccurancy;

            public double WaterTemp;
            public double WaterDepth;

            public override string ToString()
            {
                return string.Format("舱体类型：{0}； 漏水：{1}； 异常：{2}； 温度：{3}； 气压：{4}； 湿度：{5}； 加速度：{6} {7} {8}； 角速度：{9} {10} {11}； 角度：{12} {13} {14}； 磁场：{15} {16} {17}； 声呐：{18}； 声呐确信度：{19}； 水温：{20}； 水深：{21}",
                    Cab, Leak, Online, Temperature, Pressure, Humidity, Ax, Ay, Az, Wx, Wy, Wz, Roll, Pitch, Yaw, Hx, Hy, Hz, Height, SonarAccurancy, WaterTemp, WaterDepth);
            }
        }

        public static (int, int) Detect(byte[] data)
        {
            var buffer = BitConverter.ToString(data).Replace("-", "");
            var index = buffer.IndexOf("25", 0);
            if (index == -1)
            {
                return (0, 0);
            }
            var command = buffer.Substring(index, 94);
            if (!command.EndsWith("FFFF"))
            {
                return (0, 0);
            }
            return (index / 2, index / 2 + 47);
        }

        public static (SensorData, int, int) Decode(byte[] data)
        {
            var buffer = BitConverter.ToString(data).Replace("-", "");
            var index = buffer.IndexOf("25", 0);
            if (index == -1)
            {
                return (null, 0, 0);
            }
            var command = buffer.Substring(index, 94);
            if (!command.EndsWith("FFFF"))
            {
                return (null, 0, 0);
            }

            var sensor = new SensorData();

            var flag = Convert.ToInt16(command.Substring(2, 2));

            sensor.Cab = (flag & 0x0001) == 1 ? SensorData.CabType.ControlCab : SensorData.CabType.PowerCab;
            sensor.Leak = (flag & 0x0002) == 1;
            sensor.Online = (flag & 0x0004) == 1;

            var temp = command.Substring(4, 4);
            sensor.Temperature = Convert.ToInt16(temp, 16) / 100.0; // C

            var press = command.Substring(8, 8);
            sensor.Pressure = Convert.ToInt32(press, 16) / 100.0 / 1000.0; // kPa

            var humid = command.Substring(16, 4);
            sensor.Humidity = Convert.ToInt16(humid, 16) / 100.0; // %

            // 加速度
            var ax = command.Substring(20, 4);
            sensor.Ax = Convert.ToInt16(ax, 16) * 156.8 / 32768.0;

            var ay = command.Substring(24, 4);
            sensor.Ay = Convert.ToInt16(ay, 16) * 156.8 / 32768.0;

            var az = command.Substring(28, 4);
            sensor.Az = Convert.ToInt16(az, 16) * 156.8 / 32768.0;

            // 角速度
            var wx = command.Substring(32, 4);
            sensor.Wx = Convert.ToInt16(wx, 16) * 2000.0 / 32768.0;

            var wy = command.Substring(36, 4);
            sensor.Wy = Convert.ToInt16(wy, 16) * 2000.0 / 32768.0;

            var wz = command.Substring(40, 4);
            sensor.Wz = Convert.ToInt16(wz, 16) * 2000.0 / 32768.0;

            // 角度
            var roll = command.Substring(44, 4);
            sensor.Roll = Convert.ToUInt16(roll, 16) * 180.0 / 32768.0;

            var pitch = command.Substring(48, 4);
            sensor.Pitch = Convert.ToUInt16(pitch, 16) * 180.0 / 32768.0;

            var yaw = command.Substring(52, 4);
            sensor.Yaw = Convert.ToInt16(yaw, 16) * 180.0 / 32768.0;

            // 磁场
            var hx = command.Substring(56, 4);
            sensor.Hx = Convert.ToInt16(hx, 16) * 4912.0 / 32768.0;

            var hy = command.Substring(60, 4);
            sensor.Hy = Convert.ToInt16(hy, 16) * 4912.0 / 32768.0;

            var hz = command.Substring(64, 4);
            sensor.Hz = Convert.ToInt16(hz, 16) * 4912.0 / 32768.0;

            // 声呐
            var height = command.Substring(68, 8);
            sensor.Height = Convert.ToInt32(height, 16) / 1000.0;

            // 声呐确信度
            var sonarAccurancy = command.Substring(76, 4);
            sensor.SonarAccurancy = Convert.ToInt16(sonarAccurancy, 16) / 100.0;

            // 水温
            var waterTemp = command.Substring(80, 4);
            sensor.WaterTemp = Convert.ToInt16(waterTemp, 16) / 100.0;

            // 水深
            var waterDepth = command.Substring(84, 4);
            sensor.WaterDepth = Convert.ToInt16(waterDepth, 16) / 1000.0;

            return (sensor, index / 2, (index / 2) + 47);
        }
    }
}
