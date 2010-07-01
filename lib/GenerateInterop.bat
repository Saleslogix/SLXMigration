"C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\TlbImp.exe" "C:\WINDOWS\system32\olepro32.dll"            /out:Interop.StdType.dll
"C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\TlbImp.exe" "C:\Program Files\SalesLogix\stdvcl40.dll"    /out:Interop.StdVCL.dll
"C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\TlbImp.exe" "C:\Program Files\SalesLogix\SLXOptions.dll"  /out:Interop.SLXOptions.dll    /reference:Interop.StdVCL.dll
"C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\TlbImp.exe" "C:\Program Files\SalesLogix\SalesLogix.exe"  /out:Interop.SalesLogix.dll    /reference:Interop.StdType.dll /reference:Interop.SLXOptions.dll
"C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\TlbImp.exe" "C:\Program Files\SalesLogix\SLXControls.ocx" /out:Interop.SLXControls.dll   /reference:Interop.StdVCL.dll
"C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\TlbImp.exe" "C:\Program Files\SalesLogix\SLXCharts.ocx"   /out:Interop.SLXCharts.dll
"C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\TlbImp.exe" "C:\Program Files\SalesLogix\SLXDialogs.ocx"  /out:Interop.SLXDialogs.dll
"C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\AxImp.exe"  "C:\Program Files\SalesLogix\SLXControls.ocx" /out:Interop.AxSLXControls.dll /rcw:Interop.SLXControls.dll
"C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\AxImp.exe"  "C:\Program Files\SalesLogix\SLXCharts.ocx"   /out:Interop.AxSLXCharts.dll   /rcw:Interop.SLXCharts.dll
"C:\Program Files\Microsoft Visual Studio 8\SDK\v2.0\Bin\AxImp.exe"  "C:\Program Files\SalesLogix\SLXDialogs.ocx"  /out:Interop.AxSLXDialogs.dll  /rcw:Interop.SLXDialogs.dll
pause