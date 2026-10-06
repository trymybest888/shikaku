# Shikaku

เกมปริศนา Shikaku สำหรับ Windows — ไฟล์เดียว ไม่ต้องติดตั้ง เล่นออฟไลน์ได้

## เล่น

ดาวน์โหลด `Shikaku.exe` จาก [Releases](https://github.com/trymybest888/shikaku/releases) แล้วเปิดได้เลย (ต้องมี .NET Framework 4.5+ ซึ่งมีใน Windows 10/11)

ลากเมาส์แบ่งกระดานเป็นสี่เหลี่ยม แต่ละรูปต้องมีตัวเลขหนึ่งตัว และจำนวนช่องเท่ากับตัวเลขนั้น คลิกรูปเพื่อลบ

ระดับ: Easy 5×5, Medium 10×10, Hard 20×20, Expert 30×30, Master 40×40 — ทุกโจทย์มีคำตอบเดียว เกมบันทึกอัตโนมัติ และมีระบบ EXP / Level

## สร้างจากซอร์ส

```powershell
powershell -ExecutionPolicy Bypass -File .\build-desktop.ps1
powershell -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```
