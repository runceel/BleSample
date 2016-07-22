using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Windows;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Storage.Streams;

namespace BleSample
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private GattDeviceService GattDeviceService { get; set; }

        private GattCharacteristic GattCharacteristic { get; set; }

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            // SensorTagを取得
            var selector = GattDeviceService.GetDeviceSelectorFromUuid(new Guid(SensorTagUuid.UuidIrtService));
            var devices = await DeviceInformation.FindAllAsync(selector);
            var deviceInformation = devices.FirstOrDefault();
            if (deviceInformation == null)
            {
                MessageBox.Show("not found");
                return;
            }

            this.GattDeviceService = await GattDeviceService.FromIdAsync(deviceInformation.Id);
            MessageBox.Show($"found {deviceInformation.Id}");

            // センサーの有効化?
            var configCharacteristic = this.GattDeviceService.GetCharacteristics(new Guid(SensorTagUuid.UuidIrtConf)).First();
            var status = await configCharacteristic.WriteValueAsync(new byte[] { 1 }.AsBuffer());
            if (status == GattCommunicationStatus.Unreachable)
            {
                MessageBox.Show("Initialize failed");
                return;
            }
        }

        private async void ButtonReadValue_Click(object sender, RoutedEventArgs e)
        {
            if (this.GattDeviceService == null)
            {
                MessageBox.Show("Please click connect button");
                return;
            }

            // 値を読み始める
            if (this.GattCharacteristic == null)
            {
                this.GattCharacteristic = this.GattDeviceService.GetCharacteristics(new Guid(SensorTagUuid.UuidIrtData)).First();
                this.GattCharacteristic.ValueChanged += this.GattCharacteristic_ValueChanged;

                var status = await this.GattCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status == GattCommunicationStatus.Unreachable)
                {
                    MessageBox.Show("Failed");
                }
            }
        }

        private async void GattCharacteristic_ValueChanged(GattCharacteristic sender, GattValueChangedEventArgs args)
        {
            // 値を読んで表示する
            await this.Dispatcher.InvokeAsync(() =>
            {
                var data = new byte[args.CharacteristicValue.Length];
                DataReader.FromBuffer(args.CharacteristicValue).ReadBytes(data);
                var temp = BitConverter.ToUInt16(data, 2) / 128.0;
                this.TextBlockTemp.Text = $"{temp}℃";
            });
        }
    }
}
