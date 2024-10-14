using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solution
{
    internal class Mouse
    {
        public const double Width = 65;
        public const double Length = 69;
        public const double Height = 26.5;

        public const int sensorListenerInterval = 10;

        HttpClient client;
        string token;

        SensorData sensorData;
        double speed;

        public Mouse(HttpClient _client, string _token)
        {
            client = _client;
            token = _token;

            var sensorDataRaw = client.GetAsync("http://127.0.0.1:8801/api/v1/robot-motors/sensor-data?token=" + token).Result.Content.ReadAsStringAsync().Result;
            sensorData = JsonConvert.DeserializeObject<SensorData>(sensorDataRaw);

            new Thread(RunDataListener).Start();
        }

        public void RunDataListener()
        {
            while (true)
                UpdateSensorData();
        }

        public void UpdateSensorData()
        {
            var prevX = sensorData.down_x_offset;
            var prevY = sensorData.down_y_offset;

            var sensorDataRaw = client.GetAsync("http://127.0.0.1:8801/api/v1/robot-motors/sensor-data?token=" + token).Result.Content.ReadAsStringAsync().Result;
            sensorData = JsonConvert.DeserializeObject<SensorData>(sensorDataRaw);

            speed = Math.Pow(Math.Pow(prevX - sensorData.down_x_offset, 2) + Math.Pow(prevY - sensorData.down_y_offset, 2), 0.5);

            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"Speed: {speed}");
            Console.WriteLine($"Direction: {sensorData.rotation_yaw}");
        }

        /// <summary>
        /// Отдать команду на движение
        /// </summary>
        /// <param name="left">Интенсивность работы левого двигателе. Диапозон от-255 до 255</param>
        /// <param name="left_time">Время работы левого двигателя в секундах. Точность - два знака после запятой</param>
        /// <param name="right">Интенсивность работы правого двигателе. Диапозон от-255 до 255</param>
        /// <param name="right_time">Время работы правого двигателя в секундах. Точность - два знака после запятой</param>
        public void Move(int left, double left_time, int right, double right_time)
        {
            var left_time_fixed = left_time.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var right_time_fixed = right_time.ToString(System.Globalization.CultureInfo.InvariantCulture);

            var request = client.PostAsync($"http://127.0.0.1:8801/api/v1/robot-motors/move?l={left}&l_time={left_time_fixed}&r={right}&r_time={right_time_fixed}&token={token}", null).Result;
            
            var sleepTime = Convert.ToInt32((left_time > right_time? left_time : right_time) * 1000);
            Thread.Sleep(sleepTime);
        }

        public void Stop()
        {
            Move(0, 1, 0, 1);
        }

        public void Forward()
        {
            Move(150, 0.1, 150, 0.1);
        }

        public void TurnLeft()
        {
            var curYam = sensorData.rotation_yaw;
            var targetYaw = 0;

            if (-135 <= curYam && curYam < -45)
                targetYaw = -180;
            else if (-45 <= curYam && curYam < 45)
                targetYaw = -90;
            else if (45 <= curYam && curYam < 135)
                targetYaw = 0;
            else
            {
                targetYaw = 90;

                if (curYam < 0)
                    curYam = 360 + curYam;
            }

            var difYam = curYam - targetYaw;

            var time = (difYam + 8.5) / 96; //ПРИБЛИЗИТЕЛЬНАЯ линейная регрессия

            Move(-255, time, 255, time);
            Stop();
        }

        public void Explore()
        {
            //while(true)
            //{
            //    Move(-255, 0.5, 255, 0.5);
            //    Thread.Sleep(5000);
            //}

            //while (true)
            //{
            //    TurnLeft();
            //    Thread.Sleep(2000);
            //}
            //return;

            TurnLeft();
            TurnLeft();
            TurnLeft();

            while (sensorData.front_distance > 100)
                if (speed < 15)
                    Forward();

            Stop();

            TurnLeft();

            while (sensorData.front_distance > 100)
                if (speed < 15)
                    Forward();

            Stop();

            TurnLeft();
            TurnLeft();
            TurnLeft();

            while (sensorData.front_distance > 100)
                if (speed < 15)
                    Forward();

            //while (true)
            //{
            //    if (sensorData.left_45_distance > 100)
            //    {
            //        Move(-255, 0.3, 255, 0.3);
            //    }
            //    else
            //    {
            //        Move(255, 0.3, -255, 0.3);
            //    }

            //    if (sensorData.front_distance > 50)
            //        Move(50, 0.3, 50, 0.3);
            //    //else if (sensorData.front_distance < 20)
            //    //    Move(-255, 0.3, 250, 0.3);

            //}
        }
    }
}
