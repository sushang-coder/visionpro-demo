using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Cognex.VisionPro;
using Cognex.VisionPro.Display;
using Cognex.VisionPro.QuickBuild;
using Cognex.VisionPro.ToolGroup;

namespace demo1
{
    public partial class Form1 : Form
    {
        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory", SetLastError = false)]
        private static extern void CopyMemory(IntPtr destination, IntPtr source, int length);

        private const string JobFileName = "02.vpp";
        private const string ImageSourceToolName = "Image Source";
        private const string ToolBlockName = "CogToolBlock1";
        private const string OutputImageRecordKey = "OutputImage";
        private const string PreferredCameraModel = "MV-CE060-10UC";
        private const int PreviewGrabTimeoutMs = 200;
        private const int PreviewFrameIntervalMs = 120;
        private const int RunGrabTimeoutMs = 1000;
        private const int StopWaitTimeoutMs = 3000;

        private readonly HikrobotMvsCamera _camera = new HikrobotMvsCamera();
        private readonly object _previewBufferSync = new object();

        private CogJobManager _jobManager;
        private CogToolGroup _toolGroup;
        private object _imageSourceTool;
        private PropertyInfo _inputImageProperty;

        private Task _singleRunTask;
        private CancellationTokenSource _previewCts;
        private Task _previewTask;
        private TaskCompletionSource<bool> _previewStopSignal;
        private int _previewSessionId;
        private volatile bool _previewFramePending;
        private CogImage8Grey _previewBufferA;
        private CogImage8Grey _previewBufferB;
        private CogImage8Grey _previewDisplayedBuffer;

        private CancellationTokenSource _continuousCts;
        private Task _continuousTask;
        private int _continuousSessionId;

        private int _runCount;
        private string _vppPath = string.Empty;
        private bool _isClosing;
        private bool _sdkInitialized;
        private bool _jobLoaded;

        public Form1()
        {
            InitializeComponent();
            Load += Form1_Load;
            FormClosing += Form1_FormClosing;
            cogDisplayImage.SizeChanged += CogDisplayImage_SizeChanged;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            txtLog.Clear();
            txtStatus.Clear();

            try
            {
                LoadJob();
                _jobLoaded = true;
                AppendLog("已加载 02.vpp: " + _vppPath);
            }
            catch (Exception ex)
            {
                AppendLog("加载 02.vpp 失败: " + ex.Message);
                RefreshStatus("错误", ex.Message);
            }

            try
            {
                HikrobotMvsCamera.InitializeSdk();
                _sdkInitialized = true;
                AppendLog("MVS SDK 初始化成功");
            }
            catch (Exception ex)
            {
                AppendLog("MVS SDK 初始化失败: " + ex.Message);
                RefreshStatus("错误", ex.Message);
            }

            ConfigureDisplay();
            SetButtonsEnabled(_jobLoaded && _sdkInitialized);

            if (_jobLoaded && _sdkInitialized)
            {
                RefreshStatus("空闲", "已就绪，点击“显示图像”启动相机");
            }
        }

        private async void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_isClosing)
            {
                return;
            }

            e.Cancel = true;
            _isClosing = true;

            try
            {
                ClearDisplay();
            }
            catch
            {
            }

            try
            {
                await StopAllAcquisitionAsync(true, true);
            }
            catch
            {
            }

            Environment.Exit(0);
        }

        private async void btnDisplayLive_Click(object sender, EventArgs e)
        {
            await StartLivePreviewAsync();
        }

        private async void btnCloseCamera_Click(object sender, EventArgs e)
        {
            await CloseCameraAsync();
        }

        private async void btnSingleRun_Click(object sender, EventArgs e)
        {
            await RunSingleOnceAsync();
        }

        private async void btnContinuousRun_Click(object sender, EventArgs e)
        {
            await StartContinuousRunAsync();
        }

        private void LoadJob()
        {
            _vppPath = ResolveVppPath();
            object loaded = CogSerializer.LoadObjectFromFile(_vppPath);
            _jobManager = loaded as CogJobManager;
            if (_jobManager == null)
            {
                throw new InvalidOperationException("02.vpp 不是 CogJobManager。");
            }

            if (_jobManager.JobCount < 1)
            {
                throw new InvalidOperationException("02.vpp 中没有可用作业。");
            }

            CogJob job = _jobManager.Job(0);
            _toolGroup = job.VisionTool as CogToolGroup;
            if (_toolGroup == null)
            {
                throw new InvalidOperationException("作业的 VisionTool 不是 CogToolGroup。");
            }

            _imageSourceTool = FindTool(_toolGroup.Tools, ImageSourceToolName);
            if (_imageSourceTool == null)
            {
                throw new InvalidOperationException("未找到名为 \"Image Source\" 的工具。");
            }

            _inputImageProperty = _imageSourceTool.GetType().GetProperty(
                "InputImage",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (_inputImageProperty == null)
            {
                throw new InvalidOperationException("未找到 Image Source 的 InputImage 属性。");
            }

            _runCount = 0;
        }

        private string ResolveVppPath()
        {
            string[] candidates =
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, JobFileName),
                Path.Combine(Application.StartupPath, JobFileName),
                Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\..\02.vpp"))
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                if (File.Exists(candidates[i]))
                {
                    return candidates[i];
                }
            }

            throw new FileNotFoundException("找不到 02.vpp。", candidates[0]);
        }

        private ICogTool FindTool(CogToolCollection tools, string name)
        {
            for (int i = 0; i < tools.Count; i++)
            {
                ICogTool tool = tools[i];
                if (tool != null && string.Equals(tool.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    return tool;
                }
            }

            return null;
        }

        private ICogTool FindToolBlockTool(CogToolCollection tools)
        {
            ICogTool fallback = null;

            for (int i = 0; i < tools.Count; i++)
            {
                ICogTool tool = tools[i];
                if (tool == null)
                {
                    continue;
                }

                if (string.Equals(tool.Name, ToolBlockName, StringComparison.OrdinalIgnoreCase))
                {
                    return tool;
                }

                if (fallback == null &&
                    tool.GetType().Name.IndexOf("CogToolBlock", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    fallback = tool;
                }
            }

            return fallback;
        }

        private bool EnsureJobReady()
        {
            if (_toolGroup == null || _imageSourceTool == null || _inputImageProperty == null || FindToolBlockTool(_toolGroup.Tools) == null)
            {
                AppendLog("02.vpp 尚未就绪。");
                RefreshStatus("未就绪", "请先完成 VPP 加载");
                return false;
            }

            return true;
        }

        private void EnsureCameraReady()
        {
            HikrobotMvsCamera.SelectedDeviceInfo device = _camera.EnsureStarted(PreferredCameraModel);
            AppendLog("相机就绪: " + device.Description);
        }

        private void SetButtonsEnabled(bool enabled)
        {
            btnDisplayLive.Enabled = enabled;
            btnCloseCamera.Enabled = enabled;
            btnSingleRun.Enabled = enabled;
            btnContinuousRun.Enabled = enabled;
        }

        private void ConfigureDisplay()
        {
            if (!IsHandleCreated || IsDisposed)
            {
                return;
            }

            cogDisplayImage.BackColor = System.Drawing.Color.Black;
            cogDisplayImage.AutoFit = false;
            cogDisplayImage.AutoFitWithGraphics = false;
            cogDisplayImage.MaintainImageRegion = false;
            cogDisplayImage.Dock = DockStyle.Fill;
            cogDisplayImage.MouseMode = CogDisplayMouseModeConstants.Pointer;
            cogDisplayImage.MouseWheelMode = CogDisplayMouseWheelModeConstants.None;
            cogDisplayImage.PopupMenu = false;
            cogDisplayImage.HorizontalScrollBar = false;
            cogDisplayImage.VerticalScrollBar = false;
            cogDisplayImage.ScalingMethod = CogDisplayScalingMethodConstants.ContinuousBilinear;
        }

        private void CogDisplayImage_SizeChanged(object sender, EventArgs e)
        {
            RunOnUiThread(delegate
            {
                if (_isClosing || cogDisplayImage.Image == null)
                {
                    return;
                }

                FillDisplayToWindow(cogDisplayImage.Image);
            });
        }

        private async Task CloseCameraAsync()
        {
            if (_isClosing)
            {
                return;
            }

            await StopAllAcquisitionAsync(true, false);
            ClearDisplay();
            RefreshStatus("空闲", "摄像头已关闭");
            AppendLog("摄像头已关闭。");
        }

        private async Task StartLivePreviewAsync()
        {
            if (!EnsureJobReady())
            {
                return;
            }

            if (_previewTask != null && !_previewTask.IsCompleted)
            {
                AppendLog("实时预览已经在运行中。");
                return;
            }

            if (!await StopSingleRunAsyncFixed())
            {
                return;
            }

            if (!await StopContinuousAsyncFixed())
            {
                return;
            }

            if (!await StopPreviewAsyncFixed())
            {
                return;
            }

            try
            {
                EnsureCameraReady();
                ClearDisplay();
                RefreshStatus("实时预览", _camera.CurrentDeviceDescription);

                CancellationTokenSource cts = new CancellationTokenSource();
                _previewCts = cts;
                _previewStopSignal = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                int sessionId = Interlocked.Increment(ref _previewSessionId);
                _previewTask = Task.Run(() => LivePreviewLoop(sessionId, cts.Token, cts), cts.Token);

                AppendLog("实时预览已启动。");
            }
            catch (Exception ex)
            {
                AppendLog("启动实时画面失败: " + ex.Message);
                RefreshStatus("错误", ex.Message);
            }
        }

        private async Task RunSingleOnceAsync()
        {
            if (!EnsureJobReady())
            {
                return;
            }

            if (!await StopSingleRunAsyncFixed())
            {
                return;
            }

            if (!await StopContinuousAsyncFixed())
            {
                return;
            }

            if (!await StopPreviewAsyncFixed())
            {
                return;
            }

            try
            {
                EnsureCameraReady();
                RefreshStatus("单次运行", "正在采集当前帧并执行 02.vpp");

                Task<RunResult> runTask = Task.Run(() => ExecuteSingleRunCore(RunGrabTimeoutMs));
                _singleRunTask = runTask;

                RunResult result;
                try
                {
                    result = await runTask;
                }
                finally
                {
                    if (ReferenceEquals(_singleRunTask, runTask))
                    {
                        _singleRunTask = null;
                    }
                }

                ApplyRunResult(result);
                AppendLog(string.Format(
                    "单次运行完成: 相机帧={0}, 运行={1}, 结果={2}, 用时={3:0.00} ms",
                    result.FrameNumber,
                    result.RunIndex,
                    result.ResultText,
                    result.ProcessingTimeMs));
            }
            catch (TimeoutException ex)
            {
                AppendLog("单次运行超时: " + ex.Message);
                RefreshStatus("错误", ex.Message);
            }
            catch (Exception ex)
            {
                AppendLog("单次运行失败: " + ex.Message);
                RefreshStatus("错误", ex.Message);
            }
        }

        private async Task StartContinuousRunAsync()
        {
            if (!EnsureJobReady())
            {
                return;
            }

            if (!await StopSingleRunAsyncFixed())
            {
                return;
            }

            if (_continuousTask != null && !_continuousTask.IsCompleted)
            {
                AppendLog("持续运行已经在进行中。");
                return;
            }

            if (!await StopPreviewAsyncFixed())
            {
                return;
            }

            if (!await StopContinuousAsyncFixed())
            {
                return;
            }

            try
            {
                EnsureCameraReady();
                ClearDisplay();
                RefreshStatus("持续运行", "正在循环执行单次运行");

                CancellationTokenSource cts = new CancellationTokenSource();
                _continuousCts = cts;
                int sessionId = Interlocked.Increment(ref _continuousSessionId);
                _continuousTask = Task.Run(() => ContinuousRunLoop(sessionId, cts.Token, cts), cts.Token);

                AppendLog("持续运行已启动。");
            }
            catch (Exception ex)
            {
                AppendLog("启动持续运行失败: " + ex.Message);
                RefreshStatus("错误", ex.Message);
            }
        }

        private void LivePreviewLoop(int sessionId, CancellationToken token, CancellationTokenSource cts)
        {
            try
            {
                bool fitApplied = false;
                long previewIntervalTicks = Math.Max(1L, Stopwatch.Frequency * PreviewFrameIntervalMs / 1000L);
                long nextPreviewTick = 0L;

                while (!token.IsCancellationRequested)
                {
                    if (_previewFramePending)
                    {
                        Thread.Sleep(5);
                        continue;
                    }

                    long now = Stopwatch.GetTimestamp();
                    if (now < nextPreviewTick)
                    {
                        Thread.Sleep(5);
                        continue;
                    }

                    CogImage8Grey previewImage = null;
                    bool grabbed = _camera.TryAcquireMonoFrame(
                        PreviewGrabTimeoutMs,
                        delegate (IntPtr monoBuffer, int width, int height, uint frameNumber)
                        {
                            previewImage = GetPreviewWriteBuffer(width, height);
                            CopyMonoBufferToImage(monoBuffer, width, height, previewImage);
                        });

                    if (!grabbed)
                    {
                        continue;
                    }

                    if (token.IsCancellationRequested)
                    {
                        break;
                    }

                    if (_previewFramePending)
                    {
                        continue;
                    }

                    _previewFramePending = true;
                    bool doFit = !fitApplied;
                    nextPreviewTick = Stopwatch.GetTimestamp() + previewIntervalTicks;
                    if (!PostPreviewFrame(sessionId, previewImage, doFit))
                    {
                        _previewFramePending = false;
                        break;
                    }

                    fitApplied = true;
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!_isClosing)
                {
                    AppendLog("实时预览出错: " + ex.Message);
                    RefreshStatus("错误", ex.Message);
                }
            }
            finally
            {
                _previewFramePending = false;
                TaskCompletionSource<bool> stopSignal = _previewStopSignal;
                if (stopSignal != null)
                {
                    stopSignal.TrySetResult(true);
                }

                cts.Dispose();
                RunOnUiThread(delegate
                {
                    if (_previewSessionId == sessionId)
                    {
                        _previewCts = null;
                        _previewTask = null;
                        _previewStopSignal = null;
                        if (!_isClosing)
                        {
                            RefreshStatus("空闲", "实时预览已停止");
                            AppendLog("实时预览已停止。");
                        }
                    }
                });
            }
        }

        private void ContinuousRunLoop(int sessionId, CancellationToken token, CancellationTokenSource cts)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    RunResult result;
                    try
                    {
                        result = ExecuteSingleRunCore(RunGrabTimeoutMs);
                    }
                    catch (TimeoutException)
                    {
                        continue;
                    }

                    ApplyRunResult(result);

                    if (result.RunIndex == 1 || result.RunIndex % 10 == 0)
                    {
                        AppendLog(string.Format(
                            "持续运行: 运行={0}, 相机帧={1}, 结果={2}, 用时={3:0.00} ms",
                            result.RunIndex,
                            result.FrameNumber,
                            result.ResultText,
                            result.ProcessingTimeMs));
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!_isClosing)
                {
                    AppendLog("持续运行出错: " + ex.Message);
                    RefreshStatus("错误", ex.Message);
                }
            }
            finally
            {
                cts.Dispose();
                RunOnUiThread(delegate
                {
                    if (_continuousSessionId == sessionId)
                    {
                        _continuousCts = null;
                        _continuousTask = null;
                        if (!_isClosing)
                        {
                            RefreshStatus("空闲", "持续运行已停止");
                            AppendLog("持续运行已停止。");
                        }
                    }
                });
            }
        }

        private bool PostPreviewFrame(int sessionId, ICogImage image, bool doFit)
        {
            if (image == null || _isClosing || IsDisposed || !IsHandleCreated)
            {
                return false;
            }

            try
            {
                BeginInvoke((Action)delegate
                {
                    try
                    {
                        if (_isClosing || _previewSessionId != sessionId || IsDisposed)
                        {
                            return;
                        }

                        ICogImage oldImage = cogDisplayImage.Image;
                        if (!ReferenceEquals(oldImage, image))
                        {
                            cogDisplayImage.Image = image;
                            MarkPreviewDisplayedBuffer(image as CogImage8Grey);
                        }

                        if (doFit)
                        {
                            FillDisplayToWindow(image);
                        }

                        if (!ReferenceEquals(oldImage, image) && !IsPreviewBufferImage(oldImage))
                        {
                            DisposeCogImage(oldImage);
                        }
                    }
                    finally
                    {
                        _previewFramePending = false;
                    }
                });

                return true;
            }
            catch
            {
                return false;
            }
        }

        private CogImage8Grey GetPreviewWriteBuffer(int width, int height)
        {
            lock (_previewBufferSync)
            {
                bool bufferInvalid =
                    _previewBufferA == null ||
                    _previewBufferB == null ||
                    _previewBufferA.Width != width ||
                    _previewBufferA.Height != height ||
                    _previewBufferB.Width != width ||
                    _previewBufferB.Height != height;

                if (bufferInvalid)
                {
                    DisposePreviewBuffersLocked();
                    _previewBufferA = new CogImage8Grey(width, height);
                    _previewBufferB = new CogImage8Grey(width, height);
                    _previewDisplayedBuffer = null;
                }

                return ReferenceEquals(_previewDisplayedBuffer, _previewBufferA) ? _previewBufferB : _previewBufferA;
            }
        }

        private void MarkPreviewDisplayedBuffer(CogImage8Grey buffer)
        {
            lock (_previewBufferSync)
            {
                if (ReferenceEquals(buffer, _previewBufferA) || ReferenceEquals(buffer, _previewBufferB))
                {
                    _previewDisplayedBuffer = buffer;
                }
            }
        }

        private void DisposePreviewBuffers()
        {
            lock (_previewBufferSync)
            {
                DisposePreviewBuffersLocked();
            }
        }

        private void DisposePreviewBuffersLocked()
        {
            CogImage8Grey bufferA = _previewBufferA;
            CogImage8Grey bufferB = _previewBufferB;

            _previewBufferA = null;
            _previewBufferB = null;
            _previewDisplayedBuffer = null;

            DisposeCogImage(bufferA);
            if (!ReferenceEquals(bufferA, bufferB))
            {
                DisposeCogImage(bufferB);
            }
        }

        private static void CopyMonoBufferToImage(IntPtr source, int width, int height, CogImage8Grey target)
        {
            ICogImage8PixelMemory pixelMemory = target.Get8GreyPixelMemory(
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
                    IntPtr sourceRow = IntPtr.Add(source, y * width);
                    IntPtr destinationRow = IntPtr.Add(destination, y * destinationStride);
                    CopyMemory(destinationRow, sourceRow, width);
                }
            }
            finally
            {
                pixelMemory.Dispose();
            }
        }

        private bool IsPreviewBufferImage(ICogImage image)
        {
            lock (_previewBufferSync)
            {
                return ReferenceEquals(image, _previewBufferA) || ReferenceEquals(image, _previewBufferB);
            }
        }

        private async Task<bool> StopSingleRunAsyncFixed()
        {
            Task singleRunTask = _singleRunTask;
            if (singleRunTask == null)
            {
                return true;
            }

            Task finished = await Task.WhenAny(singleRunTask, Task.Delay(StopWaitTimeoutMs));
            if (finished != singleRunTask)
            {
                AppendLog("单次运行停止超时。");
                return false;
            }

            try
            {
                await singleRunTask;
            }
            catch
            {
            }

            if (ReferenceEquals(_singleRunTask, singleRunTask))
            {
                _singleRunTask = null;
            }

            return true;
        }

        private async Task<bool> StopPreviewAsyncFixed()
        {
            Task previewTask = _previewTask;
            CancellationTokenSource previewCts = _previewCts;
            Task previewStopTask = _previewStopSignal != null ? _previewStopSignal.Task : null;

            if (previewTask == null)
            {
                if (previewCts != null)
                {
                    previewCts.Cancel();
                    _previewCts = null;
                }

                _previewStopSignal = null;
                return true;
            }

            if (previewCts != null && !previewCts.IsCancellationRequested)
            {
                previewCts.Cancel();
            }

            Task finished = previewStopTask != null
                ? await Task.WhenAny(previewTask, previewStopTask, Task.Delay(StopWaitTimeoutMs))
                : await Task.WhenAny(previewTask, Task.Delay(StopWaitTimeoutMs));

            if (finished != previewTask && finished != previewStopTask)
            {
                AppendLog("实时预览停止超时。");
                return false;
            }

            try
            {
                await previewTask;
            }
            catch
            {
            }

            if (ReferenceEquals(_previewTask, previewTask))
            {
                _previewTask = null;
            }

            if (ReferenceEquals(_previewCts, previewCts))
            {
                _previewCts = null;
            }

            if (previewStopTask != null && previewStopTask.IsCompleted)
            {
                _previewStopSignal = null;
            }

            return true;
        }

        private async Task<bool> StopContinuousAsyncFixed()
        {
            Task continuousTask = _continuousTask;
            CancellationTokenSource continuousCts = _continuousCts;

            if (continuousTask == null)
            {
                if (continuousCts != null)
                {
                    continuousCts.Cancel();
                    _continuousCts = null;
                }

                return true;
            }

            if (continuousCts != null && !continuousCts.IsCancellationRequested)
            {
                continuousCts.Cancel();
            }

            Task finished = await Task.WhenAny(continuousTask, Task.Delay(StopWaitTimeoutMs));
            if (finished != continuousTask)
            {
                AppendLog("持续运行停止超时。");
                return false;
            }

            try
            {
                await continuousTask;
            }
            catch
            {
            }

            if (ReferenceEquals(_continuousTask, continuousTask))
            {
                _continuousTask = null;
            }

            if (ReferenceEquals(_continuousCts, continuousCts))
            {
                _continuousCts = null;
            }

            return true;
        }

        private async Task StopAllAcquisitionAsync(bool disposeCamera, bool finalizeSdk)
        {
            try
            {
                await StopSingleRunAsyncFixed();
            }
            catch
            {
            }

            try
            {
                await StopPreviewAsyncFixed();
            }
            catch
            {
            }

            try
            {
                await StopContinuousAsyncFixed();
            }
            catch
            {
            }

            if (disposeCamera)
            {
                try
                {
                    _camera.Dispose();
                }
                catch
                {
                }
            }

            if (finalizeSdk)
            {
                try
                {
                    HikrobotMvsCamera.FinalizeSdk();
                }
                catch
                {
                }
            }
        }

        private RunResult ExecuteSingleRunCore(int timeoutMs)
        {
            if (_toolGroup == null || _imageSourceTool == null || _inputImageProperty == null)
            {
                throw new InvalidOperationException("02.vpp 尚未就绪。");
            }

            ICogImage sourceImage;
            uint frameNumber;
            if (!_camera.TryAcquireMonoImage(timeoutMs, out sourceImage, out frameNumber))
            {
                throw new TimeoutException("获取图像超时。");
            }

            _inputImageProperty.SetValue(_imageSourceTool, sourceImage, null);
            _toolGroup.Run();

            ICogRunStatus runStatus = _toolGroup.RunStatus;
            ICogRecord lastRunRecord = _toolGroup.CreateLastRunRecord();
            List<ICogGraphic> graphics = new List<ICogGraphic>();
            CollectGraphics(lastRunRecord, graphics);

            ICogImage displayImage = FindOutputImage(lastRunRecord);
            if (displayImage == null)
            {
                throw new InvalidOperationException("未找到 CogToolBlock1.OutputImage，无法显示输出图像。");
            }

            int runIndex = Interlocked.Increment(ref _runCount);
            return new RunResult
            {
                RunIndex = runIndex,
                FrameNumber = frameNumber,
                ProcessingTimeMs = runStatus != null ? runStatus.ProcessingTime : 0.0,
                ResultText = runStatus != null ? runStatus.Result.ToString() : "Unknown",
                Message = runStatus != null ? runStatus.Message : string.Empty,
                DisplayImage = displayImage,
                StaticGraphics = graphics
            };
        }

        private ICogImage FindOutputImage(ICogRecord record)
        {
            if (record == null)
            {
                return null;
            }

            string[] candidateKeys =
            {
                "LastRun." + ToolBlockName + "." + OutputImageRecordKey,
                ToolBlockName + "." + OutputImageRecordKey,
                OutputImageRecordKey
            };

            for (int i = 0; i < candidateKeys.Length; i++)
            {
                ICogRecord outputRecord = FindRecordByKeyFlexible(record, candidateKeys[i]);
                if (outputRecord == null)
                {
                    continue;
                }

                ICogImage outputImage = outputRecord.Content as ICogImage ?? FindFirstImage(outputRecord);
                if (outputImage != null)
                {
                    return outputImage;
                }
            }

            ICogRecord pathRecord = FindRecordByPath(record, new[] { "LastRun", ToolBlockName, OutputImageRecordKey });
            if (pathRecord != null)
            {
                ICogImage outputImage = pathRecord.Content as ICogImage ?? FindFirstImage(pathRecord);
                if (outputImage != null)
                {
                    return outputImage;
                }
            }

            return null;
        }

        private void CollectGraphics(ICogRecord record, List<ICogGraphic> graphics)
        {
            if (record == null)
            {
                return;
            }

            ICogGraphic graphic = record.Content as ICogGraphic;
            if (graphic != null)
            {
                graphics.Add(graphic);
            }
            else
            {
                CogGraphicCollection graphicCollection = record.Content as CogGraphicCollection;
                if (graphicCollection != null)
                {
                    for (int i = 0; i < graphicCollection.Count; i++)
                    {
                        ICogGraphic collectedGraphic = graphicCollection[i] as ICogGraphic;
                        if (collectedGraphic != null)
                        {
                            graphics.Add(collectedGraphic);
                        }
                    }
                }
            }

            if (record.SubRecords != null)
            {
                for (int i = 0; i < record.SubRecords.Count; i++)
                {
                    CollectGraphics(record.SubRecords[i], graphics);
                }
            }
        }

        private ICogRecord FindRecordByKeyFlexible(ICogRecord record, string key)
        {
            if (record == null || string.IsNullOrWhiteSpace(key))
            {
                return null;
            }

            if (RecordKeyMatches(record.RecordKey, key))
            {
                return record;
            }

            if (record.SubRecords == null)
            {
                return null;
            }

            for (int i = 0; i < record.SubRecords.Count; i++)
            {
                ICogRecord child = record.SubRecords[i];
                if (child == null)
                {
                    continue;
                }

                if (RecordKeyMatches(child.RecordKey, key))
                {
                    return child;
                }

                ICogRecord nested = FindRecordByKeyFlexible(child, key);
                if (nested != null)
                {
                    return nested;
                }
            }

            return null;
        }

        private ICogRecord FindRecordByPath(ICogRecord record, string[] segments)
        {
            return FindRecordByPath(record, segments, 0);
        }

        private ICogRecord FindRecordByPath(ICogRecord record, string[] segments, int index)
        {
            if (record == null || segments == null || segments.Length == 0 || index >= segments.Length)
            {
                return null;
            }

            if (!RecordKeyMatches(record.RecordKey, segments[index]))
            {
                if (record.SubRecords == null)
                {
                    return null;
                }

                for (int i = 0; i < record.SubRecords.Count; i++)
                {
                    ICogRecord childMatch = FindRecordByPath(record.SubRecords[i], segments, index);
                    if (childMatch != null)
                    {
                        return childMatch;
                    }
                }

                return null;
            }

            if (index == segments.Length - 1)
            {
                return record;
            }

            if (record.SubRecords == null)
            {
                return null;
            }

            for (int i = 0; i < record.SubRecords.Count; i++)
            {
                ICogRecord childMatch = FindRecordByPath(record.SubRecords[i], segments, index + 1);
                if (childMatch != null)
                {
                    return childMatch;
                }
            }

            return null;
        }

        private bool RecordKeyMatches(string recordKey, string expectedKey)
        {
            if (string.IsNullOrWhiteSpace(recordKey) || string.IsNullOrWhiteSpace(expectedKey))
            {
                return false;
            }

            return string.Equals(recordKey, expectedKey, StringComparison.OrdinalIgnoreCase) ||
                   recordKey.EndsWith("." + expectedKey, StringComparison.OrdinalIgnoreCase) ||
                   recordKey.EndsWith(expectedKey, StringComparison.OrdinalIgnoreCase);
        }

        private ICogImage FindFirstImage(ICogRecord record)
        {
            if (record == null)
            {
                return null;
            }

            ICogImage image = record.Content as ICogImage;
            if (image != null)
            {
                return image;
            }

            if (record.SubRecords == null)
            {
                return null;
            }

            for (int i = 0; i < record.SubRecords.Count; i++)
            {
                ICogImage childImage = FindFirstImage(record.SubRecords[i]);
                if (childImage != null)
                {
                    return childImage;
                }
            }

            return null;
        }

        private void ApplyRunResult(RunResult result)
        {
            if (_isClosing || result == null)
            {
                return;
            }

            RunOnUiThread(delegate
            {
                if (_isClosing)
                {
                    return;
                }

                cogDisplayImage.StaticGraphics.Clear();
                cogDisplayImage.InteractiveGraphics.Clear();

                ICogImage oldImage = cogDisplayImage.Image;
                if (!ReferenceEquals(oldImage, result.DisplayImage))
                {
                    cogDisplayImage.Image = result.DisplayImage;
                }

                for (int i = 0; i < result.StaticGraphics.Count; i++)
                {
                    ICogGraphic graphic = result.StaticGraphics[i];
                    if (graphic != null)
                    {
                        cogDisplayImage.StaticGraphics.Add(graphic, "Run" + result.RunIndex + "_" + i);
                    }
                }

                if (result.DisplayImage != null)
                {
                    FillDisplayToWindow(result.DisplayImage);
                }

                if (!ReferenceEquals(oldImage, result.DisplayImage) && !IsPreviewBufferImage(oldImage))
                {
                    DisposeCogImage(oldImage);
                }

                string message = string.IsNullOrWhiteSpace(result.Message) ? result.ResultText : result.Message;
                RefreshStatus(
                    "运行中",
                    string.Format("运行={0}, 相机帧={1}, 结果={2}, 用时={3:0.00} ms",
                        result.RunIndex,
                        result.FrameNumber,
                        result.ResultText,
                        result.ProcessingTimeMs));

                if (!string.IsNullOrWhiteSpace(message) &&
                    !string.Equals(message, result.ResultText, StringComparison.OrdinalIgnoreCase))
                {
                    AppendLog(message);
                }
            });
        }

        private void ClearDisplay()
        {
            if (_isClosing || !IsHandleCreated || IsDisposed)
            {
                return;
            }

            RunOnUiThread(delegate
            {
                cogDisplayImage.StaticGraphics.Clear();
                cogDisplayImage.InteractiveGraphics.Clear();
                ICogImage oldImage = cogDisplayImage.Image;
                cogDisplayImage.Image = null;

                if (!IsPreviewBufferImage(oldImage))
                {
                    DisposeCogImage(oldImage);
                }

                DisposePreviewBuffers();
            });
        }

        private void DisposeCogImage(ICogImage image)
        {
            IDisposable disposable = image as IDisposable;
            if (disposable == null)
            {
                return;
            }

            try
            {
                disposable.Dispose();
            }
            catch
            {
            }
        }

        private void AppendLog(string message)
        {
            if (_isClosing || string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            RunOnUiThread(delegate
            {
                if (_isClosing)
                {
                    return;
                }

                string line = string.Format("[{0:HH:mm:ss.fff}] {1}{2}", DateTime.Now, message, Environment.NewLine);
                txtLog.AppendText(line);
            });
        }

        private void RefreshStatus(string title, string detail)
        {
            if (_isClosing)
            {
                return;
            }

            RunOnUiThread(delegate
            {
                if (_isClosing)
                {
                    return;
                }

                string cameraText = _camera.IsOpened ? _camera.CurrentDeviceDescription : "未打开";
                txtStatus.Text =
                    "状态: " + title + Environment.NewLine +
                    "详情: " + detail + Environment.NewLine +
                    "相机: " + cameraText;
            });
        }

        private void RunOnUiThread(Action action)
        {
            if (action == null || _isClosing || IsDisposed || !IsHandleCreated)
            {
                return;
            }

            if (InvokeRequired)
            {
                try
                {
                    BeginInvoke(action);
                }
                catch
                {
                }

                return;
            }

            action();
        }

        private void FillDisplayToWindow(ICogImage image)
        {
            if (image == null || _isClosing || IsDisposed || !IsHandleCreated)
            {
                return;
            }

            int displayWidth = cogDisplayImage.ClientSize.Width;
            int displayHeight = cogDisplayImage.ClientSize.Height;
            if (displayWidth <= 0 || displayHeight <= 0 || image.Width <= 0 || image.Height <= 0)
            {
                return;
            }

            double zoomX = (double)displayWidth / image.Width;
            double zoomY = (double)displayHeight / image.Height;
            double zoom = Math.Min(zoomX, zoomY);

            cogDisplayImage.SetImagePanAnchor(0.0, 0.0, CogDisplayPanAnchorConstants.Absolute);
            cogDisplayImage.SetScreenPanAnchor(0.0, 0.0, CogDisplayPanAnchorConstants.Absolute);
            cogDisplayImage.Zoom = zoom;
            cogDisplayImage.PanX = 0.0;
            cogDisplayImage.PanY = 0.0;
        }

        private sealed class RunResult
        {
            public int RunIndex { get; set; }
            public uint FrameNumber { get; set; }
            public double ProcessingTimeMs { get; set; }
            public string ResultText { get; set; }
            public string Message { get; set; }
            public ICogImage DisplayImage { get; set; }
            public List<ICogGraphic> StaticGraphics { get; set; }
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {

        }
    }
}

