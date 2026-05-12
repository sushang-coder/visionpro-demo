# VisionPro演示

这是一个基于 WinForms 的桌面程序，用来连接海康工业相机，并调用 Cognex VisionPro 作业完成图像采集、预览和结果显示。

## English Summary

A WinForms desktop demo for Hikrobot cameras and Cognex VisionPro. It loads the `02.vpp` job, previews camera images, and supports single-run or continuous processing.

## 这个项目现在能做什么

目前已经打通的流程包括：

- 初始化 MVS SDK
- 枚举并连接海康相机
- 加载 `02.vpp`
- 实时预览相机灰度图像
- 单次运行 VisionPro 作业
- 持续运行 VisionPro 作业
- 在界面里显示图像、日志和状态

程序默认优先匹配的相机型号是 `MV-CE060-10UC`。如果现场设备不是这个型号，需要按实际情况调整。

## 运行环境

- Visual Studio 2019 或更新版本
- .NET Framework 4.8
- Cognex VisionPro
- Hikrobot MVS SDK
- Windows
- 海康工业相机设备

工程目标框架是 .NET Framework 4.8，`Debug` 和 `Release` 默认都编译为 `x86`。另外还依赖这些组件：

- `Cognex.VisionPro.*`
- `MvCameraControl.Net.dll`

如果 VisionPro 或 MVS SDK 没装好，工程通常没法正常编译或运行。

## 目录结构

```text
.
├─ demo1.sln
└─ demo1/
   ├─ demo1.csproj
   ├─ Program.cs
   ├─ Form1.cs
   ├─ Form1.Designer.cs
   ├─ Form1.resx
   ├─ App.config
   ├─ HikrobotMvsCamera.cs
   └─ 02.vpp
```

主要文件如下：

- `demo1.sln`：解决方案入口
- `demo1/demo1.csproj`：主工程文件
- `demo1/Program.cs`：程序入口
- `demo1/Form1.cs`：主界面逻辑
- `demo1/Form1.Designer.cs`：界面布局
- `demo1/HikrobotMvsCamera.cs`：海康相机封装
- `demo1/02.vpp`：VisionPro 作业文件
- `demo1/App.config`：运行配置

## 界面和操作

程序里现在有四个主要按钮：

- “显示图像”：启动实时预览
- “关闭摄像头”：停止取流并关闭设备
- “单次运行”：抓一帧并执行一次 `02.vpp`
- “持续运行”：循环采集并持续执行作业

界面本身分成三块：

- 图像显示区
- 日志区
- 状态区

## 使用方法

1. 用 Visual Studio 打开 `demo1.sln`
2. 确认本机已经安装 Cognex VisionPro 和 Hikrobot MVS SDK
3. 确认 `demo1/02.vpp` 在工程目录里
4. 接好相机并确认驱动正常
5. 用 `Debug` 或 `Release` 编译
6. 运行程序后按需要使用界面按钮

第一次运行前，建议顺手确认这几项：

- VisionPro 引用路径和本机安装路径是否一致
- `MvCameraControl.Net.dll` 能不能正常解析
- 当前相机是否和默认优先型号一致
- 相机、驱动、SDK 位数是否和工程配置兼容

## 运行前提

这个项目不是纯软件示例，完整跑通要依赖设备和本机 SDK 环境。

- `HikrobotMvsCamera.cs` 负责相机发现、打开和采集
- `demo1.csproj` 里的 VisionPro 引用依赖本机安装目录
- `02.vpp` 缺失时，主界面初始化会失败
- 当前流程使用灰度图像，输出显示依赖 `CogToolBlock1.OutputImage`

## 已知问题

- 没装 VisionPro 或 MVS SDK 时，工程没法完整编译和运行
- 没有海康相机时，只能看代码，不能验证采集和处理链路
- 默认优先相机型号写死为 `MV-CE060-10UC`
- 仓库里还没有截图、录屏、安装包和完整部署说明
- 如果 SDK 安装路径和工程引用路径不一致，需要手动修正引用

## 仓库里建议放什么

建议长期保留：

- 源码
- 解决方案和配置文件
- `02.vpp` 这类运行必需资源
- 截图、演示说明、安装说明

不建议长期提交：

- `.vs/`
- `bin/`
- `obj/`
- `*.user`
- 本机临时文件和大体积调试产物

如果后面要分发可执行程序，优先放到 GitHub Releases，不要直接堆在源码目录。

## 后面还可以补什么

- `assets/`：界面截图、运行 GIF、演示视频封面
- `docs/`：VisionPro / MVS SDK 安装说明、部署步骤、常见问题
- 硬件型号、SDK 版本、驱动版本记录
- 发布包和版本说明
