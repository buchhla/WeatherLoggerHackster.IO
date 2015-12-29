// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Media;
using System.Net.Http;
using System.Net.Http.Headers;


namespace WeatherLogger
{
    public sealed partial class MainPage : Page
    {
        private const int LED_PIN = 12;
        private GpioPin pin;
        private GpioPinValue pinValue;
        private DispatcherTimer timer;
        private SolidColorBrush redBrush = new SolidColorBrush(Windows.UI.Colors.Red);
        private SolidColorBrush grayBrush = new SolidColorBrush(Windows.UI.Colors.LightGray);

        //A class which wraps the barometric sensor
        BMP280 BMP280;


        public MainPage()
        {
            InitializeComponent();

            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMinutes(1);
            timer.Tick += Timer_Tick;
            InitGPIO();
            if (pin != null)
            {
                Timer_Tick(this,null);
                timer.Start();
            }        
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin = null;
                GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            pin = gpio.OpenPin(LED_PIN);
            pinValue = GpioPinValue.High;
            pin.Write(pinValue);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            GpioStatus.Text = "GPIO pin initialized correctly.";

        }

   private async Task<WeatherData> ReadWeatherData()
        {
            WeatherData wd = null;
            try
            {
                if (BMP280 == null)
                {
                    //Create a new object for our barometric sensor class
                    BMP280 = new BMP280();
                    //Initialize the sensor
                    await BMP280.Initialize();
                }

                //Create variables to store the sensor data: temperature, pressure and altitude. 
                //Initialize them to 0.
                float temp = 0;
                float pressure = 0;
                float altitude = 0;

                //Create a constant for pressure at sea level. 
                //This is based on your local sea level pressure (Unit: Hectopascal)
                const float seaLevelPressure = 1018.34f;

                temp = await BMP280.ReadTemperature();
                temp = ConvertUnits.ConvertCelsiusToFahrenheit(temp);
                pressure = await BMP280.ReadPreasure();
                pressure = ConvertUnits.ConvertPascalToMillibar(pressure);
                altitude = await BMP280.ReadAltitude(seaLevelPressure);
                altitude = ConvertUnits.ConvertMeterToFoot(altitude);

                wd = new WeatherData();
                wd.Created = DateTime.Now;
                wd.TemperatureinF = temp;
                wd.Pressureinmb = pressure;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return wd;
        }




        private void Timer_Tick(object sender, object e)
        {
            if (pinValue == GpioPinValue.High)
            {
                pinValue = GpioPinValue.Low;
                pin.Write(pinValue);
                ReadWeatherData().ContinueWith((t) =>
                {
                    WeatherData wd = t.Result;
                    Debug.WriteLine(wd.Created);
                    //Write the values to your debug console
                    Debug.WriteLine("Created: " + wd.Created + " ft");
                    txtTime.Text = "Created: " + wd.Created + " ft";
                    Debug.WriteLine("Temperature: " + wd.TemperatureinF + " deg F");
                    txtTemp.Text = "Temperature: " + wd.TemperatureinF + " deg F";
                    Debug.WriteLine("Pressure: " + wd.Pressureinmb + " mb");
                    txtPressure.Text = "Pressure: " + wd.Pressureinmb + " mb";
                   // string json = JsonConvert.SerializeObject(wd);

                    Debug.WriteLine("");
                    AdafruitIO io = new AdafruitIO();
                    io.sendData(wd);
                }, TaskScheduler.FromCurrentSynchronizationContext());

                pinValue = GpioPinValue.High;
                pin.Write(pinValue);
            }
            else
            {
                pinValue = GpioPinValue.High;
                pin.Write(pinValue);
            }
        }
             

    }
}
