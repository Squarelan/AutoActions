# AutoActions — 繁體中文文件

[English](README.md) | [简体中文](README.zh-CN.md) | **繁體中文**

> **中文化版本說明**
> 本儲存庫是 [Codectory/AutoActions](https://github.com/Codectory/AutoActions) 的 fork,由 Squarelan 於 2026-07-21 新增簡體中文(zh-Hans)與繁體中文(zh-Hant)介面翻譯。原始版權歸 Heiko Thome (Codectory) 所有,繼續以 **GNU GPL v3** 授權。

---

## 簡介

AutoActions 讓你建立包含多個動作的**設定檔**。你可以變更顯示器設定、設定預設音訊裝置,或啟動任意指令碼與程式。

為應用程式指派設定檔後,AutoActions 會監控該應用程式,並在對應事件觸發時自動執行設定檔中的動作。

---

## 功能

### 開機自動啟動
AutoActions 會在目前使用者登入後自動啟動。

### 設定檔
建立包含多個動作的設定檔,動作可在以下應用程式事件時觸發:
- **已啟動** — 應用程式開始執行
- **已關閉** — 應用程式結束
- **取得焦點** — 應用程式成為前景視窗
- **失去焦點** — 應用程式離開前景

### 設定檔動作類型

#### 顯示器動作
切換 HDR、變更解析度、更新率與色彩深度。

#### 音訊動作
變更預設播放或錄音裝置。

#### 執行/關閉程式動作
依需求執行任意程式或指令碼,或強制關閉執行中的處理程序。

#### 參照動作
參照既有的設定檔,方便重複使用。

### 應用程式指派
可將同一個設定檔指派給多個不同的應用程式。

### 快捷動作
建立動作捷徑,可在狀態檢視或系統匣選單中快速存取。

### 相容性模式(重新啟動)
部分遊戲(如《電馭叛客 2077》)要求 HDR 在遊戲啟動**前**就已在 Windows 中開啟,才能在遊戲內顯示 HDR 選項。開啟相容性模式後,AutoActions 會在首次偵測到該應用程式時先關閉處理程序、啟用 HDR,再重新啟動它。

### 依顯示器控制
可選擇哪些顯示器由 AutoActions 控制。

---

## 螢幕截圖

![狀態介面](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Status_1-9-6.png)

![設定檔介面](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Profiles_1-9-6.png)

![應用程式介面](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Applications_1-9-6.png)

![顯示器介面](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Monitors_1-9-6.png)

![設定介面](https://raw.github.com/Codectory/AutoActions/main/Screenshots/Settings_1-9-6.png)

---

## 語言支援

本 fork 在原版英文與德文基礎上新增:
- **簡體中文(zh-Hans)** — 中國大陸，隨系統地區自動載入
- **繁體中文(zh-Hant)** — 台灣/香港，隨系統地區自動載入

無需手動切換，Windows 系統語言為中文時自動生效。

---

## 授權條款

本專案以 [GNU 通用公共授權條款 v3.0](LICENSE) 授權。
原始版權 © Heiko Thome / Codectory。
