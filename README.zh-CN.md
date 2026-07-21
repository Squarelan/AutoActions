# AutoActions — 简体中文文档

[English](README.md) | **简体中文** | [繁體中文](README.zh-TW.md)

> **汉化版说明**
> 本仓库是 [Codectory/AutoActions](https://github.com/Codectory/AutoActions) 的 fork,由 Squarelan 于 2026-07-21 添加简体中文(zh-Hans)与繁体中文(zh-Hant)界面翻译。原始版权归 Heiko Thome (Codectory) 所有,继续以 **GNU GPL v3** 授权。

---

## 简介

AutoActions 让你创建包含多个动作的**配置**。你可以更改显示器设置、设置默认音频设备,或启动任意脚本和程序。

为应用程序分配配置后,AutoActions 会监控该应用,并在对应事件触发时自动执行配置中的动作。

---

## 功能

### 开机自启
AutoActions 会在当前用户登录后自动启动。

### 配置
创建包含多个动作的配置,动作可在以下应用事件时触发:
- **已启动** — 应用程序开始运行
- **已关闭** — 应用程序退出
- **获得焦点** — 应用程序成为前台窗口
- **失去焦点** — 应用程序离开前台

### 配置动作类型

#### 显示器动作
切换 HDR、更改分辨率、刷新率和色深。

#### 音频动作
更改默认播放或录音设备。

#### 运行/关闭程序动作
按需运行任意程序或脚本,或强制关闭正在运行的进程。

#### 引用动作
引用已有的配置,方便复用。

### 应用程序分配
可将同一个配置分配给多个不同的应用程序。

### 快捷动作
创建动作快捷方式,可在状态视图或托盘菜单中快速访问。

### 兼容性模式(重启)
部分游戏(如《赛博朋克 2077》)要求 HDR 在游戏启动**前**就已在 Windows 中开启才能在游戏内显示 HDR 选项。开启兼容性模式后,AutoActions 会在首次检测到该应用时先关闭进程、启用 HDR,再重新启动它。

### 按显示器控制
可选择哪些显示器由 AutoActions 控制。

---

## 截图

![状态界面](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Status_1-9-6.png)

![配置界面](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Profiles_1-9-6.png)

![应用程序界面](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Applications_1-9-6.png)

![显示器界面](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Monitors_1-9-6.png)

![设置界面](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Settings_1-9-6.png)

---

## 语言支持

本 fork 在原版英文和德文基础上新增:
- **简体中文(zh-Hans)** — 中国大陆，随系统区域自动加载
- **繁体中文(zh-Hant)** — 台湾/香港，随系统区域自动加载

无需手动切换，Windows 系统语言为中文时自动生效。

---

## 许可证

本项目以 [GNU 通用公共许可证 v3.0](LICENSE) 授权。
原始版权 © Heiko Thome / Codectory。
