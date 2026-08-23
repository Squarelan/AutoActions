# -*- coding: utf-8 -*-
import re, sys

# 中文翻译词典 (zh-Hans)
T = {
 "ActionGroup_HDR":"HDR",
 "Actions":"动作",
 "ActionShortcuts":"快捷动作",
 "ActionType":"类型",
 "Action_HDRSwitch":"设置 HDR",
 "ActivateHDR":"启用 HDR",
 "Add":"添加",
 "AllDisplays":"所有显示器",
 "AlreadyRunning":"AutoActions 已在运行。",
 "AppClosedActions":"应用关闭时的动作",
 "AppFocusedActions":"应用获得焦点时的动作",
 "Application":"应用",
 "ApplicationAction":"应用动作",
 "ApplicationName":"应用名称",
 "Applications":"应用",
 "AppLostFocusActions":"应用失去焦点时的动作",
 "AppStartedActions":"应用启动时的动作",
 "Arguments":"参数",
 "AudioAction":"音频",
 "AutoActions":"AutoActions",
 "AutoActionsToolTip":"根据运行的应用在此显示器上启用 HDR",
 "AutoStart":"开机自启",
 "AutoUpdate":"自动更新",
 "BuyBeer":"请我喝杯啤酒!",
 "ChangeColorDepth":"更改色深",
 "ChangeHDR":"更改 HDR",
 "Changelog":"更新日志",
 "ChangePlaybackDevice":"更改播放设备",
 "ChangeRecordDevice":"更改录音设备",
 "ChangeRefreshRate":"更改刷新率",
 "ChangeResolution":"更改分辨率",
 "CheckForNewVersion":"启动时检查新版本",
 "ChooseApplication":"选择应用",
 "ChooseUWPApplication":"选择 UWP 应用...",
 "Close":"关闭",
 "CloseProgramAction":"关闭程序",
 "ColorDepth":"色深 [位]",
 "ColorDepth.BPC10":"10 位",
 "ColorDepth.BPC12":"12 位",
 "ColorDepth.BPC16":"16 位",
 "ColorDepth.BPC6":"6 位",
 "ColorDepth.BPC8":"8 位",
 "ColorDepth.BPCUnkown":"-",
 "CompatibilityModeToolTip":"在首次出现时重启该应用,因为部分应用需要在启动前就已启用 HDR。",
 "CreateLogFile":"创建日志文件",
 "CurrentApplication":"当前应用",
 "CurrentProfile":"当前配置:",
 "DeactivateHDR":"停用 HDR",
 "DefaultProfile":"默认配置",
 "DeviceID":"设备 ID",
 "DeviceUID":"UID",
 "Display":"显示器",
 "DisplayAction":"显示器动作",
 "DisplayName":"显示器名称",
 "Displays":"显示器",
 "Download":"从 GitHub 下载",
 "DownloadNewestVersion":"下载",
 "Edit":"编辑",
 "Error":"错误",
 "ExceptionRestartProcess":"重启应用失败。",
 "FilePath":"文件路径",
 "ForceKill":"强制关闭",
 "GitHub":"GitHub",
 "GlobalAutoHDR":"对所有显示器使用自动 HDR 模式",
 "GraphicsCard":"显卡",
 "HDR":"HDR",
 "HDRMode":"自动 HDR 模式",
 "HideSplashScreenOnStartup":"启动时隐藏启动画面",
 "HideSplashScreenOnUpdate":"更新时隐藏启动画面",
 "Icon":"图标",
 "Info":"信息",
 "IsPrimaryMonitor":"主显示器",
 "LastActions":"最近动作",
 "LaunchApplication":"启动应用",
 "License":"许可证",
 "Local":"本地",
 "Logging":"日志记录",
 "Logs":"日志",
 "ManagedHDR":"自动 HDR 模式",
 "MessageInvalidSettings":"设置无效。",
 "MessageMissingAudioDevice":"缺少音频设备。",
 "MessageMissingFile":"缺少文件。",
 "MessageMissingProcessName":"缺少进程名称。",
 "MessageReferenceLoop":"被引用的配置形成了循环。",
 "MessageReferenceProfileNotFound":"未找到 Guid 为 {0} 的被引用配置。",
 "MinimizeToTray":"关闭时最小化到托盘",
 "Monitors":"显示器",
 "Name":"名称",
 "No":"否",
 "NoApplication":"没有正在运行的应用",
 "NoProfile":"未分配配置",
 "OK":"确定",
 "Online":"在线",
 "Open":"打开",
 "Priority":"优先级",
 "ProcessName":"进程名称",
 "Profile":"配置",
 "Profiles":"配置",
 "ReferenceProfileAction":"引用配置",
 "RefreshRate":"刷新率 [Hz]",
 "ReleaseDate":"发布日期",
 "RemoveApplication":"移除",
 "RemoveProfile":"移除",
 "Resolution":"分辨率",
 "RestartProccessOnFirstOccurence":"重启应用",
 "RunAction":"运行",
 "RunProgramAction":"运行程序",
 "Save":"保存",
 "Settings":"设置",
 "Shutdown":"退出应用",
 "StartToTray":"启动时最小化到托盘",
 "Status":"状态",
 "Updating":"正在更新 AutoActions...",
 "Version":"版本",
 "VolumeSyncDevices":"音量同步设备",
 "WaitForProgramEnd":"等待程序退出",
 "Yes":"是",
}
print("dict total loaded:", len(T))

def gen(src, dst, keys):
    txt = open(src, encoding='utf-8-sig').read()
    # 记录源文件里的可译键(带 xml:space="preserve" 的)
    src_keys = set(re.findall(r'<data name="([^"]+)" xml:space="preserve">', txt))
    def repl(m):
        name, attrs, val = m.group(1), m.group(2), m.group(3)
        if name in T:
            return f'<data name="{name}"{attrs}><value>{T[name]}</value>'
        return m.group(0)  # 未翻译,保留原文
    out = re.sub(r'<data name="([^"]+)"( xml:space="preserve")>\s*<value>(.*?)</value>',
                 repl, txt, flags=re.S)
    open(dst, 'w', encoding='utf-8-sig').write(out)
    translated = [k for k in src_keys if k in T]
    missing = [k for k in src_keys if k not in T]
    return src_keys, translated, missing

sk, tr, ms = gen("Source/AutoActions.ProjectResources/ProjectLocales.resx",
                 "Source/AutoActions.ProjectResources/ProjectLocales.zh-Hans.resx", T)
print(f"ProjectLocales: 源键{len(sk)} 已译{len(tr)} 未译{len(ms)}")
print("  未译键:", ms)

sk2, tr2, ms2 = gen("Source/AutoActions.Displays/Locale.resx",
                    "Source/AutoActions.Displays/Locale.zh-Hans.resx", T)
print(f"Displays: 源键{len(sk2)} 已译{len(tr2)} 未译{len(ms2)}")
print("  未译键:", ms2)
