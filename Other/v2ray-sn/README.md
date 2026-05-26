# Xray-core for Netch

Builds [XTLS/Xray-core](https://github.com/XTLS/Xray-core) as `xray.exe` for Netch.

The build script pins a reproducible Xray release by default and accepts `-XrayRef`
when a different tag or branch is needed.

```powershell
.\build.ps1
.\build.ps1 -XrayRef v26.3.27
.\build.ps1 -GoProxy https://goproxy.cn,direct
```

The output is written to `Other\release\xray.exe`; the top-level build script then
copies it into `release\bin`.

If the requested `-XrayRef` changes, the script refreshes `src` automatically.
If a previous run cloned `src` but failed before producing `xray.exe`, rerun this
script after installing Go. To force a fresh clone manually, run `..\clean.ps1`
from this directory or delete `Other\v2ray-sn\src`.
