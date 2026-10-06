# Shikaku 四角

[![Build and test](https://github.com/trymybest888/shikaku/actions/workflows/build.yml/badge.svg)](https://github.com/trymybest888/shikaku/actions/workflows/build.yml)

โปรเจกต์ทำเล่น ๆ ของนักศึกษาคนหนึ่งที่ติดเกมปริศนา **Shikaku** (แบ่งสี่เหลี่ยม) จนอยากลองเขียนเองดูสักตัว 😄

เป็นเกมบน Windows ไฟล์เดียวจบ ไม่ต้องติดตั้ง ไม่ต้องต่อเน็ต ดับเบิลคลิกแล้วเล่นได้เลย

<p align="center">
  <img src="docs/screenshot.png" width="720" alt="หน้าตาเกม Shikaku">
</p>

## อยากลองเล่น

1. โหลด `Shikaku.exe` จากหน้า [Releases](https://github.com/trymybest888/shikaku/releases)
2. ดับเบิลคลิก จบ

ใช้ได้กับ Windows 10/11 (ต้องมี .NET Framework 4.5 ขึ้นไป ซึ่งปกติมีมากับ Windows อยู่แล้ว)

> โปรแกรมไม่มีลายเซ็นดิจิทัล (นักศึกษาไม่มีงบซื้อ cert 🥲) Windows เลยอาจเด้งเตือน ให้กด **More info → Run anyway**

## เล่นยังไง

ลากเมาส์บนกระดานเพื่อวาดสี่เหลี่ยม ให้ทั้งกระดานถูกแบ่งเป็นสี่เหลี่ยมหมด โดยแต่ละรูปต้อง

- มีตัวเลข **หนึ่งตัวพอดี**
- มีจำนวนช่องเท่ากับตัวเลขนั้น

ลากทับรูปเดิมเพื่อแทนที่ คลิกรูปเดิมเพื่อลบ เหลือรูปสุดท้ายเมื่อไหร่ เกมจะเติมให้เองแล้วประกาศชนะเลย

| ระดับ  | กระดาน | EXP ที่ได้ |
| ------ | ------ | ---------: |
| Easy   | 5×5    | 25         |
| Medium | 10×10  | 100        |
| Hard   | 20×20  | 400        |
| Expert | 30×30  | 900        |
| Master | 40×40  | 1,600      |

Master 40×40 นี่ตั้งใจให้ทรมานจริง ๆ ขอให้โชคดี

## มีอะไรบ้าง

- **โจทย์ประจำวัน ★** ได้กระดาน 10×10 ที่สุ่มจากวันที่ ทุกคนได้โจทย์เดียวกันในวันเดียวกัน เอาไปแข่งเวลากับเพื่อนได้ (EXP ได้วันละครั้ง)
- **คำใบ้** ถ้าตันจริง ๆ กดแล้วเกมจะเติมให้ 1 รูป แต่กระดานนั้นจะไม่ได้ EXP นะ ไม่งั้นโกงง่ายไป
- **โจทย์มีคำตอบเดียวเสมอ** ทุกโจทย์ถูกเช็กด้วยตัวแก้โจทย์ก่อนให้เล่น ไม่มีโจทย์ที่ต้องเดา และไม่มีเลข 1 ให้เติมฟรี ๆ
- **เซฟอัตโนมัติ** ปิดเกมกลางคันแล้วเปิดใหม่ ได้กระดาน เวลา และปุ่มย้อนกลับครบเหมือนเดิม
- **EXP / Level / ยศ** ทุก 1,000 EXP ขึ้น 1 Level ไต่ยศจากมือใหม่ไปจนถึงเทพเจ้า

<p align="center">
  <img src="assets/ranks/novice.png" width="88" alt="มือใหม่">
  <img src="assets/ranks/skilled.png" width="88" alt="ผู้ชำนาญ">
  <img src="assets/ranks/professional.png" width="88" alt="มืออาชีพ">
  <img src="assets/ranks/divine.png" width="88" alt="เทพเจ้า">
</p>

| Level | ยศ       |
| ----- | -------- |
| 1–5   | มือใหม่  |
| 6–10  | ผู้ชำนาญ |
| 11–20 | มืออาชีพ |
| 21+   | เทพเจ้า  |

ภาพยศสร้างด้วย AI พรอมป์ต์ที่ใช้อยู่ใน [assets/ranks/prompts.md](assets/ranks/prompts.md)

หน้าตาเกมใช้ธีมหมึกครามบนกระดาษกราฟ ให้อารมณ์เหมือนเล่นในหนังสือปริศนาญี่ปุ่น ชนะแล้วได้ตราประทับ 済 สีแดงด้วย

## เซฟเก็บไว้ที่ไหน

เก็บแยกตามผู้ใช้ไว้ที่ `%LOCALAPPDATA%\Shikaku\`

- `session.xml` เกมที่เล่นค้างอยู่
- `progress.xml` EXP และ Level

ทั้งสองไฟล์มีไฟล์สำรอง `.bak` ด้วย ถ้าอยากเริ่มใหม่หมดก็ลบโฟลเดอร์นี้ทิ้งได้เลย

## สำหรับคนอยากแกะโค้ด

เขียนด้วย C# + WinForms ล้วน ๆ คอมไพล์ด้วย `csc.exe` ที่มากับ Windows อยู่แล้ว ไม่ต้องลง Visual Studio หรือ .NET SDK

```powershell
git clone https://github.com/trymybest888/shikaku.git
cd shikaku
powershell -ExecutionPolicy Bypass -File .\build-desktop.ps1
```

จะได้ `Shikaku.exe` ในโฟลเดอร์หลัก (ปิดเกมก่อน build ใหม่นะ ไม่งั้นเขียนทับไฟล์ไม่ได้)

รันเทสต์ (ใช้โฟลเดอร์ชั่วคราว ไม่ยุ่งกับเซฟจริง):

```powershell
powershell -ExecutionPolicy Bypass -File .\tests\run-tests.ps1
```

GitHub Actions จะรันเทสต์และ build ให้ทุกครั้งที่ push ถ้า push tag ที่ขึ้นต้นด้วย `v` (เช่น `v1.1.0`) จะออก Release พร้อมแนบ `.exe` ให้เอง

```
desktop/
  Program.cs          หน้าต่างเกม กระดาน กติกา คำใบ้ โจทย์ประจำวัน
  Theme.cs            สี ฟอนต์ ปุ่ม และตราประทับ
  UniquePuzzle.cs     ตัวแก้โจทย์ เช็กว่ามีคำตอบเดียว
  PlayerProgress.cs   EXP / Level
  SessionStore.cs     เซฟเกมที่เล่นค้าง
assets/ranks/         ภาพยศ
tests/                เทสต์
```

เจอบั๊กหรืออยากเสนออะไร เปิด [Issue](https://github.com/trymybest888/shikaku/issues) มาคุยกันได้เลย 🙏
