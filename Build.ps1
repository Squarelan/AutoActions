function buildVS
{
    param
    (
        [parameter(Mandatory=$true)]
        [String] $path,

		[parameter(Mandatory=$true)]
        [String] $version,

        [parameter(Mandatory=$false)]
        [bool] $nuget = $true

    )
    process
    {
        # 自动探测 MSBuild(通过 vswhere),不再硬编码 VS 版本/版次路径
        $vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
        if (-not (Test-Path $vswhere)) { $vswhere = "$env:ProgramFiles\Microsoft Visual Studio\Installer\vswhere.exe" }
        if (-not (Test-Path $vswhere)) { Write-Error "找不到 vswhere.exe,请确认已安装 Visual Studio 2017+ 或 Build Tools。"; return }
        $msBuildExe = & $vswhere -latest -requires Microsoft.Component.MSBuild -find "MSBuild\**\Bin\MSBuild.exe" | Select-Object -First 1
        if (-not $msBuildExe) { Write-Error "vswhere 未找到 MSBuild,请在 VS Installer 中安装 'MSBuild' 组件。"; return }
        Write-Host "Using MSBuild: $($msBuildExe)" -foregroundcolor cyan

        Write-Host "Building x64 $($path)" -foregroundcolor green
        & "$($msBuildExe)" "$($path)" /t:Build /m /property:Configuration=Release /property:Platform=x64
		Write-Host "Building x86 $($path)" -foregroundcolor green
        & "$($msBuildExe)" "$($path)" /t:Build /m /property:Configuration=Release /property:Platform=x86

		$x64zip = ".\Releases\Release_AutoActions_$($version)_x64.zip"
		$x86zip = ".\Releases\Release_AutoActions_$($version)_x86.zip"

        # 确保输出目录存在
        New-Item -ItemType Directory -Force -Path ".\Releases" | Out-Null

        Write-Host "Creating Zip x64 $($x64zip)" -foregroundcolor green

        if (Test-Path $x64zip -PathType leaf)
        {del $x64zip}

        Get-ChildItem -Path ".\Source\Release_x64" |
        Where-Object {$_.PsIsContainer -eq $true -or $_.Extension -eq ".exe" -or $_.Extension -eq ".config" -or $_.Extension -eq ".dll" -or $_.Name -eq "UpdateData.json"   } | Compress-Archive -DestinationPath $x64zip

	    Write-Host "Creating Zip x86 $($x86zip)" -foregroundcolor green
        if (Test-Path $x86zip -PathType leaf)
        {del $x86zip}

        Get-ChildItem -Path ".\Source\Release_x86" |
        Where-Object {$_.PsIsContainer -eq $true -or $_.Extension -eq ".exe" -or $_.Extension -eq ".config" -or $_.Extension -eq ".dll"  -or $_.Name -eq "UpdateData.json"  } | Compress-Archive -DestinationPath $x86zip
        Write-Host "Finished!" -foregroundcolor green

    }
}



# 从 AssemblyInfo.cs 自动读取版本号(取前三段,如 1.10.0),作为发布包版本名
$assemblyInfo = ".\Source\AutoActions\Properties\AssemblyInfo.cs"
$verLine = Select-String -Path $assemblyInfo -Pattern 'AssemblyFileVersion\("([0-9]+\.[0-9]+\.[0-9]+)' | Select-Object -First 1
if (-not $verLine) { Write-Error "无法从 $($assemblyInfo) 读取 AssemblyFileVersion"; exit 1 }
$version = $verLine.Matches[0].Groups[1].Value
Write-Host "Detected version: $($version)" -foregroundcolor cyan

buildVS .\Source\AutoActions.sln -version $version
