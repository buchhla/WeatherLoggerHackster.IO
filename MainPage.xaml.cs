// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Media.SpeechSynthesis;
using Windows.UI.Core;


namespace WeatherLogger
{
    public sealed partial class MainPage : Page
    {
        private const int LED_PIN = 12;
        private GpioPin pin;
        private GpioPinValue pinValue;
        private DispatcherTimer LoggerTimer;
        private DispatcherTimer Timer;
        //A class which wraps the color sensor
        TCS34725 colorSensor;
        //A SpeechSynthesizer class for text to speech operations
        SpeechSynthesizer synthesizer;
        //A MediaElement class for playing the audio
        MediaElement audio;
        //A class which wraps the barometric sensor
        BMP280 BMP280;
        //A GPIO pin for the pushbutton
        GpioPin buttonPin;
        //The GPIO pin number we want to use to control the pushbutton
        int gpioPin = 4;

        private const string I2C_CONTROLLER_NAME = "I2C1"; //use for RPI2
        private const byte LCD_I2C_ADDRESS = 0x3F; // 7-bit I2C address of the port expander

        //Setup pins
        private const byte EN = 0x02;
        private const byte RW = 0x01;
        private const byte RS = 0x00;
        private const byte D4 = 0x04;
        private const byte D5 = 0x05;
        private const byte D6 = 0x06;
        private const byte D7 = 0x07;
        private const byte BL = 0x03;
        displayI2C lcd = new displayI2C(LCD_I2C_ADDRESS, I2C_CONTROLLER_NAME, RS, RW, EN, D4, D5, D6, D7, BL);

        public MainPage()
        {
            InitializeComponent();

            lcd.init();

            LoggerTimer = new DispatcherTimer();
            LoggerTimer.Interval = TimeSpan.FromMinutes(1);
            LoggerTimer.Tick += LoggerTimer_Tick;

            Timer = new DispatcherTimer();
            Timer.Interval = TimeSpan.FromSeconds(1);
            Timer.Tick += Timer_Tick;

            InitGPIO();

            //Create a new SpeechSynthesizer
            synthesizer = new SpeechSynthesizer();

            //Create a new MediaElement
            audio = new MediaElement();
            

            lcd.turnOnBacklight();
            lcd.clrscr();
            lcd.gotoxy(0, 0);
            lcd.prints("SV Nokomis Pi Logger");
            //StopwatchDelay.Delay(250);

            if (pin != null)
            {
                LoggerTimer_Tick(this, null);
                LoggerTimer.Start();
                //Timer_Tick(this, null);
                //Timer.Start();
            }
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            // Show an error if there is no GPIO controller
            if (gpio == null)
            {
                pin = null;
                //GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            pin = gpio.OpenPin(LED_PIN);
            pinValue = GpioPinValue.High;
            pin.Write(pinValue);
            pin.SetDriveMode(GpioPinDriveMode.Output);

            //GpioStatus.Text = "GPIO pin initialized correctly.";

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

        private async Task<LightData> ReadLightData()
        {
            LightData ld = null;
            try
            {
                if (colorSensor == null)
                {
                    colorSensor = new TCS34725();
                    await colorSensor.Initialize();
                }

                //Read the approximate color from the sensor
                string colorRead = await colorSensor.getClosestColor();
                //Output the colr name to the speaker
                await Speak(" The current color is: " + colorRead);
                RgbData rgb = await colorSensor.getRgbData();

                float lux = TCS34725.getLuxSimple(rgb);
                ld = new LightData();
                ld.Created = DateTime.Now;
                ld.rgbData = rgb;
                ld.Lux = lux;
                ld.ColorTempinK = TCS34725.calculateColorTemperature(rgb);
                Debug.WriteLine("Current lux: " + lux);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
            return ld;
        }
        //This method is used to output a string to the speaker
        private async Task Speak(string textToSpeak)
        {
            //Create a SpeechSynthesisStream using a string
            var stream = await synthesizer.SynthesizeTextToStreamAsync(textToSpeak);
            //Use a dispatcher to play the audio
            var ignored = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                //Set the souce of the MediaElement to the SpeechSynthesisStream
                audio.SetSource(stream, stream.ContentType);
                //Play the stream
                audio.Play();
            });
        }

        private void Timer_Tick(object sender, object e)
        {
            lcd.gotoxy(10, 3);
            lcd.prints("Time:" + DateTime.Now.ToString("hh:mm"));
        }


        private void LoggerTimer_Tick(object sender, object e)
        {
            if (pinValue == GpioPinValue.High)
            {
                pinValue = GpioPinValue.Low;
                pin.Write(pinValue);
                ReadWeatherData().ContinueWith((t) =>
                {
                    WeatherData wd = t.Result;
                    AdafruitIO io = new AdafruitIO();
                    io.sendData(wd);
                    Debug.WriteLine(wd.Created);
                    //Write the values to your debug console
                    Debug.WriteLine("Created: " + wd.Created + " ft");
                    txtTime.Text = "Created: " + wd.Created + " ft";
                    Debug.WriteLine("Temperature: " + wd.TemperatureinF + " deg F");
                    txtTemp.Text = "Temperature: " + wd.TemperatureinF + " deg F";
                    Debug.WriteLine("Pressure: " + wd.Pressureinmb + " mb");
                    txtPressure.Text = "Pressure: " + wd.Pressureinmb + " mb";
                    // string json = JsonConvert.SerializeObject(wd);
                    lcd.gotoxy(0, 1);
                    lcd.prints("Temperature:" + Math.Round(wd.TemperatureinF,2));
                    lcd.gotoxy(0, 2);
                    lcd.prints("Pressure: " + Math.Round(wd.Pressureinmb,2));
                    Debug.WriteLine("");

                }, TaskScheduler.FromCurrentSynchronizationContext());
                ReadLightData().ContinueWith((t) =>
                {
                    LightData ld = t.Result;

                    //AdafruitIO io = new AdafruitIO();
                    //io.sendData(ld);
                    Debug.WriteLine("Lux: " + ld.Lux);
                    Debug.WriteLine("Color Temp:" + ld.ColorTempinK + " K");
                    // string json = JsonConvert.SerializeObject(wd);
                    lcd.gotoxy(0, 3);
                    lcd.prints("Lux:" + Math.Round(ld.Lux,2));

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

        private void InitializeButtonGpio()
        {
            //Create a default GPIO controller
            GpioController gpioController = GpioController.GetDefault();
            //Use the controller to open the gpio pin of given number
            buttonPin = gpioController.OpenPin(gpioPin);
            //Debounce the pin to prevent unwanted button pressed events
            buttonPin.DebounceTimeout = new TimeSpan(1000);
            //Set the pin for input
            buttonPin.SetDriveMode(GpioPinDriveMode.Input);
            //Set a function callback in the event of a value change
            buttonPin.ValueChanged += LoggerTimer_Tick;
        }

    }
}

