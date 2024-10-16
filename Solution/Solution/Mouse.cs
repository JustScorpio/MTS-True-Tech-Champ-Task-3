using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Solution
{
    public class Mouse
    {
        public const double Width = 65;
        public const double Length = 69;
        public const double Height = 26.5;

        public const int sensorListenerInterval = 10;

        HttpClient client;
        string token;

        public SensorData sensorData;
        //double speed;

        public Mouse(HttpClient _client, string _token)
        {
            client = _client;
            token = _token;

            UpdateSensorData();

            //new Thread(RunDataListener).Start();
        }

        public void RunDataListener()
        {
            while (true)
                UpdateSensorData();
        }

        public void UpdateSensorData()
        {
            //var prevX = sensorData.down_x_offset;
            //var prevY = sensorData.down_y_offset;

            var sensorDataRaw = client.GetAsync("http://127.0.0.1:8801/api/v1/robot-motors/sensor-data?token=" + token).Result.Content.ReadAsStringAsync().Result;
            sensorData = JsonConvert.DeserializeObject<SensorData>(sensorDataRaw);

            //speed = Math.Pow(Math.Pow(prevX - sensorData.down_x_offset, 2) + Math.Pow(prevY - sensorData.down_y_offset, 2), 0.5);
        }

        public int GetDirection()
        {
            var direction = sensorData.rotation_yaw;
            if (-135 <= direction && direction < -45)
                direction = -90;
            else if (-45 <= direction && direction < 45)
                direction = -0;
            else if (45 <= direction && direction < 135)
                direction = 90;
            else
            {
                direction = 180;

                if (direction < 0)
                    direction *= -1;
            }

            return (int)Math.Round(direction);
        }

        public int GetX()
        {
            return (int)Math.Floor(sensorData.down_y_offset / 166.5 + 8);
        }

        public int GetY()
        {
            return (int)Math.Floor(sensorData.down_x_offset / 166.5 * -1 + 8);
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

            var sleepTime = Convert.ToInt32((left_time > right_time ? left_time : right_time) * 1000);
            Thread.Sleep(sleepTime);
        }

        public void Forward()
        {
            var frontDist = sensorData.front_distance;

            var speed = 0.49;

            if (frontDist > 166.5 && frontDist < 333) //ПРИБЛИЗИТЕЛЬНАЯ линейная регрессия
                speed = (frontDist + 64.09) / 557.46;

            //Аппроксимация к длине стены лабиринта
            Move(255, speed, 255, speed);
            Move(-255, 0.35, -255, 0.35);

            UpdateSensorData();
        }

        public void TurnLeft(int times = 1)
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

            var difYam = curYam - targetYaw + 90 * (times - 1);

            var time = (difYam + 8.5) / 98; //ПРИБЛИЗИТЕЛЬНАЯ линейная регрессия

            Move(-255, time, 255, time);

            UpdateSensorData();
        }

        public void TurnRight(int times = 1)
        {
            var curYam = sensorData.rotation_yaw;
            var targetYaw = 0;

            if (-135 <= curYam && curYam < -45)
                targetYaw = 0;
            else if (-45 <= curYam && curYam < 45)
                targetYaw = 90;
            else if (45 <= curYam && curYam < 135)
                targetYaw = 180;
            else
            {
                targetYaw = -90;

                if (curYam > 0)
                    curYam = -360 + curYam;
            }

            var difYam = targetYaw - curYam + 90 * (times - 1);

            var time = (difYam + 8.5) / 98; //ПРИБЛИЗИТЕЛЬНАЯ линейная регрессия

            Move(255, time, -255, time);

            UpdateSensorData();
        }

        public void Explore()
        {
            while (true)
            {
                var x = sensorData.down_y_offset;
                Forward();
                Thread.Sleep(2000);
                Console.SetCursorPosition(0, 5);
                Console.WriteLine(x - sensorData.down_y_offset);
                TurnLeft(2);
            }
        }
    }
}
