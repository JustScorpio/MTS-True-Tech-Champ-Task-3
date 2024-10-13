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

        public Mouse(HttpClient _client, string _token)
        {
            client = _client;
            token = _token;

            UpdateSensorData();
        }

        public void UpdateSensorData()
        {
            var sensorDataRaw = client.GetAsync("http://127.0.0.1:8801/api/v1/robot-motors/sensor-data?token=" + token).Result.Content.ReadAsStringAsync().Result;
            sensorData = JsonConvert.DeserializeObject<SensorData>(sensorDataRaw);
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
        }

        public void Explore()
        {
            while (true)
            {
                UpdateSensorData();

                if (sensorData.left_45_distance > 80)
                {
                    Move(-255, 0.3, 255, 0.3);
                }
                else
                {
                    Move(255, 0.3, -255, 0.3);
                }

                if (sensorData.front_distance > 50)
                    Move(50, 0.3, 50, 0.3);
                //else if (sensorData.front_distance < 20)
                //    Move(-255, 0.3, 250, 0.3);

            }
        }
    }
}
