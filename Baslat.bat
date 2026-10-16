@echo off
chcp 65001 >nul
title MacroSoft MacERP V7 (.NET 9 C#)
echo ====================================================================
echo             MacroSoft MacERP V7 (.NET 9 C# Reborn)
echo ====================================================================
echo  * Yerel Veritabanı : 127.0.0.1:3306 / erp (3,321,448 Aktif Kayıt)
echo  * Canlı Sistem     : Koruma Altında (Salt Okunur)
echo  * Varsayılan Kul.  : 100 - Ali SARIGÜL (Yönetici / Admin)
echo  * Varsayılan Şifre : 123
echo ====================================================================
echo.
echo Uygulama açılıyor...
start "" "D:\AsiaSoft\src\AsiasoftERP.UI\bin\Debug\net9.0-windows\AsiasoftERP.exe"
exit
