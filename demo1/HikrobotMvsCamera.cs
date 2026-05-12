using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cognex.VisionPro;
using MvCamCtrl.NET;

namespace demo1
{
    internal sealed class HikrobotMvsCamera : IDisposable
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        private static extern void CopyMemory(IntPtr destination, IntPtr source, int length);

        private readonly object _syncRoot = new object();
        private MyCamera _camera;
        private bool _opened;
        private bool _grabbing;
        private IntPtr _monoBuffer = IntPtr.Zero;
        private uint _monoBufferSize;
        private SelectedDeviceInfo _currentDevice;

        internal sealed class SelectedDeviceInfo
        {
            internal SelectedDeviceInfo(int index, uint transportType, string transportName, string modelName, string serialNumber, MyCamera.MV_CC_DEVICE_INFO nativeInfo)
            {
                Index = index;
                TransportType = transportType;
                TransportName = transportName ?? string.Empty;
                ModelName = modelName ?? string.Empty;
                SerialNumber = serialNumber ?? string.Empty;
                NativeInfo = nativeInfo;
            }

            public int Index { get; private set; }
            public uint TransportType { get; private set; }
            public string TransportName { get; private set; }
            public string ModelName { get; private set; }
            public string SerialNumber { get; private set; }
            internal MyCamera.MV_CC_DEVICE_INFO NativeInfo { get; private set; }

            public string Description
            {
                get
                {
                    string model = string.IsNullOrWhiteSpace(ModelName) ? "UnknownModel" : ModelName.Trim();
                    string serial = string.IsNullOrWhiteSpace(SerialNumber) ? "N/A" : SerialNumber.Trim();
                    return string.Format("[{0}] {1} | {2} | SN={3}", Index, TransportName, model, serial);
                }
            }

            public override string ToString()
            {
                return Description;
            }
        }

        public static void InitializeSdk()
        {
            int ret = MyCamera.MV_CC_Initialize_NET();
            if (ret != MyCamera.MV_OK && ret != MyCamera.MV_E_CALLORDER)
            {
                throw new InvalidOperationException(string.Format("MVS SDK 初始化失败，错误码 0x{0:X8}", ret));
            }
        }

        public static void FinalizeSdk()
        {
            try
            {
                MyCamera.MV_CC_Finalize_NET();
            }
            catch
            {
            }
        }

        public bool IsOpened
        {
            get
            {
                lock (_syncRoot)
                {
                    return _opened;
                }
            }
        }

        public string CurrentDeviceDescription
        {
            get
            {
                lock (_syncRoot)
                {
                    return _currentDevice != null ? _currentDevice.Description : string.Empty;
                }
            }
        }

        public SelectedDeviceInfo EnsureStarted(string preferredModel)
        {
            lock (_syncRoot)
            {
                if (_camera == null)
                {
                    _camera = new MyCamera();
                }

                if (!_opened)
                {
                    SelectedDeviceInfo selected = SelectDevice(preferredModel);
                    OpenSelectedDeviceLocked(selected);
                    _currentDevice = selected;
                }

                if (!_grabbing)
                {
                    StartGrabbingLocked();
                }

                return _currentDevice;
            }
        }

        public bool TryAcquireMonoImage(int timeoutMs, out ICogImage image, out uint frameNumber)
        {
            ICogImage capturedImage = null;
            uint capturedFrameNumber = 0;

            bool success = TryAcquireMonoFrame(
                timeoutMs,
                delegate (IntPtr monoBuffer, int width, int height, uint currentFrameNumber)
                {
                    capturedImage = CreateCogImage(monoBuffer, width, height);
                    capturedFrameNumber = currentFrameNumber;
                });

            image = capturedImage;
            frameNumber = capturedFrameNumber;
            return success;
        }

        public bool TryAcquireMonoFrame(int timeoutMs, Action<IntPtr, int, int, uint> frameHandler)
        {
            if (frameHandler == null)
            {
                throw new ArgumentNullException("frameHandler");
            }

            MyCamera camera;
            lock (_syncRoot)
            {
                if (_camera == null || !_opened || !_grabbing)
                {
                    throw new InvalidOperationException("相机尚未启动。");
                }

                camera = _camera;
            }

            MyCamera.MV_FRAME_OUT frame = new MyCamera.MV_FRAME_OUT();
            int ret = camera.MV_CC_GetImageBuffer_NET(ref frame, timeoutMs);
            if (ret != MyCamera.MV_OK)
            {
                if (IsTimeoutLikeError(ret))
                {
                    return false;
                }

                throw new InvalidOperationException(string.Format("获取图像失败，错误码 0x{0:X8}", ret));
            }

            try
            {
                int width = (int)frame.stFrameInfo.nWidth;
                int height = (int)frame.stFrameInfo.nHeight;
                if (width <= 0 || height <= 0)
                {
                    throw new InvalidOperationException("相机返回的图像尺寸无效。");
                }

                uint currentFrameNumber = frame.stFrameInfo.nFrameNum;
                IntPtr monoPtr;
                if (!IsDirectMono8PixelType(frame.stFrameInfo.enPixelType))
                {
                    monoPtr = ConvertToMono8(camera, ref frame, width, height);
                }
                else
                {
                    CopyFrameToMonoBuffer(frame.pBufAddr, width, height);
                    monoPtr = _monoBuffer;
                }

                frameHandler(monoPtr, width, height, currentFrameNumber);
                return true;
            }
            finally
            {
                try
                {
                    camera.MV_CC_FreeImageBuffer_NET(ref frame);
                }
                catch
                {
                }
            }
        }

        public void Close()
        {
            lock (_syncRoot)
            {
                CleanupOpenDeviceLocked();
                FreeBufferLocked();
                _currentDevice = null;
            }
        }

        public void Dispose()
        {
            Close();
            GC.SuppressFinalize(this);
        }

        private SelectedDeviceInfo SelectDevice(string preferredModel)
        {
            List<SelectedDeviceInfo> devices = EnumerateDevices();
            if (devices.Count == 0)
            {
                throw new InvalidOperationException("未发现任何 MVS 相机。");
            }

            if (!string.IsNullOrWhiteSpace(preferredModel))
            {
                SelectedDeviceInfo preferred = devices.Find(
                    delegate (SelectedDeviceInfo device)
                    {
                        return ContainsIgnoreCase(device.ModelName, preferredModel) ||
                               ContainsIgnoreCase(device.SerialNumber, preferredModel);
                    });

                if (preferred != null)
                {
                    return preferred;
                }
            }

            SelectedDeviceInfo usbDevice = devices.Find(
                delegate (SelectedDeviceInfo device)
                {
                    return string.Equals(device.TransportName, "USB3", StringComparison.OrdinalIgnoreCase);
                });

            return usbDevice ?? devices[0];
        }

        private static List<SelectedDeviceInfo> EnumerateDevices()
        {
            List<SelectedDeviceInfo> devices = new List<SelectedDeviceInfo>();
            MyCamera.MV_CC_DEVICE_INFO_LIST deviceList = new MyCamera.MV_CC_DEVICE_INFO_LIST();
            int ret = MyCamera.MV_CC_EnumDevices_NET(MyCamera.MV_GIGE_DEVICE | MyCamera.MV_USB_DEVICE, ref deviceList);
            if (ret != MyCamera.MV_OK)
            {
                throw new InvalidOperationException(string.Format("枚举相机失败，错误码 0x{0:X8}", ret));
            }

            int count = (int)deviceList.nDeviceNum;
            for (int i = 0; i < count; i++)
            {
                MyCamera.MV_CC_DEVICE_INFO nativeInfo =
                    (MyCamera.MV_CC_DEVICE_INFO)Marshal.PtrToStructure(deviceList.pDeviceInfo[i], typeof(MyCamera.MV_CC_DEVICE_INFO));
                devices.Add(BuildDeviceInfo(i, nativeInfo));
            }

            return devices;
        }

        private static SelectedDeviceInfo BuildDeviceInfo(int index, MyCamera.MV_CC_DEVICE_INFO nativeInfo)
        {
            if (nativeInfo.nTLayerType == MyCamera.MV_GIGE_DEVICE)
            {
                MyCamera.MV_GIGE_DEVICE_INFO_EX gigEInfo =
                    (MyCamera.MV_GIGE_DEVICE_INFO_EX)MyCamera.ByteToStruct(nativeInfo.SpecialInfo.stGigEInfo, typeof(MyCamera.MV_GIGE_DEVICE_INFO_EX));

                return new SelectedDeviceInfo(
                    index,
                    nativeInfo.nTLayerType,
                    "GigE",
                    CleanString(gigEInfo.chModelName),
                    CleanString(gigEInfo.chSerialNumber),
                    nativeInfo);
            }

            if (nativeInfo.nTLayerType == MyCamera.MV_USB_DEVICE)
            {
                MyCamera.MV_USB3_DEVICE_INFO_EX usbInfo =
                    (MyCamera.MV_USB3_DEVICE_INFO_EX)MyCamera.ByteToStruct(nativeInfo.SpecialInfo.stUsb3VInfo, typeof(MyCamera.MV_USB3_DEVICE_INFO_EX));

                return new SelectedDeviceInfo(
                    index,
                    nativeInfo.nTLayerType,
                    "USB3",
                    CleanString(usbInfo.chModelName),
                    CleanString(usbInfo.chSerialNumber),
                    nativeInfo);
            }

            return new SelectedDeviceInfo(
                index,
                nativeInfo.nTLayerType,
                GetTransportName(nativeInfo.nTLayerType),
                string.Empty,
                string.Empty,
                nativeInfo);
        }

        private void OpenSelectedDeviceLocked(SelectedDeviceInfo selected)
        {
            MyCamera.MV_CC_DEVICE_INFO nativeInfo = selected.NativeInfo;
            int ret = _camera.MV_CC_CreateDevice_NET(ref nativeInfo);
            if (ret != MyCamera.MV_OK)
            {
                throw new InvalidOperationException(string.Format("创建设备失败，错误码 0x{0:X8}", ret));
            }

            try
            {
                ret = _camera.MV_CC_OpenDevice_NET();
                if (ret != MyCamera.MV_OK)
                {
                    throw new InvalidOperationException(string.Format("打开设备失败，错误码 0x{0:X8}", ret));
                }

                if (selected.TransportType == MyCamera.MV_GIGE_DEVICE)
                {
                    int packetSize = _camera.MV_CC_GetOptimalPacketSize_NET();
                    if (packetSize > 0)
                    {
                        _camera.MV_CC_SetIntValueEx_NET("GevSCPSPacketSize", packetSize);
                    }
                }

                ret = _camera.MV_CC_SetEnumValue_NET("TriggerMode", 0);
                if (ret != MyCamera.MV_OK)
                {
                    throw new InvalidOperationException(string.Format("关闭触发模式失败，错误码 0x{0:X8}", ret));
                }

                try
                {
                    _camera.MV_CC_SetImageNodeNum_NET(3);
                }
                catch
                {
                }

                try
                {
                    _camera.MV_CC_SetGrabStrategy_NET(MyCamera.MV_GRAB_STRATEGY.MV_GrabStrategy_LatestImagesOnly);
                }
                catch
                {
                }

                try
                {
                    _camera.MV_CC_SetOutputQueueSize_NET(1);
                }
                catch
                {
                }

                _opened = true;
            }
            catch
            {
                CleanupOpenDeviceLocked();
                throw;
            }
        }

        private void StartGrabbingLocked()
        {
            int ret = _camera.MV_CC_StartGrabbing_NET();
            if (ret != MyCamera.MV_OK)
            {
                CleanupOpenDeviceLocked();
                throw new InvalidOperationException(string.Format("开启取流失败，错误码 0x{0:X8}", ret));
            }

            _grabbing = true;
        }

        private void CleanupOpenDeviceLocked()
        {
            if (_camera == null)
            {
                _opened = false;
                _grabbing = false;
                return;
            }

            if (_grabbing)
            {
                try
                {
                    _camera.MV_CC_StopGrabbing_NET();
                }
                catch
                {
                }

                _grabbing = false;
            }

            if (_opened)
            {
                try
                {
                    _camera.MV_CC_CloseDevice_NET();
                }
                catch
                {
                }

                try
                {
                    _camera.MV_CC_DestroyDevice_NET();
                }
                catch
                {
                }

                _opened = false;
            }
        }

        private void FreeBufferLocked()
        {
            if (_monoBuffer != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(_monoBuffer);
                _monoBuffer = IntPtr.Zero;
                _monoBufferSize = 0;
            }
        }

        private IntPtr ConvertToMono8(MyCamera camera, ref MyCamera.MV_FRAME_OUT frame, int width, int height)
        {
            EnsureMonoBuffer(width, height);

            MyCamera.MV_PIXEL_CONVERT_PARAM convertParam = new MyCamera.MV_PIXEL_CONVERT_PARAM();
            convertParam.nWidth = (ushort)width;
            convertParam.nHeight = (ushort)height;
            convertParam.pSrcData = frame.pBufAddr;
            convertParam.nSrcDataLen = frame.stFrameInfo.nFrameLen;
            convertParam.enSrcPixelType = frame.stFrameInfo.enPixelType;
            convertParam.enDstPixelType = MyCamera.MvGvspPixelType.PixelType_Gvsp_Mono8;
            convertParam.pDstBuffer = _monoBuffer;
            convertParam.nDstBufferSize = _monoBufferSize;

            int ret = camera.MV_CC_ConvertPixelType_NET(ref convertParam);
            if (ret != MyCamera.MV_OK)
            {
                throw new InvalidOperationException(string.Format("像素格式转换失败，错误码 0x{0:X8}", ret));
            }

            return _monoBuffer;
        }

        private void CopyFrameToMonoBuffer(IntPtr source, int width, int height)
        {
            EnsureMonoBuffer(width, height);
            int length = checked(width * height);
            CopyMemory(_monoBuffer, source, length);
        }

        private void EnsureMonoBuffer(int width, int height)
        {
            uint required = (uint)(width * height);
            if (_monoBuffer != IntPtr.Zero && _monoBufferSize >= required)
            {
                return;
            }

            FreeBufferLocked();
            _monoBuffer = Marshal.AllocHGlobal((int)required);
            _monoBufferSize = required;
        }

        private static ICogImage CreateCogImage(IntPtr buffer, int width, int height)
        {
            CogImage8Grey image = new CogImage8Grey(width, height);
            ICogImage8PixelMemory pixelMemory = image.Get8GreyPixelMemory(
                CogImageDataModeConstants.Write,
                0,
                0,
                width,
                height);

            try
            {
                IntPtr destination = pixelMemory.Scan0;
                int destinationStride = pixelMemory.Stride;
                for (int y = 0; y < height; y++)
                {
                    IntPtr sourceRow = IntPtr.Add(buffer, y * width);
                    IntPtr destinationRow = IntPtr.Add(destination, y * destinationStride);
                    CopyMemory(destinationRow, sourceRow, width);
                }
            }
            finally
            {
                pixelMemory.Dispose();
            }

            return image;
        }

        private static bool IsDirectMono8PixelType(MyCamera.MvGvspPixelType pixelType)
        {
            string name = pixelType.ToString();
            return string.Equals(name, "PixelType_Gvsp_Mono8", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "PixelType_Gvsp_HB_Mono8", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(name, "PixelType_Gvsp_Mono8_Signed", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsTimeoutLikeError(int ret)
        {
            return ret == MyCamera.MV_E_NODATA ||
                   ret == MyCamera.MV_E_GC_TIMEOUT ||
                   ret == MyCamera.MV_E_BUSY;
        }

        private static string GetTransportName(uint transportType)
        {
            if (transportType == MyCamera.MV_GIGE_DEVICE)
            {
                return "GigE";
            }

            if (transportType == MyCamera.MV_USB_DEVICE)
            {
                return "USB3";
            }

            return string.Format("0x{0:X}", transportType);
        }

        private static string CleanString(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim('\0', ' ', '\t');
        }

        private static bool ContainsIgnoreCase(string source, string value)
        {
            if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            return source.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
