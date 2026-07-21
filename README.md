# AutoActions

> **简体中文汉化版 / Simplified Chinese Localization**
>
> 本仓库是 [Codectory/AutoActions](https://github.com/Codectory/AutoActions) 的 fork,由 Squarelan 添加了简体中文(zh-Hans)界面翻译。
> 界面语言跟随 Windows 系统区域设置,在中文系统上自动显示中文。
>
> **修改说明(GPL v3 §5a):** 2026-07-21,新增 `ProjectLocales.zh-Hans.resx`、`Locale.zh-Hans.resx` 两个中文资源文件并在对应 `.csproj` 中注册。GPL 协议全文(`LicenseContent`)未翻译,回退英文原文。
> 原始版权归 Heiko Thome (Codectory) 所有,本项目继续以 **GNU GPL v3** 授权。
>
> ---
>
> This repository is a fork of [Codectory/AutoActions](https://github.com/Codectory/AutoActions) with a Simplified Chinese (zh-Hans) UI translation added by Squarelan. The UI language follows the Windows system locale.
> **Modification notice (GPL v3 §5a):** On 2026-07-21, added `ProjectLocales.zh-Hans.resx` and `Locale.zh-Hans.resx`, registered in the corresponding `.csproj` files. Original copyright © Heiko Thome (Codectory); this project remains licensed under **GNU GPL v3**.

With AutoActions, you can created profiles which included several actions. You can change display settings, set default audio devices or launch any other script or program.
Once you have added an application and assigned a profile to it, AutoActions will monitor this application and will run the actions based on the assigned profile.


##  Features

##### Auto-Start
AutoActions will start, after the current user has logged on. 
##### Profiles
You can create profiles, which contain actions to be run on application events. The possible events are "Started", "Closed", "Got focus", "Lost focus".
##### Profile actions
###### Display action
Toggle HDR,  change resolution, refresh rate and color depth
###### Audio action
Audio actions change default playback or record audio devices.
###### Run or close program action
Run any program or script as you want or kill running proccesses.
###### Reference action
Reference action are referencing to an existing profile.
##### Application assignment
You can assign one profile to different applications. 
##### Action shortcuts
Create action shortcuts and access them in the status view or from the tray menu.
##### Compatibility mode (Restart)
In some games, e.g. Cyberpunk 2077,  you don't see any HDR options in settings, when HDR was not enabled in Windows before the game was launched. Therefore, the application kills the process on the first occurence, activates hdr and restarts the process.  
##### Display settings per monitor
Select which monitors you want to control with AutoActions

## Screenshots:

![ScreenShot](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Status_1-9-6.png)

![ScreenShot](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Profiles_1-9-6.png)

![ScreenShot](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Applications_1-9-6.png)

![ScreenShot](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Monitors_1-9-6.png)

![ScreenShot](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Settings_1-9-6.png)
