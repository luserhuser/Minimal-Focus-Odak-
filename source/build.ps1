$ErrorActionPreference = 'Stop'
$fx = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319"
& "$fx\csc.exe" /nologo /target:winexe /optimize+ /out:"$PSScriptRoot\..\Odak.exe" /reference:"$fx\WPF\PresentationFramework.dll" /reference:"$fx\WPF\PresentationCore.dll" /reference:"$fx\WPF\WindowsBase.dll" /reference:"$fx\System.Xaml.dll" /reference:"$fx\System.Windows.Forms.dll" /reference:"$fx\System.Drawing.dll" /reference:"$fx\System.Web.Extensions.dll" /reference:"$fx\System.Net.Http.dll" /reference:"$fx\System.Security.dll" "$PSScriptRoot\Odak.cs"
if ($LASTEXITCODE -ne 0) { throw 'Derleme başarısız.' }


